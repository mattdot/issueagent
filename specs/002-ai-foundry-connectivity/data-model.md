# Data Model: AI Foundry Connectivity

## Overview
This document defines the data structures and entities required for Azure AI Foundry connectivity in the IssueAgent GitHub Action.

## Core Entities

### 1. AzureFoundryConfiguration
**Purpose**: Encapsulates all configuration parameters required to connect to Azure AI Foundry  
**Location**: `src/IssueAgent.Shared/Models/AzureFoundryConfiguration.cs`

**Fields**:
| Field | Type | Required | Default | Validation | Description |
|-------|------|----------|---------|------------|-------------|
| Endpoint | string | Yes | - | Must be valid HTTPS URL ending in `.services.ai.azure.com/api/projects/{name}` | Azure AI Foundry project endpoint URL |
| ApiKey | string | Yes | - | Non-empty, trimmed | API key for authentication |
| ModelDeploymentName | string | No | "gpt-5-mini" | Non-empty, alphanumeric with hyphens | Name of the deployed model in Azure AI Foundry |
| ApiVersion | string | No | "2025-04-01-preview" | Format: YYYY-MM-DD or YYYY-MM-DD-preview | Azure AI Foundry API version |
| ConnectionTimeout | TimeSpan | No | 30 seconds | > 0 and <= 5 minutes | Maximum time to wait for connection establishment |

**Validation Rules**:
- Endpoint must start with "https://"
- Endpoint must match pattern: `https://*.services.ai.azure.com/api/projects/*`
- ApiKey must not be null, empty, or whitespace
- ModelDeploymentName must not contain special characters except hyphens
- ApiVersion must follow YYYY-MM-DD format
- ConnectionTimeout must be between 1 second and 5 minutes

**Relationships**:
- Used by `IssueAgent.Agent.Runtime.AgentBootstrap` for initialization
- Populated from `IssueAgent.Action.Program` action inputs

### 2. AzureFoundryConnectionResult
**Purpose**: Represents the outcome of an Azure AI Foundry connection attempt  
**Location**: `src/IssueAgent.Shared/Models/AzureFoundryConnectionResult.cs`

**Fields**:
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| IsSuccess | bool | Yes | Indicates whether connection was successful |
| Client | PersistentAgentsClient? | Conditional | Initialized client (null if IsSuccess = false) |
| ErrorMessage | string? | Conditional | Descriptive error message (null if IsSuccess = true) |
| ErrorCategory | ConnectionErrorCategory? | Conditional | Categorization of error (null if IsSuccess = true) |
| AttemptedEndpoint | string | Yes | Endpoint URL that was attempted (last 20 chars for logging) |
| AttemptedAt | DateTimeOffset | Yes | UTC timestamp of connection attempt |
| Duration | TimeSpan | Yes | Time taken for connection attempt |

**State Transitions**:
- Initial state: Uninitialized
- After attempt: IsSuccess = true (with Client) OR IsSuccess = false (with ErrorMessage + ErrorCategory)

### 3. ConnectionErrorCategory (Enum)
**Purpose**: Categorizes connection failures for error handling and logging  
**Location**: `src/IssueAgent.Shared/Models/ConnectionErrorCategory.cs`

**Values**:
| Value | Description | Example Scenario |
|-------|-------------|------------------|
| MissingConfiguration | Required configuration parameter not provided | Endpoint or API key missing |
| InvalidConfiguration | Configuration parameter format invalid | Malformed endpoint URL |
| AuthenticationFailure | API key rejected by Azure AI Foundry | Expired or invalid key |
| NetworkTimeout | Connection attempt exceeded timeout | Unreachable endpoint |
| NetworkError | Network-level connectivity failure | DNS resolution failure |
| ModelNotFound | Specified model deployment doesn't exist | Wrong deployment name |
| QuotaExceeded | Azure AI Foundry service quota exceeded | Rate limiting |
| ApiVersionUnsupported | API version deprecated or not supported | Old API version specified |
| UnknownError | Unexpected error not categorized above | Other failures |

**Usage**: Maps to specific error messages in error handling logic

