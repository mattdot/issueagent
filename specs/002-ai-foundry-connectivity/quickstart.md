# Quickstart: Azure AI Foundry Connectivity

## Overview
This quickstart guide validates the Azure AI Foundry connectivity feature by walking through a complete end-to-end scenario from configuration to successful connection.

## Prerequisites

### Azure Resources
1. Azure AI Foundry project created
   - Navigate to [Azure AI Foundry Portal](https://ai.azure.com)
   - Create or select an existing project
   - Note the project endpoint URL (format: `https://<resource>.services.ai.azure.com/api/projects/<project>`)

2. Model deployment configured
   - In Azure AI Foundry portal, navigate to "Models + endpoints"
   - Deploy a model (e.g., `gpt-5-mini`)
   - Note the deployment name

3. API key obtained
   - In Azure AI Foundry portal, navigate to project settings
   - Under "Keys and Endpoint", copy an API key

### GitHub Repository Setup
1. Fork or create a test repository
2. Navigate to Settings → Secrets and variables → Actions
3. Create the following secrets:
   - `AZURE_FOUNDRY_ENDPOINT`: Your project endpoint URL
   - `AZURE_FOUNDRY_API_KEY`: Your API key
   - `AZURE_FOUNDRY_MODEL_DEPLOYMENT`: Your model deployment name

## Scenario: Configure and Test Connection

### Step 1: Create Test Workflow

Create `.github/workflows/test-azure-foundry.yml`:

```yaml
name: Test Azure AI Foundry Connection

on:
  issues:
    types: [opened]
  workflow_dispatch:

jobs:
  test-connection:
    runs-on: ubuntu-latest
    permissions:
      issues: write
      contents: read

    steps:
      - name: Test IssueAgent with Azure AI Foundry
        uses: mattdot/issueagent@main  # Update to actual version after release
        with:
          azure_foundry_endpoint: ${{ secrets.AZURE_FOUNDRY_ENDPOINT }}
          azure_foundry_api_key: ${{ secrets.AZURE_FOUNDRY_API_KEY }}
          azure_foundry_model_deployment: 'gpt-5-mini'  # Optional, defaults to gpt-5-mini
          # azure_foundry_api_version: '2025-04-01-preview'  # Optional, uses default if not specified
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Step 2: Trigger Test

**Option A: Create a Test Issue**
1. Navigate to your repository's Issues tab
2. Click "New Issue"
3. Title: "Test Azure AI Foundry Connection"
4. Body: "This issue tests the IssueAgent Azure AI Foundry connectivity feature."
5. Click "Submit new issue"

**Option B: Manual Workflow Dispatch**
1. Navigate to Actions → "Test Azure AI Foundry Connection"
2. Click "Run workflow" → "Run workflow"

### Step 3: Verify Success

#### Expected Workflow Output
```
Initializing IssueAgent...
Configuring Azure AI Foundry connection...
Connecting to Azure AI Foundry endpoint ending in ...my-project
Azure AI Foundry connection successful (duration: 847ms)
Model deployment 'gpt-5-mini' verified
Processing issue context...
```

#### Success Criteria
- ✅ Workflow completes without errors (green checkmark)
- ✅ Logs show "Azure AI Foundry connection successful"
- ✅ Connection duration under 3 seconds
- ✅ No credential information visible in logs
- ✅ Issue receives agent response (if full issue processing enabled)

### Step 4: Verify Configuration Sources

Test alternative configuration method using environment variables:

Create `.github/workflows/test-azure-foundry-env.yml`:

```yaml
name: Test Azure AI Foundry (Environment Variables)

on:
  workflow_dispatch:

jobs:
  test-connection:
    runs-on: ubuntu-latest
    permissions:
      issues: write
      contents: read

    steps:
      - name: Test IssueAgent with Azure AI Foundry (Env Vars)
        uses: mattdot/issueagent@main
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          AZURE_AI_FOUNDRY_ENDPOINT: ${{ secrets.AZURE_FOUNDRY_ENDPOINT }}
          AZURE_AI_FOUNDRY_API_KEY: ${{ secrets.AZURE_FOUNDRY_API_KEY }}
          AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT: 'gpt-5-mini'
```

#### Success Criteria
- ✅ Configuration loaded from environment variables
- ✅ Connection successful without explicit action inputs
- ✅ Logs show configuration source: "Loaded from environment variables"

## Error Scenarios

### Test 1: Missing Endpoint

**Setup**: Remove `azure_foundry_endpoint` from workflow

**Expected Behavior**:
- ❌ Workflow fails with clear error
- Error message: "Azure AI Foundry endpoint is required. Provide 'azure_foundry_endpoint' input or set AZURE_AI_FOUNDRY_ENDPOINT environment variable."
- Exit code: 1

### Test 2: Invalid API Key

**Setup**: Use incorrect API key value

**Expected Behavior**:
- ❌ Workflow fails within 30 seconds
- Error message: "Authentication to Azure AI Foundry failed. Verify API key is valid and not expired. Check Azure portal under 'Keys and Endpoint' for your AI Foundry project."
- No retry attempts logged
- Exit code: 1

### Test 3: Invalid Endpoint Format

**Setup**: Use malformed endpoint URL
```yaml
azure_foundry_endpoint: "http://example.com/wrong"
```

**Expected Behavior**:
- ❌ Workflow fails immediately (validation error)
- Error message: "Azure AI Foundry endpoint must be a valid HTTPS URL in format: https://<resource>.services.ai.azure.com/api/projects/<project>. Received: http://example.com/wrong"
- Exit code: 1

### Test 4: Model Deployment Not Found

**Setup**: Use non-existent model deployment name
```yaml
azure_foundry_model_deployment: "nonexistent-model"
```

**Expected Behavior**:
- ❌ Workflow fails after connection attempt
- Error message: "Model deployment 'nonexistent-model' not found in Azure AI Foundry project. Verify deployment name in Azure AI Foundry portal under 'Models + endpoints'."
- Exit code: 1

### Test 5: Network Timeout

**Setup**: Use unreachable endpoint
```yaml
azure_foundry_endpoint: "https://unreachable.services.ai.azure.com/api/projects/test"
```

**Expected Behavior**:
- ❌ Workflow fails after 30 seconds
- Error message: "Connection to Azure AI Foundry timed out after 30 seconds. Verify endpoint URL is correct and network connectivity allows access to *.services.ai.azure.com."
- No retry attempts logged
- Exit code: 1

## Validation Checklist

After completing the quickstart, verify the following:

### Functional Requirements
- [ ] **FR-001**: Action accepts Azure AI Foundry endpoint URL as input
- [ ] **FR-002**: Action accepts API key securely (from GitHub secrets)
- [ ] **FR-003**: Action accepts API version with default value
- [ ] **FR-004**: Action accepts model deployment name as input
- [ ] **FR-005**: Connection established successfully with valid credentials
- [ ] **FR-006**: All required parameters validated before connection attempt
- [ ] **FR-007**: Authentication failures reported with clear error messages
- [ ] **FR-008**: Configuration works via GitHub Actions inputs
- [ ] **FR-009**: Configuration works via environment variables
- [ ] **FR-010**: API keys stored and passed securely via GitHub secrets
- [ ] **FR-013**: Connection verified during startup with fail-fast behavior
- [ ] **FR-014**: 30-second timeout enforced (no retries)
- [ ] **FR-015**: Connection attempts and outcomes logged
- [ ] **FR-016**: API request/response details NOT logged

### Constitutional Principles
- [ ] **Security**: No credentials visible in workflow logs
- [ ] **Minimum Viable Delivery**: Connection feature works end-to-end
- [ ] **Performance**: Connection completed in <3 seconds
- [ ] **Human-Centered**: Error messages provide actionable guidance
- [ ] **Extensibility**: Configuration supports both inputs and environment variables

### Edge Cases
- [ ] Connection timeout after 30 seconds (no retry)
- [ ] Invalid API key fails with authentication error
- [ ] Missing model deployment fails with descriptive error
- [ ] Malformed endpoint URL fails validation immediately
- [ ] API version validation accepts YYYY-MM-DD format

## Troubleshooting

### Issue: "Azure AI Foundry endpoint is required"
**Solution**: Ensure `azure_foundry_endpoint` input is provided or `AZURE_AI_FOUNDRY_ENDPOINT` environment variable is set.

### Issue: "Authentication to Azure AI Foundry failed"
**Solution**:
1. Verify API key is copied correctly (no extra spaces)
2. Check API key is not expired in Azure portal
3. Confirm API key has access to the specified project

### Issue: "Connection timed out after 30 seconds"
**Solution**:
1. Verify endpoint URL is correct
2. Check GitHub Actions runner can access `*.services.ai.azure.com`
3. Confirm Azure AI Foundry service is operational

### Issue: Credentials Visible in Logs
**Solution**:
- Ensure credentials are passed via GitHub secrets (not hardcoded)
- Verify RedactionMiddleware is active in agent bootstrap
- Report security issue if credentials leak despite secrets usage

## Next Steps

After successfully completing the quickstart:

1. **Integration**: Integrate Azure AI Foundry connection with existing issue context processing
2. **Testing**: Run full integration test suite with real Azure AI Foundry responses
3. **Documentation**: Update main README with Azure AI Foundry configuration instructions
4. **Examples**: Add sample workflows to repository

## Success Metrics

Track the following metrics after deployment:

- **Connection Success Rate**: >99% for valid configurations
- **Connection Latency P95**: <1.5 seconds
- **Error Message Clarity**: User can resolve issues without contacting support
- **Configuration Adoption**: Percentage of users successfully configuring on first attempt

---

**Quickstart Complete**: End-to-end validation path defined with success criteria and error scenarios.
