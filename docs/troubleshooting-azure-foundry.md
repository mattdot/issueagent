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

**Cause**: The Azure AI Foundry credentials were not loaded properly by the Docker container. This was a known issue in versions prior to the fix in October 2025.

**Solutions**:

1. **Ensure you're using the latest version**:
   ```yaml
   uses: mattdot/issueagent@main  # or @v1.1.0 or later
   ```

2. **Verify inputs are passed correctly**:
   ```yaml
   with:
     azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
     azure_ai_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
   ```

3. **Historical note**: In versions before October 2025, there was a mismatch between the GitHub Actions input variable names (which created `INPUT_AZURE_AI_FOUNDRY_*` environment variables) and the C# code (which looked for `INPUT_AZURE_FOUNDRY_*` without the `_AI_` part). This has been fixed. If you're still experiencing this issue, ensure you're on the latest version.

---

### Error: "Authentication to Azure AI Foundry failed. Verify API key is valid and not expired"

**Category**: `AuthenticationFailure`

**Cause**: The API key is invalid, expired, or doesn't have access to the endpoint.

**Solutions**:

1. **Regenerate API key**:
   - Go to Azure AI Foundry portal
   - Navigate to Settings → Keys and Endpoints
   - Regenerate the key
   - Update GitHub Secret `AZURE_AI_FOUNDRY_API_KEY`

2. **Verify key format**:
   ```bash
   # API keys should be 84 characters (typically)
   echo -n "$AZURE_AI_FOUNDRY_API_KEY" | wc -c
   ```

3. **Check key permissions**:
   - Ensure the API key has access to the specific project
   - Verify RBAC permissions in Azure portal

4. **Verify secret configuration**:
   ```yaml
   # Ensure secret is passed correctly
   with:
     azure_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
   
   # NOT like this (hardcoded):
   # azure_foundry_api_key: "abcd1234..."  # WRONG!
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
     azure_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
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
export ISSUE_AGENT_ENDPOINT="https://your-resource.services.ai.azure.com/api/projects/your-project"
export ISSUE_AGENT_KEY="your-api-key"

# Run validator
./validate-connection.sh
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
Verify the API key has access to the endpoint. (Category: AuthenticationFailure)
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

4. **Check Azure status**:
   - [Azure AI Foundry status](https://azure.status.microsoft)
   - [Azure service health](https://portal.azure.com/#view/Microsoft_Azure_Health/AzureHealthBrowseBlade)

---

## Related Documentation

- [Azure AI Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/)
- [Agent Framework Quickstart](https://learn.microsoft.com/en-us/agent-framework/tutorials/quick-start)
- [Main README](../README.md)
- [Issue Context Runbook](operations/issue-context-runbook.md)
