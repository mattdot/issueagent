# Azure Service Principal Setup for Issue Agent

This guide walks through creating and configuring an Azure service principal for Issue Agent to authenticate with Azure AI Foundry using OIDC (OpenID Connect).

## Prerequisites

- Azure subscription with access to create service principals
- Azure CLI installed (`az` command)
- An Azure AI Foundry project created
- GitHub repository with admin access

## Step 1: Gather Azure Information

Before creating the service principal, collect the following information:

```bash
# Login to Azure
az login

# Set your subscription (if you have multiple)
az account set --subscription "YOUR_SUBSCRIPTION_NAME_OR_ID"

# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo "Subscription ID: $SUBSCRIPTION_ID"

# Get your tenant ID
TENANT_ID=$(az account show --query tenantId -o tsv)
echo "Tenant ID: $TENANT_ID"

# Get your Azure AI Foundry resource group
RESOURCE_GROUP="your-ai-foundry-resource-group"
echo "Resource Group: $RESOURCE_GROUP"
```

## Step 2: Create Azure AD Application

Create an Azure AD application registration that will represent the GitHub Actions workflow:

```bash
# Set application name
APP_NAME="github-issueagent-sp"

# Create the application
az ad app create --display-name $APP_NAME

# Get the application (client) ID
CLIENT_ID=$(az ad app list --display-name $APP_NAME --query "[0].appId" -o tsv)
echo "Client ID: $CLIENT_ID"
```

## Step 3: Configure Federated Credential for GitHub OIDC

Configure the application to trust GitHub Actions OIDC tokens:

```bash
# Replace with your GitHub organization and repository
GITHUB_ORG="your-org"
GITHUB_REPO="your-repo"
BRANCH="main"  # or your default branch

# Create federated credential
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters "{
    \"name\": \"github-actions-federated\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/${BRANCH}\",
    \"audiences\": [\"api://AzureADTokenExchange\"],
    \"description\": \"GitHub Actions OIDC for Issue Agent\"
  }"
```

### Alternative: Allow Any Branch

If you want to allow the action to run from any branch:

```bash
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters "{
    \"name\": \"github-actions-federated-all-branches\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:ref:refs/heads/*\",
    \"audiences\": [\"api://AzureADTokenExchange\"],
    \"description\": \"GitHub Actions OIDC for Issue Agent (all branches)\"
  }"
```

### Alternative: Allow Pull Requests

To allow the action to run on pull requests:

```bash
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters "{
    \"name\": \"github-actions-federated-prs\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_ORG}/${GITHUB_REPO}:pull_request\",
    \"audiences\": [\"api://AzureADTokenExchange\"],
    \"description\": \"GitHub Actions OIDC for Issue Agent (PRs)\"
  }"
```

## Step 4: Assign Azure Permissions

Grant the service principal access to Azure AI Foundry:

```bash
# Assign Cognitive Services User role
# This allows calling Azure AI services APIs
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $CLIENT_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP

# Verify the role assignment
az role assignment list \
  --assignee $CLIENT_ID \
  --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --output table
```

### Required Azure Roles

| Role | Scope | Purpose |
|------|-------|---------|
| **Cognitive Services User** | Resource Group or AI Foundry Resource | Allows the service principal to call Azure AI services endpoints and use deployed models |

### Optional: More Granular Permissions

For better security, you can assign permissions at the resource level instead of resource group:

```bash
# Get the Azure AI Foundry resource ID
AI_FOUNDRY_RESOURCE_ID=$(az resource list \
  --resource-group $RESOURCE_GROUP \
  --resource-type "Microsoft.CognitiveServices/accounts" \
  --query "[0].id" -o tsv)

# Assign role at resource level
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $CLIENT_ID \
  --scope $AI_FOUNDRY_RESOURCE_ID
```

## Step 5: Configure GitHub Secrets

Add the following secrets to your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Add the following repository secrets:

| Secret Name | Value | Example |
|-------------|-------|---------|
| `AZURE_AI_FOUNDRY_ENDPOINT` | Your Azure AI Foundry project endpoint | `https://my-project.services.ai.azure.com/api/projects/my-project` |
| `AZURE_CLIENT_ID` | The client ID from Step 2 | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | The tenant ID from Step 1 | `87654321-4321-4321-4321-210987654321` |

## Step 6: Update GitHub Workflow

Create or update your GitHub Actions workflow file (`.github/workflows/issue-agent.yml`):

```yaml
name: Issue Agent
on:
  issues:
    types: [opened, reopened]
  issue_comment:
    types: [created]

jobs:
  analyze-issue:
    runs-on: ubuntu-latest
    permissions:
      issues: write
      id-token: write  # Required for OIDC authentication
    steps:
      - name: Analyze Issue
        uses: mattdot/issueagent@v1
        with:
          github_token: ${{ github.token }}
          azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
          azure_client_id: ${{ secrets.AZURE_CLIENT_ID }}
          azure_tenant_id: ${{ secrets.AZURE_TENANT_ID }}
          azure_ai_foundry_model_deployment: gpt-4o-mini
```

**Important:** The `id-token: write` permission is **required** for GitHub Actions to generate OIDC tokens.

## Verification

To verify your setup is working:

1. Create a test issue in your repository
2. Check the GitHub Actions logs for authentication success
3. Look for log messages like:
   ```
   Creating OIDC authentication provider with client ID and tenant ID
   Creating Azure AI Foundry client with endpoint: ...
   Azure AI Foundry connection established
   ```

## Troubleshooting

### Authentication Failed

**Error:** `Authentication failed. Verify the API key has access to the endpoint.`

**Solutions:**
- Verify the `CLIENT_ID` and `TENANT_ID` are correct
- Check that the federated credential subject matches your repository and branch
- Ensure the `id-token: write` permission is set in your workflow
- Verify the service principal has the Cognitive Services User role

### Missing Configuration

**Error:** `Azure client ID is required when endpoint is provided.`

**Solutions:**
- Ensure `AZURE_CLIENT_ID` secret is set in GitHub
- Check that you're passing `azure_client_id` in the workflow

### Connection Timeout

**Error:** `Connection attempt timed out after 30 seconds.`

**Solutions:**
- Verify the Azure AI Foundry endpoint URL is correct
- Check network connectivity from GitHub Actions to Azure
- Ensure the Azure AI Foundry resource is running and accessible

## Security Best Practices

1. **Use Resource-Scoped Permissions**: Grant the service principal access only to the specific Azure AI Foundry resource, not the entire resource group
2. **Limit Branch Access**: Configure federated credentials for specific branches or environments
3. **Regular Rotation**: While OIDC doesn't use secrets, periodically review and rotate federated credentials
4. **Monitor Usage**: Enable Azure Activity Log to monitor service principal usage
5. **Principle of Least Privilege**: Only grant the minimum required role (Cognitive Services User)

## Additional Resources

- [GitHub Actions OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [Azure Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [Azure AI Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-services/)
