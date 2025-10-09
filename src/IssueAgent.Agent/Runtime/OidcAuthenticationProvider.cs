using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;

namespace IssueAgent.Agent.Runtime;

/// <summary>
/// OIDC-based authentication provider for Azure AI Foundry using service principal credentials.
/// Uses GitHub Actions OIDC token to authenticate via DefaultAzureCredential.
/// </summary>
public class OidcAuthenticationProvider : IAzureAIFoundryAuthenticationProvider
{
    private readonly string _clientId;
    private readonly string _tenantId;

    /// <summary>
    /// Initializes a new instance of the OidcAuthenticationProvider class.
    /// </summary>
    /// <param name="clientId">Azure service principal client ID</param>
    /// <param name="tenantId">Azure tenant ID</param>
    /// <exception cref="ArgumentException">Thrown when clientId or tenantId is null or empty</exception>
    public OidcAuthenticationProvider(string clientId, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        }

        _clientId = clientId;
        _tenantId = tenantId;
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

        // Create credential using DefaultAzureCredential which will use OIDC token from GitHub Actions
        // when running in GitHub Actions environment with proper permissions
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            // Exclude interactive and VS Code credentials for GitHub Actions environment
            ExcludeInteractiveBrowserCredential = true,
            ExcludeVisualStudioCodeCredential = true,
            ExcludeVisualStudioCredential = true,
            ExcludeAzurePowerShellCredential = true,
            ExcludeAzureCliCredential = true,
            // Only use environment variables and workload identity (OIDC)
            TenantId = _tenantId
        };

        var credential = new DefaultAzureCredential(credentialOptions);
        var options = new PersistentAgentsAdministrationClientOptions();
        var client = new PersistentAgentsClient(endpoint, credential, options);

        return Task.FromResult(client);
    }

    /// <inheritdoc />
    public string GetAuthenticationMethodName()
    {
        return "OIDC (Service Principal)";
    }
}
