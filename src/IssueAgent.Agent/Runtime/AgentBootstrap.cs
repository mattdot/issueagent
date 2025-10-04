using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

        var graphQlLogger = loggerFactory.CreateLogger<GitHubGraphQLClient>();
        var graphQlClient = new GitHubGraphQLClient(environment.Token, graphQlLogger);
        var queryExecutor = new IssueContextQueryExecutor(graphQlClient);
        var metricsRecorder = new LoggingStartupMetricsRecorder(loggerFactory.CreateLogger<LoggingStartupMetricsRecorder>());
        var agent = new IssueContextAgent(new GitHubTokenGuard(), queryExecutor, metricsRecorder);

        try
        {
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
            throw new InvalidOperationException("GitHub token missing. Provide the 'github-token' input or set GITHUB_TOKEN.");
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

        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["repository"] = repository,
            ["eventName"] = eventName,
            ["runId"] = runId,
            ["commentsPageSize"] = commentsPageSize,
            ["github-token"] = token
        };

        return new RuntimeEnvironment(token, request, metadata);
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
        var explicitToken = ReadInputVariable("github-token");
        if (!string.IsNullOrWhiteSpace(explicitToken))
        {
            return explicitToken;
        }

        return Environment.GetEnvironmentVariable("GITHUB_TOKEN");
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
            $"INPUT_{canonical}",
            $"INPUT_{canonical.Replace('-', '_')}"
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

    private sealed record RuntimeEnvironment(string Token, IssueContextRequest Request, IReadOnlyDictionary<string, object?> Metadata);

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
