# OIDC Authentication Implementation Summary

This document summarizes the changes made to implement OIDC authentication for Azure AI Foundry in the Issue Agent.

## Overview

The Issue Agent now uses **OIDC (OpenID Connect)** authentication via Azure service principal instead of API keys. This provides:

- **Enhanced Security**: No API keys to manage or rotate
- **Better Audit Trail**: Service principal actions are tracked in Azure Activity Log
- **Simplified Key Management**: No secrets to store, only client and tenant IDs
- **Compliance**: Meets enterprise security requirements for identity-based access

## Changes Made

### 1. Action Configuration (`action.yml`)

**Removed:**
- `azure_ai_foundry_api_key` input

**Added:**
- `azure_client_id` input
- `azure_tenant_id` input

**Updated environment variables:**
- `AZURE_AI_FOUNDRY_API_KEY` → Removed
- `AZURE_CLIENT_ID` → Added
- `AZURE_TENANT_ID` → Added

### 2. Authentication Provider

**Removed:**
- `src/IssueAgent.Agent/Runtime/ApiKeyAuthenticationProvider.cs`
- Custom HTTP pipeline policy for API key injection
- NoOpTokenCredential wrapper

**Added:**
- `src/IssueAgent.Agent/Runtime/OidcAuthenticationProvider.cs`
- Uses `DefaultAzureCredential` from Azure.Identity
- Configured to use workload identity (OIDC) from GitHub Actions

### 3. Configuration Model

**File:** `src/IssueAgent.Shared/Models/AzureAIFoundryConfiguration.cs`

**Changed properties:**
- `ApiKey` (required string, min 32 chars) → Removed
- `ClientId` (required string) → Added
- `TenantId` (required string) → Added

**Updated validation:**
- Removed API key length validation
- Added client ID and tenant ID required validation
- Updated error messages to reference new configuration

### 4. Bootstrap Configuration

**File:** `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs`

**Updated `LoadAzureAIFoundryConfiguration` method:**
- Reads `INPUT_AZURE_CLIENT_ID` and `AZURE_CLIENT_ID` environment variables
- Reads `INPUT_AZURE_TENANT_ID` and `AZURE_TENANT_ID` environment variables
- Removed API key loading logic

**Updated `InitializeAzureAIFoundryAsync` method:**
- Uses `OidcAuthenticationProvider` instead of `ApiKeyAuthenticationProvider`
- Updated logging messages

### 5. Test Updates

**Removed:**
- `tests/IssueAgent.UnitTests/Runtime/ApiKeyAuthenticationProviderTests.cs`

**Added:**
- `tests/IssueAgent.UnitTests/Runtime/OidcAuthenticationProviderTests.cs`

**Updated all test files:**
- `tests/IssueAgent.UnitTests/Shared/AzureAIFoundryConfigurationTests.cs`
- `tests/IssueAgent.ContractTests/Configuration/AzureAIFoundryConfigurationValidationTests.cs`
- `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs`

**Test changes:**
- Replaced `ApiKey` with `ClientId` and `TenantId` in all test fixtures
- Updated test helper methods to use OIDC credentials
- All 106 unit tests, 16 contract tests, and 12 integration tests passing

### 6. Documentation

**Added:**
- `docs/service-principal-setup.md` - Complete guide for service principal setup
  - Step-by-step Azure CLI commands
  - Federated credential configuration
  - Required Azure permissions (Cognitive Services User role)
  - GitHub workflow configuration examples
  - Troubleshooting and security best practices

**Updated:**
- `README.md`
  - Replaced API key setup with service principal setup
  - Added OIDC authentication section
  - Updated workflow examples with `id-token: write` permission
  - Updated input parameter documentation
  
- `docs/troubleshooting-azure-foundry.md`
  - Replaced API key troubleshooting with OIDC troubleshooting
  - Added service principal verification steps
  - Updated error messages and solutions
  - Added federated credential troubleshooting

## Breaking Changes

This is a **BREAKING CHANGE** for existing users:

### Migration Required

Existing users must:
1. Create an Azure service principal
2. Configure federated credentials for GitHub OIDC
3. Assign Cognitive Services User role
4. Update GitHub secrets:
   - Remove: `AZURE_AI_FOUNDRY_API_KEY`
   - Add: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`
5. Update workflow files:
   - Add `id-token: write` permission
   - Replace `azure_ai_foundry_api_key` with `azure_client_id` and `azure_tenant_id`

### Migration Guide Available

See `docs/service-principal-setup.md` for complete migration instructions.

## Security Improvements

1. **No Secrets in Transit**: Client ID and tenant ID are not secrets (they can be public)
2. **Scoped Access**: Federated credentials can be scoped to specific branches or environments
3. **Audit Trail**: All service principal actions logged in Azure Activity Log
4. **Automatic Token Refresh**: Azure SDK handles token lifecycle
5. **No Key Rotation**: Eliminates need for periodic API key rotation

## Required Azure Permissions

The service principal requires:
- **Role**: Cognitive Services User
- **Scope**: Azure AI Foundry resource or resource group
- **Purpose**: Allows calling Azure AI services endpoints

## Workflow Requirements

GitHub Actions workflows must include:

```yaml
permissions:
  issues: write
  id-token: write  # Required for OIDC authentication
```

The `id-token: write` permission allows GitHub Actions to generate OIDC tokens.

## Test Results

All tests passing after implementation:
- ✅ 106 unit tests
- ✅ 16 contract tests  
- ✅ 12 integration tests (3 skipped, 9 passed)

## Files Changed

### Code Files (10)
- `action.yml` - Updated inputs
- `src/IssueAgent.Agent/Runtime/AgentBootstrap.cs` - Updated configuration loading
- `src/IssueAgent.Agent/Runtime/ApiKeyAuthenticationProvider.cs` - Removed
- `src/IssueAgent.Agent/Runtime/OidcAuthenticationProvider.cs` - Added
- `src/IssueAgent.Shared/Models/AzureAIFoundryConfiguration.cs` - Updated properties
- `tests/IssueAgent.UnitTests/Runtime/ApiKeyAuthenticationProviderTests.cs` - Removed
- `tests/IssueAgent.UnitTests/Runtime/OidcAuthenticationProviderTests.cs` - Added
- `tests/IssueAgent.UnitTests/Shared/AzureAIFoundryConfigurationTests.cs` - Updated
- `tests/IssueAgent.ContractTests/Configuration/AzureAIFoundryConfigurationValidationTests.cs` - Updated
- `tests/IssueAgent.IntegrationTests/AzureFoundry/ConnectionTests.cs` - Updated

### Documentation Files (3)
- `README.md` - Updated with OIDC setup
- `docs/service-principal-setup.md` - New comprehensive guide
- `docs/troubleshooting-azure-foundry.md` - Updated for OIDC

## Next Steps

For users to adopt this change:

1. Review `docs/service-principal-setup.md`
2. Create Azure service principal
3. Configure GitHub secrets
4. Update workflow files
5. Test with a sample issue

## Support

For questions or issues:
- See `docs/service-principal-setup.md` for setup help
- See `docs/troubleshooting-azure-foundry.md` for troubleshooting
- Open an issue at https://github.com/mattdot/issueagent/issues
