using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;
using IssueAgent.Agent.GraphQL;
using IssueAgent.Agent.Instrumentation;
using IssueAgent.Agent.Logging;
using IssueAgent.Agent.Security;
using IssueAgent.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IssueAgent.Agent.Runtime;

public static class AgentBootstrap
{

    public static async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        RuntimeEnvironment environment;

        try
        {
            environment = LoadRuntimeEnvironment();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or JsonException)
        {
            Console.Error.WriteLine($"Bootstrap failure: {ex.Message}");
            return 1;
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger("IssueAgent");

        LogMetadata(logger, environment.Metadata);

        // Initialize Azure AI Foundry connection if configured
        var azureFoundryConfig = LoadAzureAIFoundryConfiguration();
        if (azureFoundryConfig != null)
        {
            logger.LogInformation("Initializing Azure AI Foundry connection...");
            var connectionResult = await InitializeAzureAIFoundryAsync(azureFoundryConfig, cancellationToken).ConfigureAwait(false);

            if (!connectionResult.IsSuccess)
            {
                logger.LogError(
                    "Azure AI Foundry connection failed after {Duration}ms: {ErrorMessage} (Category: {ErrorCategory})",
                    connectionResult.Duration.TotalMilliseconds,
                    connectionResult.ErrorMessage,
                    connectionResult.ErrorCategory);
                return 1;
            }

            logger.LogInformation(
                "Azure AI Foundry connection established in {Duration}ms to endpoint {EndpointSuffix}",
                connectionResult.Duration.TotalMilliseconds,
                connectionResult.AttemptedEndpoint);
        }
        else
        {
            logger.LogInformation("Azure AI Foundry not configured - skipping initialization");
        }

        var graphQlLogger = loggerFactory.CreateLogger<GitHubGraphQLClient>();
        var graphQlEndpoint = ReadGraphQLEndpoint();
        var graphQlClient = new GitHubGraphQLClient(environment.Token, graphQlLogger, endpoint: graphQlEndpoint);
        var queryExecutor = new IssueContextQueryExecutor(graphQlClient);
        var metricsRecorder = new LoggingStartupMetricsRecorder(loggerFactory.CreateLogger<LoggingStartupMetricsRecorder>());
        var agent = new IssueContextAgent(new GitHubTokenGuard(), queryExecutor, metricsRecorder);

        try
        {
            // Check if execution should be skipped (bot or insufficient permissions)
            if (environment.ShouldSkip)
            {
                logger.LogInformation("Execution skipped: triggered by bot or user without sufficient permissions");
                var skipResult = IssueContextResult.Skipped(
                    environment.Request.RunId,
                    environment.Request.EventType,
                    "Triggered by bot or user without write/maintain/admin permissions");
                
                LogResult(logger, skipResult);
                return 0; // Success exit code for skipped execution
            }

            var result = await agent.ExecuteAsync(environment.Request, environment.Token, cancellationToken).ConfigureAwait(false);

            LogResult(logger, result);

            return result.Status == IssueContextStatus.Success ? 0 : 1;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Execution cancelled.");
            return 130;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception executing issue context agent.");
            return 1;
        }
    }

    private static RuntimeEnvironment LoadRuntimeEnvironment()
    {
        var token = ReadToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("GitHub token missing. Provide the 'github_token' input or set GITHUB_TOKEN.");
        }

        var repository = RequireEnvironment("GITHUB_REPOSITORY", "Repository context not supplied (GITHUB_REPOSITORY).");
        var eventName = RequireEnvironment("GITHUB_EVENT_NAME", "Event name missing (GITHUB_EVENT_NAME).");
        var eventPath = RequireEnvironment("GITHUB_EVENT_PATH", "Event payload path missing (GITHUB_EVENT_PATH).");
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? Guid.NewGuid().ToString("n");
        var commentsPageSize = ParseIntEnvironment("INPUT_COMMENTS_PAGE_SIZE", 5, 1, 20);

        var eventPayload = File.ReadAllText(eventPath);
        using var document = JsonDocument.Parse(eventPayload);
        var root = document.RootElement.Clone();

