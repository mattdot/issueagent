using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Core.Pipeline;

namespace IssueAgent.Agent.Runtime;

/// <summary>
/// API key-based authentication provider for Azure AI Foundry.
/// Uses a custom authentication policy to properly send the API key in the `api-key` header.
/// For production, consider using DefaultAzureCredential or ManagedIdentityCredential with Azure Entra ID.
/// </summary>
public class ApiKeyAuthenticationProvider : IAzureAIFoundryAuthenticationProvider
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

        // Create a client with API key authentication
        // Azure AI Foundry requires the API key to be sent in the "api-key" header, not as a bearer token
        var options = new PersistentAgentsAdministrationClientOptions();

        // Add our custom policy to inject the api-key header
        options.AddPolicy(new ApiKeyAuthenticationPolicy(_apiKey), HttpPipelinePosition.PerCall);

        // Use a no-op TokenCredential since we're handling auth via the custom policy
        var credential = new NoOpTokenCredential();
        var client = new PersistentAgentsClient(endpoint, credential, options);

        return Task.FromResult(client);
    }

    /// <inheritdoc />
    public string GetAuthenticationMethodName()
    {
        return "API Key";
    }

    /// <summary>
    /// HTTP pipeline policy that adds the Azure AI Foundry API key as a request header.
    /// Azure AI services use the "api-key" header for API key authentication instead of bearer tokens.
    /// </summary>
    private class ApiKeyAuthenticationPolicy : HttpPipelinePolicy
    {
        private readonly string _apiKey;

        public ApiKeyAuthenticationPolicy(string apiKey)
        {
            _apiKey = apiKey;
        }

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            // Add the api-key header for Azure AI Foundry authentication
            message.Request.Headers.SetValue("api-key", _apiKey);

            // Remove the Authorization header if present (from the NoOpTokenCredential)
            message.Request.Headers.Remove("Authorization");

            ProcessNext(message, pipeline);
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            // Add the api-key header for Azure AI Foundry authentication
            message.Request.Headers.SetValue("api-key", _apiKey);

            // Remove the Authorization header if present (from the NoOpTokenCredential)
            message.Request.Headers.Remove("Authorization");

            return ProcessNextAsync(message, pipeline);
        }
    }

    /// <summary>
    /// No-op TokenCredential that provides an empty token.
    /// The actual authentication is handled by ApiKeyAuthenticationPolicy.
    /// </summary>
    private class NoOpTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            // Return an empty/placeholder token - authentication is handled by the custom policy
            return new AccessToken(string.Empty, DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
