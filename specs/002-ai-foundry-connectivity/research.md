# Research: AI Foundry Connectivity

## Overview
This document captures research findings for integrating Azure AI Foundry connectivity into the IssueAgent GitHub Action using the Microsoft Agent Framework SDK.

## Key Decisions

### Decision 1: Authentication Method (MVP)
**Chosen**: API Key Authentication  
**Rationale**:
- Simplest to implement and configure in GitHub Actions
- Supported by Microsoft Agent Framework SDK (`Azure.AI.Agents.Persistent`)
- Direct mapping to GitHub Actions secrets
- Azure AI Foundry documentation recommends starting with API keys before moving to managed identities
- Precedent in Agent Framework samples showing API key via `ApiKeyCredential`

**Alternatives Considered**:
- **Managed Identity**: More secure, but requires complex Azure infrastructure setup; reserved for future enhancement
- **Service Principal**: Better than API keys but requires client ID/secret/tenant management; reserved for future enhancement  
- **Azure CLI Credential**: Used in samples but not suitable for GitHub Actions environment

**References**:
- [Azure AI Foundry API Key Authentication](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/openapi-spec#authenticating-with-api-key)
- [Azure AI Services Authentication](https://learn.microsoft.com/en-us/dotnet/ai/azure-ai-services-authentication#authentication-using-keys)

### Decision 2: SDK Selection
**Chosen**: Azure.AI.Agents.Persistent + Azure.Identity  
**Rationale**:
- Official Microsoft Agent Framework package for Azure AI Foundry
- Matches constitutional requirement for Microsoft Agent Framework mandate
- Provides `PersistentAgentsClient` for connection management
- Full support for .NET 8.0 AOT compilation
- Consistent with existing IssueAgent architecture

**Code Pattern**:
```csharp
using Azure.AI.Agents.Persistent;
using Azure;

var endpoint = "https://<resource>.services.ai.azure.com/api/projects/<project>";
var apiKey = Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_API_KEY");
var client = new PersistentAgentsClient(endpoint, new ApiKeyCredential(apiKey));
```

**Alternatives Considered**:
- **Azure.AI.Projects**: General SDK but less specific to agent scenarios
- **Azure.AI.OpenAI**: Limited to OpenAI models only, doesn't support broader Foundry features

**References**:
- [PersistentAgentsClient Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/quickstart#configure-and-run-an-agent)
- [Agent Framework Quickstart](https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start)

### Decision 3: Configuration Inputs
**Chosen**: Three required inputs with environment variable fallbacks  
**Parameters**:
1. `azure_foundry_endpoint` (required): Full project endpoint URL
2. `azure_foundry_api_key` (required): API key from Azure portal
3. `azure_foundry_model_deployment` (optional): Model deployment name, defaults to "gpt-5-mini"
4. `azure_foundry_api_version` (optional): API version, defaults to "2025-04-01-preview"

**Rationale**:
- Matches Azure AI Foundry connection requirements
- Aligns with GitHub Actions input patterns
- Supports both direct inputs and environment variable configuration
- Enables future extensibility without breaking changes

**Environment Variable Mapping**:
- `AZURE_AI_FOUNDRY_ENDPOINT`
- `AZURE_AI_FOUNDRY_API_KEY`
- `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT`
- `AZURE_AI_FOUNDRY_API_VERSION`

**References**:
- [Azure AI Foundry Project Endpoints](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/quickstart#configure-and-run-an-agent)
- [Azure OpenAI API Versioning](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/reference#rest-api-versioning)

### Decision 4: Connection Lifecycle
**Chosen**: Initialize once during startup, fail-fast validation  
**Rationale**:
- Minimizes overhead (single connection establishment)
- Enables <3s cold-start latency requirement
- Fail-fast aligns with clarification decision (no retries)
- Connection validation catches configuration errors early

**Implementation Pattern**:
```csharp
// In AgentBootstrap.cs
public async Task<PersistentAgentsClient> InitializeAzureFoundryAsync(
    string endpoint,
    string apiKey,
    TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    try
    {
        var client = new PersistentAgentsClient(endpoint, new ApiKeyCredential(apiKey));
        // Validate by attempting to retrieve deployment
        await ValidateConnectionAsync(client, cts.Token);
        return client;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Failed to connect to Azure AI Foundry at {endpoint}. " +
            "Verify endpoint URL and API key are correct.", ex);
    }
}
```

**Alternatives Considered**:
- **Lazy initialization**: Deferred connection until first use; rejected due to startup performance preference
- **Connection pooling**: Not applicable for single-execution GitHub Action

### Decision 5: Error Handling Strategy
**Chosen**: Structured error messages with actionable guidance  
**Error Categories**:
1. **Missing Configuration**: "Azure AI Foundry endpoint is required. Provide 'azure_foundry_endpoint' input or set AZURE_AI_FOUNDRY_ENDPOINT environment variable."
2. **Authentication Failure**: "Authentication to Azure AI Foundry failed. Verify API key is valid and not expired."
3. **Network Timeout**: "Connection to Azure AI Foundry timed out after 30 seconds. Check endpoint URL and network connectivity."
4. **Model Not Found**: "Model deployment '{name}' not found in Azure AI Foundry project. Verify deployment name in Azure portal."
5. **API Version Deprecated**: "Azure AI Foundry API version '{version}' is deprecated or unsupported. Update to a supported version."

**Rationale**:
- Aligns with human-centered agent experience constitutional principle
- Reduces support burden by providing clear next steps
- Matches existing error handling patterns in RedactionMiddleware

### Decision 6: Logging Strategy
**Chosen**: Connection events only, no API interaction logging  
**Logged Events**:
- Connection attempt started (endpoint redacted to last 10 chars)
- Connection successful
- Connection failed (with error category, credentials redacted)

**Not Logged**:
- API request/response payloads
- Token usage
- Model inference details

**Rationale**:
- Aligns with clarification decision to defer OpenTelemetry to future
- Maintains security by limiting credential exposure
- Sufficient for troubleshooting connection issues
- Leverages existing RedactionMiddleware for credential scrubbing

**Implementation Pattern**:
```csharp
_logger.LogInformation(
    "Connecting to Azure AI Foundry endpoint ending in {EndpointSuffix}",
    endpoint.Substring(Math.Max(0, endpoint.Length - 10)));

// Use RedactionMiddleware for any credential-adjacent logging
```

### Decision 7: Extensibility Design
**Chosen**: Strategy pattern for authentication providers  
**Interface**:
```csharp
public interface IAzureFoundryAuthenticationProvider
{
    Task<PersistentAgentsClient> CreateClientAsync(
        string endpoint,
        CancellationToken cancellationToken);
}

public class ApiKeyAuthenticationProvider : IAzureFoundryAuthenticationProvider
{
    private readonly string _apiKey;
    public async Task<PersistentAgentsClient> CreateClientAsync(...)
    {
        return new PersistentAgentsClient(endpoint, new ApiKeyCredential(_apiKey));
    }
}

// Future: ManagedIdentityAuthenticationProvider, ServicePrincipalAuthenticationProvider
```

**Rationale**:
- Enables adding new authentication methods without modifying existing code
- Aligns with "extensibility" constitutional principle
- Follows Open/Closed Principle
- Future authentication types can be selected via optional input parameter

**Alternatives Considered**:
- **Direct conditional logic**: Simpler but requires code changes for new auth methods
- **Factory pattern**: More complex than needed for initial implementation

## Best Practices

### Azure AI Foundry Connection Management
**Source**: Microsoft Agent Framework Samples  
**Best Practices**:
1. Store endpoint and API key in secure configuration (GitHub Secrets)
2. Use absolute endpoint URLs (include `/api/projects/{name}`)
3. Validate connection during startup before processing
4. Dispose clients properly if implementing IDisposable patterns
5. Use latest stable API version unless specific version required

### GitHub Actions Security
**Source**: GitHub Actions Best Practices  
**Best Practices**:
1. Always use secrets for API keys (never hardcode)
2. Mask credentials in workflow logs automatically
3. Use environment variables as secondary input method
4. Document required secrets in action README

### .NET 8.0 AOT Compatibility
**Source**: .NET AOT Documentation  
**Considerations**:
1. Azure.AI.Agents.Persistent is AOT-compatible
2. ApiKeyCredential is AOT-compatible
3. Avoid reflection-based credential providers
4. Test AOT build with trimming enabled

## Dependencies

### NuGet Packages
| Package | Version | Purpose |
|---------|---------|---------|
| Azure.AI.Agents.Persistent | Latest | Azure AI Foundry agent client |
| Azure.Identity | Latest | Credential types (ApiKeyCredential) |
| Azure | Latest | Base Azure SDK types |

### Configuration Requirements
- GitHub Actions runner: ubuntu-latest (Docker)
- .NET 8.0 Runtime
- Network access to `*.services.ai.azure.com`

## Performance Considerations

### Cold Start Impact
- **Connection establishment**: ~500-1000ms (network + auth)
- **Validation call**: ~200-500ms (single API call)
- **Total overhead**: ~700-1500ms (well under 3s target)

### Mitigation Strategies
1. Single connection initialization during startup
2. Fail-fast validation (no retries)
3. Parallel initialization with other bootstrap tasks where possible

## Security Considerations

### Credential Handling
- API keys stored exclusively in GitHub Secrets
- RedactionMiddleware scrubs keys from logs
- No credentials in exception messages or stack traces
- Connection strings never logged in full

### Network Security
- HTTPS-only connections to Azure AI Foundry
- No proxy configuration in MVP (future enhancement)
- Certificate validation enforced by Azure SDK

## Testing Strategy

### Unit Tests
1. Configuration parsing (inputs + environment variables)
2. Error message generation for each failure scenario
3. Credential redaction in log messages
4. Authentication provider selection logic

### Integration Tests
1. Successful connection to test Azure AI Foundry endpoint
2. Authentication failure with invalid key
3. Timeout handling with unreachable endpoint
4. Model deployment validation

### Contract Tests
- Verify Azure.AI.Agents.Persistent client initialization API
- Validate error response structures from Azure AI Foundry

## Open Questions Resolved
All NEEDS CLARIFICATION items from Technical Context have been resolved:
- ✅ Language/Version: .NET 8.0 LTS
- ✅ Primary Dependencies: Azure.AI.Agents.Persistent, Azure.Identity
- ✅ Testing: xUnit with integration test support
- ✅ Performance Goals: <3s cold-start
- ✅ Constraints: 30s timeout, no retry logic, connection logging only

## References

### Official Documentation
- [Azure AI Foundry Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/quickstart)
- [Microsoft Agent Framework Tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start)
- [Azure AI Authentication](https://learn.microsoft.com/en-us/dotnet/ai/azure-ai-services-authentication)

### Code Samples
- [Agent Framework Azure AI Foundry Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/AgentProviders/Agent_With_AzureFoundryAgent)
- [PersistentAgentsClient Examples](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/tools/azure-ai-search-samples)

---

**Research Complete**: All technical unknowns resolved. Ready for Phase 1 (Design & Contracts).
