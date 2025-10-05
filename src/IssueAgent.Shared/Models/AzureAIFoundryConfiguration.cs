using System.Text.RegularExpressions;

namespace IssueAgent.Shared.Models;

/// <summary>
/// Encapsulates all configuration parameters required to connect to Azure AI Foundry.
/// </summary>
public class AzureAIFoundryConfiguration
{
    private const string DefaultModelDeploymentName = "gpt-5-mini";
    
    /// <summary>
    /// Default API version for Azure AI Foundry connections.
    /// </summary>
    public const string DefaultApiVersion = "2025-04-01-preview";
    
    private static readonly TimeSpan DefaultConnectionTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxConnectionTimeout = TimeSpan.FromMinutes(5);
    
    private static readonly Regex EndpointPattern = new(@"^https://[^/]+\.services\.ai\.azure\.com/api/projects/[^/]+$", RegexOptions.Compiled);
    private static readonly Regex ModelNamePattern = new(@"^[a-zA-Z0-9-]+$", RegexOptions.Compiled);
    private static readonly Regex ApiVersionPattern = new(@"^\d{4}-\d{2}-\d{2}(-preview)?$", RegexOptions.Compiled);

    /// <summary>
    /// Azure AI Foundry project endpoint URL.
    /// Must be a valid HTTPS URL ending in .services.ai.azure.com/api/projects/{name}
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// API key for authentication.
    /// Must be at least 32 characters.
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Name of the deployed model in Azure AI Foundry.
    /// Defaults to "gpt-5-mini" if not provided.
    /// Must contain only alphanumeric characters and hyphens.
    /// </summary>
    public string? ModelDeploymentName { get; set; }

    /// <summary>
    /// Azure AI Foundry API version.
    /// Defaults to "2025-04-01-preview" if not provided.
    /// Must follow YYYY-MM-DD or YYYY-MM-DD-preview format.
    /// </summary>
    public string? ApiVersion { get; set; }

    /// <summary>
    /// Maximum time to wait for connection establishment.
    /// Defaults to 30 seconds if not provided.
    /// Must be greater than 0 and less than or equal to 5 minutes.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; }

    /// <summary>
    /// Validates the configuration and applies defaults.
    /// Throws ValidationException if configuration is invalid.
    /// </summary>
    public void Validate()
    {
        // Validate endpoint
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new ValidationException(
                "Azure AI Foundry endpoint is required. Provide 'azure_foundry_endpoint' input or set AZURE_AI_FOUNDRY_ENDPOINT environment variable.");
        }

        Endpoint = Endpoint.Trim();

        if (!Endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                $"Azure AI Foundry endpoint must be a valid HTTPS URL in format: https://<resource>.services.ai.azure.com/api/projects/<project>. Received: {TruncateEndpoint(Endpoint)}");
        }

        if (!EndpointPattern.IsMatch(Endpoint))
        {
            throw new ValidationException(
                $"Azure AI Foundry endpoint must end with '.services.ai.azure.com/api/projects/<project>'. Received: {TruncateEndpoint(Endpoint)}");
        }

        // Validate API key
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ValidationException(
                "Azure AI Foundry API key is required. Provide 'azure_foundry_api_key' input or set AZURE_AI_FOUNDRY_API_KEY environment variable.");
        }

        ApiKey = ApiKey.Trim();

        if (ApiKey.Length < 32)
        {
            throw new ValidationException(
                "Azure AI Foundry API key must be at least 32 characters. Check the API key in Azure AI Foundry portal under 'Keys and Endpoint'.");
        }

        // Validate and apply default for model deployment name
        if (string.IsNullOrWhiteSpace(ModelDeploymentName))
        {
            ModelDeploymentName = DefaultModelDeploymentName;
        }
        else
        {
            ModelDeploymentName = ModelDeploymentName.Trim();

            if (!ModelNamePattern.IsMatch(ModelDeploymentName))
            {
                throw new ValidationException(
                    $"Model deployment name must contain only alphanumeric characters and hyphens. Received: {ModelDeploymentName}");
            }

            if (ModelDeploymentName.Length < 1 || ModelDeploymentName.Length > 64)
            {
                throw new ValidationException(
                    $"Model deployment name must be between 1 and 64 characters. Received length: {ModelDeploymentName.Length}");
            }
        }

        // Validate and apply default for API version
        if (string.IsNullOrWhiteSpace(ApiVersion))
        {
            ApiVersion = DefaultApiVersion;
        }
        else
        {
            ApiVersion = ApiVersion.Trim();

            if (!ApiVersionPattern.IsMatch(ApiVersion))
            {
                throw new ValidationException(
                    $"Azure AI Foundry API version must be in format YYYY-MM-DD or YYYY-MM-DD-preview (e.g., 2025-04-01-preview). Received: {ApiVersion}");
            }

            // Check for future date (basic check on year-month-day part)
            var datePart = ApiVersion.Split('-')[0..3];
            if (DateTime.TryParse(string.Join("-", datePart), out var apiDate))
            {
                if (apiDate.Date > DateTime.UtcNow.Date)
                {
                    throw new ValidationException(
                        $"API version date cannot be in the future. Received: {ApiVersion}");
                }
            }
        }

        // Validate and apply default for connection timeout
        if (ConnectionTimeout == default)
        {
            ConnectionTimeout = DefaultConnectionTimeout;
        }
        else
        {
            if (ConnectionTimeout <= TimeSpan.Zero)
            {
                throw new ValidationException(
                    $"Connection timeout must be greater than 0 seconds. Received: {ConnectionTimeout.TotalSeconds} seconds");
            }

            if (ConnectionTimeout > MaxConnectionTimeout)
            {
                throw new ValidationException(
                    $"Connection timeout must not exceed 5 minutes. Received: {ConnectionTimeout.TotalMinutes} minutes");
            }
        }
    }

    private static string TruncateEndpoint(string endpoint)
    {
        return endpoint.Length > 50 ? endpoint[..47] + "..." : endpoint;
    }
}

/// <summary>
/// Exception thrown when Azure AI Foundry configuration validation fails.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