### 4. IAzureFoundryAuthenticationProvider (Interface)
**Purpose**: Abstraction for different authentication methods (extensibility)  
**Location**: `src/IssueAgent.Agent/Runtime/IAzureFoundryAuthenticationProvider.cs`

**Methods**:
```csharp
Task<PersistentAgentsClient> CreateClientAsync(
    string endpoint,
    CancellationToken cancellationToken);
    
string GetAuthenticationMethodName();
```

**Implementations**:
- `ApiKeyAuthenticationProvider` (MVP)
- `ManagedIdentityAuthenticationProvider` (future)
- `ServicePrincipalAuthenticationProvider` (future)

**Relationship**:
- Used by `AgentBootstrap` to create `PersistentAgentsClient`
- Selected based on configuration (currently hardcoded to API key)

## Supporting Types

### 5. AzureFoundryConfigurationSource (Enum)
**Purpose**: Tracks where configuration values originated  
**Location**: `src/IssueAgent.Shared/Models/AzureFoundryConfigurationSource.cs`

**Values**:
- `ActionInput`: Value from GitHub Actions input parameter
- `EnvironmentVariable`: Value from environment variable
- `DefaultValue`: System default value used

**Usage**: Included in error messages to guide users to correct configuration location

## Entity Relationships

```
┌─────────────────────────────┐
│ Program (Action Entry)      │
│                             │
│ Reads GitHub Action inputs  │
│ and environment variables   │
└──────────┬──────────────────┘
           │ creates
           ▼
┌──────────────────────────────┐
│ AzureFoundryConfiguration    │
│                              │
│ - Endpoint                   │
│ - ApiKey                     │
│ - ModelDeploymentName        │
│ - ApiVersion                 │
│ - ConnectionTimeout          │
└──────────┬───────────────────┘
           │ passed to
           ▼
┌──────────────────────────────┐
│ AgentBootstrap               │
│                              │
│ Uses IAzureFoundryAuth-      │
│ enticationProvider           │
└──────────┬───────────────────┘
           │ creates
           ▼
┌──────────────────────────────┐
│ AzureFoundryConnectionResult │
│                              │
│ - IsSuccess                  │
│ - Client / ErrorMessage      │
│ - ErrorCategory              │
└──────────────────────────────┘
```

## Data Flow

### Successful Connection Flow
1. **Input** → `Program` parses action inputs and environment variables
2. **Validation** → Create `AzureFoundryConfiguration` with validation
3. **Bootstrap** → `AgentBootstrap.InitializeAzureFoundryAsync()` called
4. **Authentication** → `ApiKeyAuthenticationProvider.CreateClientAsync()` instantiates client
5. **Validation** → Attempt to validate connection (lightweight API call)
6. **Result** → `AzureFoundryConnectionResult` with IsSuccess=true and populated Client
7. **Usage** → Client stored in agent context for issue processing

### Failed Connection Flow
1. **Input** → `Program` parses action inputs and environment variables
2. **Validation** → Validation failure OR configuration missing
3. **Error Categorization** → Exception mapped to `ConnectionErrorCategory`
4. **Result** → `AzureFoundryConnectionResult` with IsSuccess=false, ErrorMessage, and ErrorCategory
5. **Logging** → Error logged with category and actionable guidance
6. **Termination** → Action fails with exit code 1

## Validation Logic

### Endpoint Validation
```
1. Check not null/empty
2. Trim whitespace
3. Verify HTTPS scheme
4. Verify domain ends with .services.ai.azure.com
5. Verify path starts with /api/projects/
6. Extract project name from path
```

### API Key Validation
```
1. Check not null/empty
2. Trim whitespace
3. Verify minimum length (32 chars for Azure API keys)
```

### Model Deployment Name Validation
```
1. If not provided, use default value "gpt-5-mini"
2. Trim whitespace
3. Verify alphanumeric with hyphens only (regex: ^[a-zA-Z0-9-]+$)
4. Verify length between 1 and 64 characters
```

