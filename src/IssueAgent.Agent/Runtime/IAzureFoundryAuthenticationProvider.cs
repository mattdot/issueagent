using Azure.AI.Agents.Persistent;

namespace IssueAgent.Agent.Runtime;

/// <summary>
/// Abstraction for different Azure AI Foundry authentication methods.
/// Enables extensibility for future authentication types (managed identity, service principal).
/// </summary>
public interface IAzureFoundryAuthenticationProvider
{
    /// <summary>
    /// Creates an authenticated PersistentAgentsClient for the specified endpoint.
    /// </summary>
    /// <param name="endpoint">Azure AI Foundry project endpoint URL</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Authenticated PersistentAgentsClient</returns>
    Task<PersistentAgentsClient> CreateClientAsync(
        string endpoint,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the human-readable name of the authentication method.
    /// </summary>
    /// <returns>Authentication method name (e.g., "API Key", "Managed Identity")</returns>
    string GetAuthenticationMethodName();
}
