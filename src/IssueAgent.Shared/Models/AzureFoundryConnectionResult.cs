using Azure.AI.Agents.Persistent;

namespace IssueAgent.Shared.Models;

/// <summary>
/// Represents the outcome of an Azure AI Foundry connection attempt.
/// </summary>
public class AzureFoundryConnectionResult
{
    /// <summary>
    /// Indicates whether the connection was successful.
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// The initialized PersistentAgentsClient if connection was successful, null otherwise.
    /// </summary>
    public PersistentAgentsClient? Client { get; init; }

    /// <summary>
    /// Descriptive error message if connection failed, null otherwise.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Categorization of the error if connection failed, null otherwise.
    /// </summary>
    public ConnectionErrorCategory? ErrorCategory { get; init; }

    /// <summary>
    /// Last 20 characters of the endpoint URL that was attempted (for logging).
    /// </summary>
    public required string AttemptedEndpoint { get; init; }

    /// <summary>
    /// UTC timestamp of the connection attempt.
    /// </summary>
    public required DateTimeOffset AttemptedAt { get; init; }

    /// <summary>
    /// Time taken for the connection attempt.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a successful connection result.
    /// </summary>
    public static AzureFoundryConnectionResult Success(
        PersistentAgentsClient client,
        string endpoint,
        TimeSpan duration)
    {
        return new AzureFoundryConnectionResult
        {
            IsSuccess = true,
            Client = client,
            ErrorMessage = null,
            ErrorCategory = null,
            AttemptedEndpoint = GetEndpointSuffix(endpoint),
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = duration
        };
    }

    /// <summary>
    /// Creates a failed connection result.
    /// </summary>
    public static AzureFoundryConnectionResult Failure(
        string errorMessage,
        ConnectionErrorCategory errorCategory,
        string endpoint,
        TimeSpan duration)
    {
        return new AzureFoundryConnectionResult
        {
            IsSuccess = false,
            Client = null,
            ErrorMessage = errorMessage,
            ErrorCategory = errorCategory,
            AttemptedEndpoint = GetEndpointSuffix(endpoint),
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = duration
        };
    }

    private static string GetEndpointSuffix(string endpoint)
    {
        if (string.IsNullOrEmpty(endpoint))
        {
            return "<empty>";
        }

        return endpoint.Length > 20 ? "..." + endpoint[^20..] : endpoint;
    }
}