### API Version Validation
```
1. If provided:
   a. Verify format matches YYYY-MM-DD or YYYY-MM-DD-preview (regex: ^\d{4}-\d{2}-\d{2}(-preview)?$)
   b. Verify date is not in future
2. If not provided, use default value "2025-04-01-preview"
```

## Error Message Templates

### Missing Configuration
```
"Azure AI Foundry {parameter} is required. Provide '{input-name}' input or set {ENV_VAR} environment variable."
```

### Invalid Endpoint
```
"Azure AI Foundry endpoint must be a valid URL in format: https://<resource>.services.ai.azure.com/api/projects/<project>. Received: {sanitized-endpoint}"
```

### Authentication Failure
```
"Authentication to Azure AI Foundry failed. Verify API key is valid and not expired. Check Azure portal under 'Keys and Endpoint' for your AI Foundry project."
```

### Network Timeout
```
"Connection to Azure AI Foundry timed out after {timeout} seconds. Verify endpoint URL is correct and network connectivity allows access to {domain}."
```

### Model Not Found
```
"Model deployment '{model-name}' not found in Azure AI Foundry project. Verify deployment name in Azure AI Foundry portal under 'Models + endpoints'."
```

### Quota Exceeded
```
"Azure AI Foundry service quota exceeded. Retry after a delay or request quota increase in Azure portal."
```

### API Version Unsupported
```
**Format**: Structured error message with context  
**Example**:  
"Azure AI Foundry API version '{version}' is deprecated or unsupported. Update to a supported version (latest: 2025-04-01-preview)."
```

## Storage and Lifecycle

### Configuration Lifecycle
- **Created**: Once during action startup in `Program.Main()`
- **Validated**: Immediately after creation, before bootstrap
- **Used**: Passed to `AgentBootstrap.InitializeAzureFoundryAsync()`
- **Disposed**: Configuration is immutable; no disposal needed

### Client Lifecycle
- **Created**: Once during bootstrap after successful connection
- **Used**: Throughout issue processing workflow
- **Disposed**: When action completes (PersistentAgentsClient implements IDisposable)

### Connection Result Lifecycle
- **Created**: After connection attempt completes
- **Logged**: Immediately after creation
- **Used**: To determine action success/failure and error reporting
- **Disposed**: Not applicable (value type and strings)

## Serialization

### Action Inputs (YAML)
```yaml
- uses: mattdot/issueagent@v1
  with:
    azure_foundry_endpoint: ${{ secrets.AZURE_FOUNDRY_ENDPOINT }}
    azure_foundry_api_key: ${{ secrets.AZURE_FOUNDRY_API_KEY }}
    azure_foundry_model_deployment: 'gpt-5-mini'  # optional, defaults to gpt-5-mini
    azure_foundry_api_version: '2025-04-01-preview'  # optional
```

### Environment Variables
```bash
export AZURE_AI_FOUNDRY_ENDPOINT="https://..."
export AZURE_AI_FOUNDRY_API_KEY="..."
export AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT="gpt-5-mini"  # optional, defaults to gpt-5-mini
export AZURE_AI_FOUNDRY_API_VERSION="2025-04-01-preview"  # optional
```

### Logging Format (Redacted)
```
[INFO] Connecting to Azure AI Foundry endpoint ending in ...project-name
[INFO] Azure AI Foundry connection successful (duration: 847ms)
```

```
[ERROR] Azure AI Foundry connection failed: Authentication failure
[ERROR] Category: AuthenticationFailure
[ERROR] Guidance: Verify API key is valid and not expired...
```

## Dependencies

### External Types
- `Azure.AI.Agents.Persistent.PersistentAgentsClient` (from Azure.AI.Agents.Persistent package)
- `Azure.ApiKeyCredential` (from Azure package)
- `System.TimeSpan` (framework)
- `System.DateTimeOffset` (framework)

### Internal Types
- `IssueAgent.Shared.Models.*` (configuration and result models)
- `IssueAgent.Agent.Runtime.AgentBootstrap` (uses configuration)
- `IssueAgent.Action.Program` (creates configuration)

---

**Data Model Complete**: All entities, relationships, validation rules, and error messages defined. Ready for contract generation.
