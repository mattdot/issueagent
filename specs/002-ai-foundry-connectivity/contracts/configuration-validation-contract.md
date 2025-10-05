# Azure AI Foundry Configuration Contract

## Contract: Azure AI Foundry Configuration Validation

### Purpose
Define validation rules for Azure AI Foundry connection configuration to ensure all required parameters are provided in the correct format before attempting connection.

### Contract Specification

#### Required Fields
1. **Endpoint** (string)
   - MUST NOT be null, empty, or whitespace
   - MUST start with "https://"
   - MUST match pattern: `https://*.services.ai.azure.com/api/projects/*`
   - Example: `https://my-resource.services.ai.azure.com/api/projects/my-project`

2. **ApiKey** (string)
   - MUST NOT be null, empty, or whitespace
   - MUST be at least 32 characters (Azure API key minimum)
   - MUST be trimmed of leading/trailing whitespace

3. **ModelDeploymentName** (string)
   - If not provided, defaults to `"gpt-5-mini"`
   - If provided, MUST match pattern: `^[a-zA-Z0-9-]+$` (alphanumeric with hyphens only)
   - MUST be between 1 and 64 characters
   - Example: `gpt-5-mini`, `gpt-4o-mini`, `Phi-4-mini-instruct`

#### Optional Fields
4. **ApiVersion** (string)
   - If not provided, defaults to `"2025-04-01-preview"`
   - If provided, MUST match format: `YYYY-MM-DD` or `YYYY-MM-DD-preview`
   - If provided, MUST NOT be a future date
   - Example: `2025-04-01-preview`, `2025-08-07`, `2024-10-21`, `2024-06-01`

5. **ConnectionTimeout** (TimeSpan)
   - If provided, MUST be greater than 0 seconds
   - If provided, MUST be less than or equal to 5 minutes
   - If not provided, defaults to 30 seconds

### Test Scenarios

#### Valid Configuration
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-5-mini",
  "apiVersion": "2025-04-01-preview",
  "connectionTimeout": "00:00:30"
}
```
**Expected**: Validation succeeds

#### Missing Endpoint
```json
{
  "endpoint": null,
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o-mini"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry endpoint is required. Provide 'azure_foundry_endpoint' input or set AZURE_AI_FOUNDRY_ENDPOINT environment variable."`

#### Invalid Endpoint Format (Not HTTPS)
```json
{
  "endpoint": "http://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o-mini"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry endpoint must be a valid HTTPS URL in format: https://<resource>.services.ai.azure.com/api/projects/<project>. Received: http://ai-foundry-test..."`

#### Invalid Endpoint Domain
```json
{
  "endpoint": "https://example.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o-mini"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry endpoint must end with '.services.ai.azure.com/api/projects/<project>'."`

#### Missing API Key
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "",
  "modelDeploymentName": "gpt-5-mini"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry API key is required. Provide 'azure_foundry_api_key' input or set AZURE_AI_FOUNDRY_API_KEY environment variable."`

#### API Key Too Short
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "short",
  "modelDeploymentName": "gpt-5-mini"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry API key must be at least 32 characters."`

#### Missing Model Deployment Name
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": null
}
```
**Expected**: Validation succeeds with default:  
- `modelDeploymentName = "gpt-5-mini"` (default)

#### Invalid Model Deployment Name (Special Characters)
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o_mini!"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry model deployment name must contain only alphanumeric characters and hyphens."`

#### Invalid API Version Format
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-5-mini",
  "apiVersion": "2024.10.21"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry API version must be in format YYYY-MM-DD or YYYY-MM-DD-preview (e.g., 2025-04-01-preview)."`

#### Future API Version
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o-mini",
  "apiVersion": "2099-12-31"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry API version cannot be a future date."`

#### Invalid Connection Timeout (Too Long)
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345",
  "modelDeploymentName": "gpt-4o-mini",
  "connectionTimeout": "00:10:00"
}
```
**Expected**: ValidationException with message:  
`"Azure AI Foundry connection timeout must be between 1 second and 5 minutes."`

#### Valid With Default Values
```json
{
  "endpoint": "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
  "apiKey": "abcdefghijklmnopqrstuvwxyz012345"
}
```
**Expected**: Validation succeeds with:
- `modelDeploymentName = "gpt-5-mini"` (default)
- `apiVersion = "2025-04-01-preview"` (default)
- `connectionTimeout = TimeSpan.FromSeconds(30)` (default)

### Implementation Notes

#### Validation Method Signature
```csharp
public static class AzureFoundryConfigurationValidator
{
    public static ValidationResult Validate(AzureFoundryConfiguration config);
}

public class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorField { get; init; }
}
```

#### Validation Order
1. Check required fields (null/empty check)
2. Validate format of each field
3. Apply default values for optional fields
4. Return first validation error encountered (fail-fast)

#### Endpoint Validation Regex
```regex
^https://[a-z0-9-]+\.services\.ai\.azure\.com/api/projects/[a-z0-9-]+$
```

#### Model Deployment Name Validation Regex
```regex
^[a-zA-Z0-9-]+$
```

#### API Version Validation Regex
```regex
^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$
```

### Contract Test Requirements

Each test scenario above must have a corresponding unit test in:
- `tests/IssueAgent.UnitTests/Shared/AzureFoundryConfigurationValidatorTests.cs`

Tests must:
1. Arrange: Create configuration with specific invalid state
2. Act: Call `Validate()` method
3. Assert: Verify exact error message matches contract specification

### Edge Cases

#### Endpoint with Trailing Slash
```
Input: "https://test.services.ai.azure.com/api/projects/proj/"
Expected: Accept (normalize by trimming trailing slash)
```

#### API Key with Whitespace
```
Input: "  abcdefghijklmnopqrstuvwxyz012345  "
Expected: Accept (trim whitespace before validation)
```

#### Model Name All Hyphens
```
Input: "---"
Expected: Reject (must contain at least one alphanumeric character)
```

#### API Version Boundary Dates
```
Input: "2020-01-01"
Expected: Accept (valid historical date)
Input: "2099-12-31"
Expected: Reject (future date)
```

### Contract Version
**Version**: 1.0.0  
**Created**: 2025-10-05  
**Status**: Draft (to be implemented)

---

**Contract Definition Complete**: All validation rules, test scenarios, and error messages specified for configuration contract.
