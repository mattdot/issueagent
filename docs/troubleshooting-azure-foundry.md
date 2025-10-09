# Troubleshooting: Azure AI Foundry Connectivity

This guide helps resolve common issues when connecting Issue Agent to Azure AI Foundry.

## Table of Contents

- [Connection Errors](#connection-errors)
- [Authentication Failures](#authentication-failures)
- [Configuration Issues](#configuration-issues)
- [Performance Problems](#performance-problems)
- [Validation Errors](#validation-errors)

---

## Connection Errors

### Error: "Connection to Azure AI Foundry timed out after 30 seconds"

**Category**: `NetworkTimeout`

**Cause**: The action cannot reach the Azure AI Foundry endpoint within the timeout period.

**Solutions**:

1. **Verify endpoint URL**:
   ```bash
   # Check if endpoint is reachable
   curl -I https://your-resource.services.ai.azure.com/api/projects/your-project
   ```

2. **Check network connectivity**:
   - Ensure GitHub Actions runner can access `*.services.ai.azure.com`
   - Verify no firewall rules blocking Azure AI Foundry
   - For self-hosted runners, check corporate proxy settings

3. **Azure service status**:
   - Check [Azure status page](https://status.azure.com/)
   - Verify your Azure AI Foundry project is running

4. **Increase timeout** (if needed):
   ```yaml
   # Not currently configurable - fixed at 30 seconds
   # Contact maintainers if this is insufficient
   ```

---

## Authentication Failures

### Error: "Unauthorized. Access token is missing, invalid, audience is incorrect (https://ai.azure.com/), or have expired"

**Category**: `AuthenticationFailure` / `UnexpectedError`

**Cause**: The Azure OIDC authentication failed or the service principal doesn't have proper permissions.

**Solutions**:

1. **Ensure you're using the latest version**:
   ```yaml
   uses: mattdot/issueagent@main  # or @v1.1.0 or later
   ```

2. **Verify OIDC inputs are passed correctly**:
   ```yaml
   permissions:
     id-token: write  # Required for OIDC
     issues: read
   with:
     azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
     azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
     azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
   ```

3. **Verify service principal setup**:
   - Check that the federated credential is configured correctly
   - Ensure the subject matches your repository: `repo:YOUR_ORG/YOUR_REPO:ref:refs/heads/main`
   - Verify the service principal has "Cognitive Services User" role

4. **Check workflow permissions**:
   - Ensure `id-token: write` permission is set in the workflow
   - This is required for GitHub Actions to generate OIDC tokens

---

### Error: "Authentication failed. Verify the API key has access to the endpoint"

**Category**: `AuthenticationFailure`

**Cause**: The service principal credentials are invalid or don't have access to the Azure AI Foundry resource.

**Solutions**:

1. **Verify service principal has correct role**:
   ```bash
   # Check role assignments
   az role assignment list \
     --assignee $CLIENT_ID \
     --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
     --output table
   ```

2. **Verify federated credential configuration**:
   ```bash
   # List federated credentials
   az ad app federated-credential list --id $CLIENT_ID
   
   # Check that the subject matches your repo
   # Should be: repo:YOUR_ORG/YOUR_REPO:ref:refs/heads/BRANCH
   ```

3. **Verify secret configuration**:
   ```yaml
   # Ensure secrets are passed correctly
   with:
     azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
     azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
   ```

4. **Re-assign role if needed**:
   ```bash
   # Assign Cognitive Services User role
   az role assignment create \
     --role "Cognitive Services User" \
     --assignee $CLIENT_ID \
     --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP
   ```

---

## Configuration Issues

### Error: "Azure AI Foundry endpoint is required"

**Category**: `MissingConfiguration`

**Cause**: No endpoint was provided via input parameter or environment variable.

**Solutions**:

1. **Add endpoint to workflow**:
   ```yaml
   with:
     azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
   ```

2. **Or set environment variable**:
   ```yaml
   env:
     AZURE_AI_FOUNDRY_ENDPOINT: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
   ```

3. **Verify secret exists**:
   - Go to repository Settings → Secrets and variables → Actions
   - Ensure `AZURE_AI_FOUNDRY_ENDPOINT` is defined

---

### Error: "Azure client ID is required when endpoint is provided"

**Category**: `MissingConfiguration`

**Cause**: No client ID was provided via input parameter or environment variable.

**Solutions**:

1. **Add client ID to workflow**:
   ```yaml
   with:
     azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
   ```

2. **Verify secret exists**:
   - Go to repository Settings → Secrets and variables → Actions
   - Ensure `AZURE_CLIENT_ID` is defined with your service principal client ID

---

### Error: "Azure tenant ID is required when endpoint is provided"

**Category**: `MissingConfiguration`

**Cause**: No tenant ID was provided via input parameter or environment variable.

**Solutions**:

1. **Add tenant ID to workflow**:
   ```yaml
   with:
     azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
   ```

2. **Verify secret exists**:
   - Go to repository Settings → Secrets and variables → Actions
   - Ensure `AZURE_TENANT_ID` is defined with your Azure tenant ID

---

### Error: "Azure AI Foundry endpoint must end with '.services.ai.azure.com/api/projects/<project>'"

**Category**: `InvalidConfiguration`

**Cause**: Endpoint URL is malformed or incomplete.

**Solutions**:

1. **Get correct endpoint format**:
   - Azure AI Foundry portal → Settings → Overview
   - Copy the full "Project endpoint" URL
   - Format: `https://<resource>.services.ai.azure.com/api/projects/<project-name>`

2. **Common mistakes**:
   ```bash
   # ❌ WRONG - Missing /api/projects/<project>
   https://mdfoundry.services.ai.azure.com/
   
   # ❌ WRONG - Cognitive Services endpoint (not AI Foundry)
   https://mdfoundry.cognitiveservices.azure.com/openai/...
   
   # ✅ CORRECT - Full AI Foundry project endpoint
   https://mdfoundry.services.ai.azure.com/api/projects/my-project
   ```

3. **Validate URL**:
   ```bash
   # Test endpoint format
   echo "$AZURE_AI_FOUNDRY_ENDPOINT" | grep -E '^https://.*\.services\.ai\.azure\.com/api/projects/.*$'
   ```

---

### Error: "Azure AI Foundry API key must be at least 32 characters"

**Category**: `InvalidConfiguration`

**Cause**: The API key is too short or truncated.

**Solutions**:

1. **Check key length**:
   ```bash
   echo -n "$AZURE_AI_FOUNDRY_API_KEY" | wc -c
   # Should be at least 32 characters (typically 84)
   ```

2. **Verify no whitespace**:
   ```bash
   # Trim whitespace when copying
   export AZURE_AI_FOUNDRY_API_KEY=$(echo "$AZURE_AI_FOUNDRY_API_KEY" | xargs)
   ```

3. **Regenerate if needed**:
   - Azure AI Foundry portal → Settings → Keys
   - Generate new key
   - Copy entire key value

---

## Performance Problems

### Warning: "Connection took longer than target (3000ms)"

**Cause**: Connection succeeded but took longer than the performance target.

**Impact**: This is a warning, not an error. The action still works.

**Solutions**:

1. **Check network latency**:
   ```bash
   # Test latency to Azure
   ping your-resource.services.ai.azure.com
   ```

2. **For self-hosted runners**:
   - Ensure runner has good network connectivity to Azure
   - Consider moving runner closer to Azure region
   - Check for proxy/firewall delays

3. **Azure region**:
   - Verify your Azure AI Foundry project is in a nearby region
   - Consider creating project in region closer to GitHub Actions runners

---

## Validation Errors

### Error: "Model deployment '<name>' not found in Azure AI Foundry project"

**Category**: `ModelNotFound`

**Cause**: The specified model deployment doesn't exist in your project.

**Solutions**:

1. **List available deployments**:
   - Azure AI Foundry portal → Deployments
   - Note the exact deployment name

2. **Update workflow**:
   ```yaml
   with:
     azure_foundry_model_deployment: gpt-4o-mini  # Match exact name
   ```

3. **Create deployment if needed**:
   - Azure AI Foundry portal → Deployments → Create deployment
   - Choose a model (e.g., gpt-4o-mini, gpt-4)
   - Deploy to your project

---

### Error: "API version '<version>' is not supported"

**Category**: `ApiVersionUnsupported`

**Cause**: The specified API version is deprecated or doesn't exist.

**Solutions**:

1. **Use latest stable version**:
   ```yaml
   with:
     azure_foundry_api_version: "2025-04-01-preview"  # Current default
   ```

2. **Check supported versions**:
   - See [Azure OpenAI API versioning](https://learn.microsoft.com/en-us/azure/ai-services/openai/api-version-deprecation)

3. **Omit parameter to use default**:
   ```yaml
   # Default version is always set to latest stable
   with:
     azure_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
     azure_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
     # azure_foundry_api_version not needed - uses default
   ```

---

## Debugging Tips

### Enable Detailed Logging

GitHub Actions logs include:
- Connection start with endpoint suffix
- Connection duration in milliseconds
- Success/failure with error category
- API keys are automatically redacted

### Test Connection Locally

Use the validation script:

```bash
# Set environment variables
export AZURE_AI_FOUNDRY_ENDPOINT="https://your-resource.services.ai.azure.com/api/projects/your-project"
export AZURE_CLIENT_ID="your-client-id"
export AZURE_TENANT_ID="your-tenant-id"

# Run validator (if available)
# ./validate-connection.sh
```

Expected output:
```
✅ SUCCESS: Connected to Azure AI Foundry
   Endpoint: ...your-resource
   Duration: 83ms
```

### Common Log Messages

**Success**:
```
Azure AI Foundry connection established in 83ms to endpoint ...mdfoundry
```

**Missing Configuration**:
```
Azure AI Foundry not configured - skipping initialization
```

**Connection Failure**:
```
Azure AI Foundry connection failed after 125ms: Authentication failed. 
Verify the service principal has Cognitive Services User role. (Category: AuthenticationFailure)
```

---

## Getting Help

If you're still experiencing issues:

1. **Check workflow logs**:
   - GitHub Actions → Your workflow run → Logs
   - Look for "Azure AI Foundry" messages
   - Note the error category

2. **Verify configuration**:
   ```bash
   # Test locally with the validation script
   ./validate-connection.sh
   ```

3. **Open an issue**:
   - [GitHub Issues](https://github.com/mattdot/issueagent/issues)
   - Include:
     - Error message and category
     - Workflow YAML (with secrets redacted)
     - Connection duration from logs
     - Azure region
     - Service principal configuration (client ID and tenant ID only, no secrets)

4. **Check Azure status**:
   - [Azure AI Foundry status](https://azure.status.microsoft)
   - [Azure service health](https://portal.azure.com/#view/Microsoft_Azure_Health/AzureHealthBrowseBlade)

---

## Related Documentation

- [Service Principal Setup Guide](service-principal-setup.md)
- [Azure AI Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/)
- [GitHub Actions OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [Agent Framework Quickstart](https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start)
- [Main README](../README.md)
- [Issue Context Runbook](operations/issue-context-runbook.md)