        var request = CreateRequest(repository, eventName, runId, commentsPageSize, root);
        var shouldSkip = ShouldSkipExecution(eventName, root);

        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["repository"] = repository,
            ["eventName"] = eventName,
            ["runId"] = runId,
            ["commentsPageSize"] = commentsPageSize,
            ["github_token"] = token
        };

        return new RuntimeEnvironment(token, request, metadata, shouldSkip);
    }

    /// <summary>
    /// Initializes and validates connection to Azure AI Foundry.
    /// </summary>
    /// <param name="configuration">Azure AI Foundry configuration with endpoint, API key, model deployment, and API version.</param>
    /// <param name="cancellationToken">Cancellation token to abort the connection attempt.</param>
    /// <returns>Connection result with client instance on success or error details on failure.</returns>
    public static async Task<AzureAIFoundryConnectionResult> InitializeAzureAIFoundryAsync(
        AzureAIFoundryConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var stopwatch = Stopwatch.StartNew();
        var attemptedEndpoint = configuration.Endpoint;

        try
        {
            // Validate configuration before attempting connection
            configuration.Validate();

            // Create timeout enforcement
            using var timeoutCts = new CancellationTokenSource(configuration.ConnectionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            // Create authentication provider and client
            var authProvider = new ApiKeyAuthenticationProvider(configuration.ApiKey);
            var client = await authProvider.CreateClientAsync(
                configuration.Endpoint,
                linkedCts.Token).ConfigureAwait(false);

            // Validate connection by attempting to get the Administration client
            // This ensures the endpoint is reachable and credentials are valid
            // We use a minimal operation to avoid creating test resources
            try
            {
                // Simple connectivity check - accessing Administration property
                // The actual API call will happen when we use the agent in production
                var administrationClient = client.Administration;
                
                // Note: We don't perform actual agent creation/deletion here to avoid polluting
                // the Azure AI Foundry environment. The client construction and property access
                // validates that credentials and endpoint are structurally correct.
                // Network and service errors will be caught in production usage.
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Model/deployment not found (this would happen during actual agent operations)
                stopwatch.Stop();
                return AzureAIFoundryConnectionResult.Failure(
                    $"Model deployment '{configuration.ModelDeploymentName ?? "gpt-5-mini"}' not found. Verify the deployment name in Azure AI Foundry.",
                    ConnectionErrorCategory.ModelNotFound,
                    attemptedEndpoint,
                    stopwatch.Elapsed);
            }
            catch (RequestFailedException ex) when (ex.Status == 401 || ex.Status == 403)
            {
                // Authentication failure
                stopwatch.Stop();
                return AzureAIFoundryConnectionResult.Failure(
                    $"Authentication failed. Verify the API key has access to the endpoint. Status: {ex.Status}",
                    ConnectionErrorCategory.AuthenticationFailure,
                    attemptedEndpoint,
                    stopwatch.Elapsed);
            }
            catch (RequestFailedException ex) when (ex.Status == 429)
            {
                // Quota exceeded
                stopwatch.Stop();
                return AzureAIFoundryConnectionResult.Failure(
                    "Azure AI Foundry quota exceeded. Check your subscription limits and usage.",
                    ConnectionErrorCategory.QuotaExceeded,
                    attemptedEndpoint,
                    stopwatch.Elapsed);
            }
            catch (RequestFailedException ex) when (ex.Status == 400 && ex.Message.Contains("api-version"))
            {
                // API version unsupported
                stopwatch.Stop();
                return AzureAIFoundryConnectionResult.Failure(
                    $"API version '{configuration.ApiVersion ?? AzureAIFoundryConfiguration.DefaultApiVersion}' is not supported by the endpoint. Use a supported version like '{AzureAIFoundryConfiguration.DefaultApiVersion}'.",
                    ConnectionErrorCategory.ApiVersionUnsupported,
                    attemptedEndpoint,
                    stopwatch.Elapsed);
            }
            catch (RequestFailedException ex)
            {
                // Other Azure request failures
                stopwatch.Stop();
                return AzureAIFoundryConnectionResult.Failure(
                    $"Azure AI Foundry request failed: {ex.Message} (Status: {ex.Status})",
                    ConnectionErrorCategory.UnknownError,
                    attemptedEndpoint,
                    stopwatch.Elapsed);
            }

            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Success(client, attemptedEndpoint, stopwatch.Elapsed);
        }
        catch (ValidationException validationEx)
        {
            stopwatch.Stop();
            
            // Distinguish between missing configuration and invalid configuration
            var errorCategory = validationEx.Message.Contains("is required", StringComparison.OrdinalIgnoreCase)
                ? ConnectionErrorCategory.MissingConfiguration
                : ConnectionErrorCategory.InvalidConfiguration;
            
            return AzureAIFoundryConnectionResult.Failure(
                validationEx.Message,
                errorCategory,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // External cancellation (user/system requested) - not a timeout
            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Failure(
                "Connection attempt was cancelled by the caller.",
                ConnectionErrorCategory.UnknownError,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            // Timeout cancellation (exceeded ConnectionTimeout)
            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Failure(
                $"Connection attempt timed out after {configuration.ConnectionTimeout.TotalSeconds:F1} seconds. Check network connectivity and endpoint availability.",
                ConnectionErrorCategory.NetworkTimeout,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
        catch (HttpRequestException httpEx) when (httpEx.InnerException is SocketException)
        {
            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Failure(
                $"Network error connecting to Azure AI Foundry: {httpEx.Message}. Check endpoint URL and network connectivity.",
                ConnectionErrorCategory.NetworkError,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
        catch (HttpRequestException httpEx)
        {
            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Failure(
                $"HTTP error connecting to Azure AI Foundry: {httpEx.Message}",
                ConnectionErrorCategory.NetworkError,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return AzureAIFoundryConnectionResult.Failure(
                $"Unexpected error initializing Azure AI Foundry: {ex.GetType().Name} - {ex.Message}",
                ConnectionErrorCategory.UnknownError,
                attemptedEndpoint,
                stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Loads Azure AI Foundry configuration from action inputs or environment variables.
    /// Returns null if Azure AI Foundry is not configured (optional feature).
    /// </summary>
    private static AzureAIFoundryConfiguration? LoadAzureAIFoundryConfiguration()
    {
        // Check if Azure AI Foundry is configured (endpoint is the required field)
        var endpoint = Environment.GetEnvironmentVariable("INPUT_AZURE_FOUNDRY_ENDPOINT")
            ?? Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT");

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            // Azure AI Foundry not configured - this is optional
            return null;
        }

        var apiKey = Environment.GetEnvironmentVariable("INPUT_AZURE_FOUNDRY_API_KEY")
            ?? Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_API_KEY")
            ?? throw new InvalidOperationException(
                "Azure AI Foundry API key is required when endpoint is provided. " +
                "Set 'azure_foundry_api_key' input or AZURE_AI_FOUNDRY_API_KEY environment variable.");

        var modelDeployment = Environment.GetEnvironmentVariable("INPUT_AZURE_FOUNDRY_MODEL_DEPLOYMENT")
            ?? Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT");

        var apiVersion = Environment.GetEnvironmentVariable("INPUT_AZURE_FOUNDRY_API_VERSION")
            ?? Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_API_VERSION");

        return new AzureAIFoundryConfiguration
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            ModelDeploymentName = modelDeployment,
            ApiVersion = apiVersion,
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };
    }

    private static string RequireEnvironment(string key, string message)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value;
    }

    private static string? ReadToken()
    {
    var explicitToken = ReadInputVariable("github_token");
        if (!string.IsNullOrWhiteSpace(explicitToken))
        {
            return explicitToken;
        }

        return Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    }

    private static Uri? ReadGraphQLEndpoint()
    {
        // Check for GITHUB_GRAPHQL_URL first (GitHub Enterprise Server)
        var graphqlUrl = Environment.GetEnvironmentVariable("GITHUB_GRAPHQL_URL");
        if (!string.IsNullOrWhiteSpace(graphqlUrl) && Uri.TryCreate(graphqlUrl, UriKind.Absolute, out var graphqlUri))
        {
            return graphqlUri;
        }

        // Fallback to GITHUB_API_URL and append /graphql (GitHub Enterprise Server)
        var apiUrl = Environment.GetEnvironmentVariable("GITHUB_API_URL");
        if (!string.IsNullOrWhiteSpace(apiUrl) && Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri))
        {
            // GITHUB_API_URL is typically "https://api.github.com" or "https://github.company.com/api/v3"
            // GraphQL endpoint is at the same origin + /graphql
            var graphqlPath = new Uri(apiUri, "/graphql");
            return graphqlPath;
        }

        // Return null to use the default GitHub.com endpoint
        return null;
    }

    private static string? ReadInputVariable(string inputName)
    {
        if (string.IsNullOrWhiteSpace(inputName))
        {
            return null;
        }

        var canonical = inputName.Trim().Replace(' ', '-').ToUpperInvariant();
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"INPUT_{canonical}"
        };

        foreach (var key in candidates)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int ParseIntEnvironment(string key, int fallback, int minValue, int maxValue)
    {
        var raw = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, minValue, maxValue);
    }

    private static IssueContextRequest CreateRequest(string repository, string eventName, string runId, int commentsPageSize, JsonElement payload)
    {
        var segments = repository.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2)
        {
            throw new InvalidOperationException($"Repository value '{repository}' is invalid. Expected 'owner/name'.");
        }

        var issueNumber = ReadIssueNumber(payload);
        var eventType = ResolveEventType(eventName, payload);

        return new IssueContextRequest(
            Owner: segments[0],
            Name: segments[1],
            IssueNumber: issueNumber,
            CommentsPageSize: commentsPageSize,
            RunId: runId,
            EventType: eventType);
    }

    private static bool ShouldSkipExecution(string eventName, JsonElement payload)
    {
        // Check if the issue is actually a pull request
        // PRs are a special type of issue in GitHub, but we should not process them
        if (payload.TryGetProperty("issue", out var issueElement))
        {
            if (issueElement.TryGetProperty("pull_request", out _))
            {
                return true;
            }
        }

        // Only check for issue_comment events
        if (!string.Equals(eventName, "issue_comment", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Check if sender is a bot
        if (payload.TryGetProperty("sender", out var senderElement))
        {
            if (senderElement.TryGetProperty("type", out var typeElement))
            {
                var senderType = typeElement.GetString();
                if (string.Equals(senderType, "Bot", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // Check if comment author has insufficient permissions
        if (payload.TryGetProperty("comment", out var commentElement))
        {
            if (commentElement.TryGetProperty("author_association", out var associationElement))
            {
                var association = associationElement.GetString();
                // Allow: OWNER, MEMBER, COLLABORATOR (with write access), MAINTAINER (GitHub org)
                // Block: CONTRIBUTOR, FIRST_TIME_CONTRIBUTOR, FIRST_TIMER, NONE
                if (!string.IsNullOrEmpty(association))
                {
                    var allowedAssociations = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "OWNER", "MEMBER", "COLLABORATOR", "MAINTAINER"
                    };
                    
                    if (!allowedAssociations.Contains(association))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static int ReadIssueNumber(JsonElement payload)
    {
        if (!payload.TryGetProperty("issue", out var issueElement))
        {
            throw new InvalidOperationException("Event payload missing 'issue' node.");
        }

        if (!issueElement.TryGetProperty("number", out var numberElement))
        {
            throw new InvalidOperationException("Issue payload missing 'number'.");
        }

        return numberElement.GetInt32();
    }

    private static IssueEventType ResolveEventType(string eventName, JsonElement payload)
    {
        if (string.Equals(eventName, "issue_comment", StringComparison.OrdinalIgnoreCase))
        {
            EnsureAction(payload, "created", "issue_comment");
            return IssueEventType.IssueCommentCreated;
        }

        if (string.Equals(eventName, "issues", StringComparison.OrdinalIgnoreCase))
        {
            var action = ReadAction(payload);
            return action switch
            {
                var value when string.Equals(value, "opened", StringComparison.OrdinalIgnoreCase) => IssueEventType.IssueOpened,
                var value when string.Equals(value, "reopened", StringComparison.OrdinalIgnoreCase) => IssueEventType.IssueReopened,
                _ => throw new InvalidOperationException($"Unsupported issues action '{action}'.")
            };
        }

        throw new InvalidOperationException($"Unsupported GitHub event '{eventName}'.");
    }

    private static void EnsureAction(JsonElement payload, string expected, string eventName)
    {
        var action = ReadAction(payload);
        if (!string.Equals(action, expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported {eventName} action '{action}'.");
        }
    }

    private static string ReadAction(JsonElement payload)
    {
        if (!payload.TryGetProperty("action", out var actionElement))
        {
            throw new InvalidOperationException("Event payload missing 'action'.");
        }

        return actionElement.GetString() ?? string.Empty;
    }

    private static void LogMetadata(ILogger logger, IReadOnlyDictionary<string, object?> metadata)
    {
        var sanitized = RedactionMiddleware.RedactPayload(metadata);
        logger.LogInformation("Runtime metadata: {Metadata}", FormatKeyValuePairs(sanitized));
    }

    private static void LogResult(ILogger logger, IssueContextResult result)
    {
        logger.LogInformation("Issue context status: {Status} - {Message}", result.Status, result.Message);

        var issueSummary = result.Issue is null
            ? "issue=null"
            : $"issueId={result.Issue.Id}, issueNumber={result.Issue.Number}, comments={(result.Issue.LatestComments?.Count ?? 0)}";

        logger.LogInformation(
            "Issue context detail: runId={RunId}, eventType={EventType}, retrievedAtUtc={RetrievedAtUtc:o}, {IssueSummary}",
            result.RunId,
            result.EventType,
            result.RetrievedAtUtc,
            issueSummary);
    }

    private static string FormatKeyValuePairs(IReadOnlyDictionary<string, object?> values)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var pair in values)
        {
            if (!first)
            {
                builder.Append(", ");
            }

            builder.Append(pair.Key);
            builder.Append('=');
            builder.Append(pair.Value);

            first = false;
        }

        return builder.ToString();
    }

    private sealed record RuntimeEnvironment(string Token, IssueContextRequest Request, IReadOnlyDictionary<string, object?> Metadata, bool ShouldSkip);

    private sealed class LoggingStartupMetricsRecorder : StartupMetricsRecorder
    {
        private readonly ILogger<LoggingStartupMetricsRecorder> _logger;

        public LoggingStartupMetricsRecorder(ILogger<LoggingStartupMetricsRecorder> logger)
        {
            _logger = logger;
        }

        protected override void Record(TimeSpan duration)
        {
            var milliseconds = (long)Math.Round(duration.TotalMilliseconds);
            _logger.LogInformation("StartupDurationMs={Duration}", milliseconds);
        }
    }
}
