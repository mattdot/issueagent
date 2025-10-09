# Scripts

This directory contains utility scripts for setting up and managing the IssueAgent GitHub Action.

## setup-entra-app.sh

A script to automate the creation of an Entra (Azure AD) app registration with service principal and OIDC configuration for GitHub Actions workflows.

### Purpose

This script simplifies the setup process for using OIDC (OpenID Connect) authentication with the IssueAgent GitHub Action. Instead of managing API keys, OIDC allows GitHub Actions workflows to authenticate to Azure using workload identity federation, which is more secure and doesn't require storing long-lived credentials.

### Prerequisites

- **Azure CLI** (`az`) installed and authenticated (`az login`)
- **GitHub CLI** (`gh`) installed and authenticated (`gh auth login`)
- Permissions to create app registrations in Azure AD
- Owner or Admin access to the GitHub repository

### Installation

#### Azure CLI
```bash
# macOS
brew install azure-cli

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Windows
# Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows
```

#### GitHub CLI
```bash
# macOS
brew install gh

# Linux
(type -p wget >/dev/null || (sudo apt update && sudo apt-get install wget -y)) \
&& sudo mkdir -p -m 755 /etc/apt/keyrings \
&& wget -qO- https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
&& sudo chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
&& echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
&& sudo apt update \
&& sudo apt install gh -y

# Windows
# Download from: https://cli.github.com/
```

### Usage

#### Basic Usage (Auto-detect repository)
```bash
./scripts/setup-entra-app.sh
```

#### Custom App Name and Repository
```bash
./scripts/setup-entra-app.sh --app-name my-issueagent --repo myorg/myrepo
```

#### Configure for Specific GitHub Environment
```bash
./scripts/setup-entra-app.sh --environment production
```

#### Use Specific Azure Subscription
```bash
./scripts/setup-entra-app.sh --subscription "12345678-1234-1234-1234-123456789012"
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| `-n, --app-name NAME` | Name for the Entra app registration | `issueagent-github-action` |
| `-r, --repo OWNER/REPO` | GitHub repository in format owner/repo | Auto-detected from git remote |
| `-e, --environment ENV` | GitHub environment name for OIDC | None (uses main branch) |
| `-s, --subscription ID` | Azure subscription ID | Current active subscription |
| `-h, --help` | Show help message | - |

### What the Script Does

1. **Creates an Entra (Azure AD) app registration** with the specified name
2. **Creates a service principal** for the app registration
3. **Configures federated credentials** for GitHub OIDC authentication:
   - Main branch workflow credential
   - Pull request workflow credential
4. **Outputs configuration values** needed for GitHub Actions

### Output

After successful execution, the script outputs:

- **Azure Client ID** (App ID)
- **Azure Tenant ID**
- **Azure Subscription ID**
- GitHub CLI commands to set these as repository secrets
- Example GitHub Actions workflow using OIDC

### Example Output

```
[SUCCESS] Setup complete!

╔════════════════════════════════════════════════════════════════════════════╗
║                     Configuration for GitHub Actions                      ║
╚════════════════════════════════════════════════════════════════════════════╝

Add the following secrets/variables to your GitHub repository:
Repository: mattdot/issueagent

Settings → Secrets and variables → Actions → Secrets:

  AZURE_CLIENT_ID: 12345678-1234-1234-1234-123456789012
  AZURE_TENANT_ID: 87654321-4321-4321-4321-210987654321
  AZURE_SUBSCRIPTION_ID: abcdefab-abcd-abcd-abcd-abcdefabcdef

You can set these using the GitHub CLI:

  gh secret set AZURE_CLIENT_ID --body "12345678-1234-1234-1234-123456789012" --repo mattdot/issueagent
  gh secret set AZURE_TENANT_ID --body "87654321-4321-4321-4321-210987654321" --repo mattdot/issueagent
  gh secret set AZURE_SUBSCRIPTION_ID --body "abcdefab-abcd-abcd-abcd-abcdefabcdef" --repo mattdot/issueagent
```

### Next Steps After Running the Script

1. **Set GitHub Secrets**: Use the provided `gh secret set` commands or manually add secrets in repository settings
2. **Grant Azure Permissions**: Assign appropriate roles to the service principal in Azure:
   ```bash
   # Example: Grant Cognitive Services User role
   az role assignment create \
     --assignee <APP_ID> \
     --role "Cognitive Services User" \
     --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP>/providers/Microsoft.CognitiveServices/accounts/<AI_FOUNDRY_RESOURCE>
   ```
3. **Update Workflow**: Modify your GitHub Actions workflow to use OIDC authentication

### Example GitHub Actions Workflow

```yaml
name: Issue Agent with Azure OIDC
on:
  issues:
    types: [opened, reopened]
  issue_comment:
    types: [created]

permissions:
  id-token: write      # Required for OIDC
  issues: read
  contents: read

jobs:
  issue-agent:
    runs-on: ubuntu-latest
    steps:
      - name: Azure Login via OIDC
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      
      - name: Run Issue Agent
        uses: mattdot/issueagent@main
        with:
          github_token: ${{ github.token }}
          azure_ai_foundry_endpoint: ${{ vars.ISSUE_AGENT_ENDPOINT }}
          # API key can be retrieved via Azure CLI after OIDC auth
```

### Troubleshooting

#### "Azure CLI (az) is not installed"
Install Azure CLI following the installation instructions above.

#### "Not logged in to Azure CLI"
Run `az login` and follow the authentication prompts.

#### "Not logged in to GitHub CLI"
Run `gh auth login` and follow the authentication prompts.

#### "Could not auto-detect GitHub repository"
Use the `--repo` option to specify the repository explicitly:
```bash
./scripts/setup-entra-app.sh --repo owner/repo
```

#### App registration already exists
The script will detect existing app registrations and service principals by name. If found, it will use the existing resources and only add missing federated credentials.

### Security Considerations

- **No Secrets in Code**: OIDC eliminates the need to store API keys as GitHub secrets
- **Short-lived Tokens**: GitHub Actions receives short-lived tokens from Azure AD
- **Scoped Access**: Federated credentials can be scoped to specific branches or environments
- **Audit Trail**: All authentication events are logged in Azure AD

### Learn More

- [Azure AD Workload Identity Federation](https://docs.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)
- [GitHub Actions OIDC](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
- [Azure Login Action](https://github.com/Azure/login)
