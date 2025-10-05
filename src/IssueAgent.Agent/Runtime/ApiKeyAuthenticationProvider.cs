using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;

namespace IssueAgent.Agent.Runtime;

/// <summary>
/// API key-based authentication provider for Azure AI Foundry.
/// This is an MVP workaround - Azure AI Foundry Persistent Agents primarily supports TokenCredential (Azure Entra ID).
/// For production, use DefaultAzureCredential or ManagedIdentityCredential instead.
/// </summary>
public class ApiKeyAuthenticationProvider : IAzureFoundryAuthenticationProvider
{
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the ApiKeyAuthenticationProvider class.
    /// </summary>
    /// <param name="apiKey">Azure AI Foundry API key</param>
    /// <exception cref="ArgumentException">Thrown when apiKey is null or empty</exception>
    public ApiKeyAuthenticationProvider(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        _apiKey = apiKey;
    }

    /// <inheritdoc />
    public Task<PersistentAgentsClient> CreateClientAsync(
        string endpoint,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Create a simple StaticTokenCredential wrapper for API key
        // Note: This is a workaround for MVP. Azure AI Foundry prefers Azure Entra ID (DefaultAzureCredential)
        var credential = new StaticTokenCredential(_apiKey);
        var client = new PersistentAgentsClient(endpoint, credential);

        return Task.FromResult(client);
    }

    /// <inheritdoc />
    public string GetAuthenticationMethodName()
    {
        return "API Key (Static Token)";
    }

    /// <summary>
    /// Simple TokenCredential implementation that returns a static API key as a bearer token.
    /// This is a workaround for MVP scenarios. Production should use DefaultAzureCredential.
    /// </summary>
    private class StaticTokenCredential : TokenCredential
    {
        private readonly string _apiKey;

        public StaticTokenCredential(string apiKey)
        {
            _apiKey = apiKey;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            // Return API key as bearer token (expires far in the future since it's static)
            return new AccessToken(_apiKey, DateTimeOffset.UtcNow.AddYears(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
