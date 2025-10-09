#!/usr/bin/env bash
set -euo pipefail

# Script to create an Entra (Azure AD) app registration with service principal
# and configure OIDC (federated credentials) for GitHub Actions workflows
#
# This script automates the setup required for the issue-agent GitHub Action
# to authenticate to Azure using OIDC/workload identity federation.
#
# Prerequisites:
#   - Azure CLI (az) installed and authenticated
#   - GitHub CLI (gh) installed and authenticated
#   - Appropriate permissions in Azure AD to create app registrations
#   - Owner or Admin access to the GitHub repository
#
# Usage:
#   ./scripts/setup-entra-app.sh [options]
#
# Options:
#   -n, --app-name NAME          Name for the Entra app registration (default: issueagent-github-action)
#   -r, --repo OWNER/REPO        GitHub repository in format owner/repo (auto-detected if not provided)
#   -e, --environment ENV        GitHub environment name for OIDC (optional, defaults to production workflows)
#   -s, --subscription ID        Azure subscription ID (uses current subscription if not provided)
#   -h, --help                   Show this help message
#
# Examples:
#   # Basic usage (auto-detect repository):
#   ./scripts/setup-entra-app.sh
#
#   # Specify custom app name and repository:
#   ./scripts/setup-entra-app.sh --app-name my-issueagent --repo myorg/myrepo
#
#   # Configure for a specific GitHub environment:
#   ./scripts/setup-entra-app.sh --environment production
#
# What this script does:
#   1. Creates an Entra (Azure AD) app registration
#   2. Creates a service principal for the app
#   3. Configures federated credentials for GitHub OIDC authentication
#   4. Outputs the required configuration values for GitHub secrets/variables

#==============================================================================
# Default Configuration
#==============================================================================

APP_NAME="issueagent-github-action"
GITHUB_REPO=""
GITHUB_ENVIRONMENT=""
AZURE_SUBSCRIPTION=""

#==============================================================================
# Helper Functions
#==============================================================================

usage() {
    cat <<EOF
Usage: $(basename "$0") [options]

Creates an Entra app registration with OIDC configuration for GitHub Actions.

Options:
  -n, --app-name NAME          Name for the Entra app registration (default: issueagent-github-action)
  -r, --repo OWNER/REPO        GitHub repository in format owner/repo (auto-detected if not provided)
  -e, --environment ENV        GitHub environment name for OIDC (optional)
  -s, --subscription ID        Azure subscription ID (uses current subscription if not provided)
  -h, --help                   Show this help message

Examples:
  $(basename "$0")
  $(basename "$0") --app-name my-issueagent --repo myorg/myrepo
  $(basename "$0") --environment production

EOF
}

log_info() {
    echo "[INFO] $*"
}

log_success() {
    echo "[SUCCESS] $*"
}

log_error() {
    echo "[ERROR] $*" >&2
}

log_warning() {
    echo "[WARNING] $*"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check for Azure CLI
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI (az) is not installed. Please install it from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    fi
    
    # Check for GitHub CLI
    if ! command -v gh &> /dev/null; then
        log_error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
        exit 1
    fi
    
    # Check Azure CLI login status
    if ! az account show &> /dev/null; then
        log_error "Not logged in to Azure CLI. Please run 'az login' first."
        exit 1
    fi
    
    # Check GitHub CLI login status
    if ! gh auth status &> /dev/null; then
        log_error "Not logged in to GitHub CLI. Please run 'gh auth login' first."
        exit 1
    fi
    
    log_success "All prerequisites met"
}

detect_github_repo() {
    if [[ -n "$GITHUB_REPO" ]]; then
        return
    fi
    
    log_info "Auto-detecting GitHub repository..."
    
    # Try to get repository from git remote
    if git rev-parse --git-dir > /dev/null 2>&1; then
        local remote_url
        remote_url=$(git config --get remote.origin.url 2>/dev/null || echo "")
        
        if [[ -n "$remote_url" ]]; then
            # Extract owner/repo from various GitHub URL formats
            if [[ "$remote_url" =~ github\.com[:/]([^/]+)/([^/.]+)(\.git)?$ ]]; then
                GITHUB_REPO="${BASH_REMATCH[1]}/${BASH_REMATCH[2]}"
                log_info "Detected repository: $GITHUB_REPO"
                return
            fi
        fi
    fi
    
    # Fallback: try gh repo view
    if GITHUB_REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner 2>/dev/null); then
        log_info "Detected repository: $GITHUB_REPO"
        return
    fi
    
    log_error "Could not auto-detect GitHub repository. Please provide it with --repo option."
    exit 1
}

get_azure_subscription() {
    if [[ -n "$AZURE_SUBSCRIPTION" ]]; then
        return
    fi
    
    log_info "Getting current Azure subscription..."
    AZURE_SUBSCRIPTION=$(az account show --query id -o tsv)
    log_info "Using subscription: $AZURE_SUBSCRIPTION"
}

#==============================================================================
# Main Setup Functions
#==============================================================================

create_app_registration() {
    log_info "Creating Entra app registration: $APP_NAME..."
    
    # Check if app already exists
    local existing_app_id
    existing_app_id=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv 2>/dev/null || echo "")
    
    if [[ -n "$existing_app_id" && "$existing_app_id" != "null" ]]; then
        log_warning "App registration '$APP_NAME' already exists with App ID: $existing_app_id"
        echo "$existing_app_id"
        return
    fi
    
    # Create the app registration
    local app_id
    app_id=$(az ad app create \
        --display-name "$APP_NAME" \
        --query appId \
        -o tsv)
    
    if [[ -z "$app_id" ]]; then
        log_error "Failed to create app registration"
        exit 1
    fi
    
    log_success "Created app registration with App ID: $app_id"
    echo "$app_id"
}

create_service_principal() {
    local app_id=$1
    
    log_info "Creating service principal for app ID: $app_id..."
    
    # Check if service principal already exists
    local existing_sp_id
    existing_sp_id=$(az ad sp list --filter "appId eq '$app_id'" --query "[0].id" -o tsv 2>/dev/null || echo "")
    
    if [[ -n "$existing_sp_id" && "$existing_sp_id" != "null" ]]; then
        log_warning "Service principal already exists with ID: $existing_sp_id"
        echo "$existing_sp_id"
        return
    fi
    
    # Create the service principal
    local sp_id
    sp_id=$(az ad sp create --id "$app_id" --query id -o tsv)
    
    if [[ -z "$sp_id" ]]; then
        log_error "Failed to create service principal"
        exit 1
    fi
    
    log_success "Created service principal with ID: $sp_id"
    echo "$sp_id"
}

configure_federated_credentials() {
    local app_id=$1
    local object_id=$2
    
    log_info "Configuring OIDC federated credentials for GitHub Actions..."
    
    # Build the subject identifier based on environment
    local subject
    if [[ -n "$GITHUB_ENVIRONMENT" ]]; then
        subject="repo:${GITHUB_REPO}:environment:${GITHUB_ENVIRONMENT}"
        local credential_name="${APP_NAME}-${GITHUB_ENVIRONMENT}"
    else
        # For workflows without environment restrictions (broader access)
        subject="repo:${GITHUB_REPO}:ref:refs/heads/main"
        local credential_name="${APP_NAME}-main-branch"
    fi
    
    log_info "Creating federated credential for subject: $subject"
    
    # Check if federated credential already exists
    local existing_cred
    existing_cred=$(az ad app federated-credential list \
        --id "$object_id" \
        --query "[?name=='$credential_name'].name" \
        -o tsv 2>/dev/null || echo "")
    
    if [[ -n "$existing_cred" ]]; then
        log_warning "Federated credential '$credential_name' already exists, skipping creation"
        return
    fi
    
    # Create federated credential
    az ad app federated-credential create \
        --id "$object_id" \
        --parameters "{
            \"name\": \"$credential_name\",
            \"issuer\": \"https://token.actions.githubusercontent.com\",
            \"subject\": \"$subject\",
            \"description\": \"OIDC for GitHub Actions workflow in $GITHUB_REPO\",
            \"audiences\": [
                \"api://AzureADTokenExchange\"
            ]
        }" > /dev/null
    
    log_success "Created federated credential: $credential_name"
    
    # Also create a pull request credential for PR workflows
    local pr_subject="repo:${GITHUB_REPO}:pull_request"
    local pr_credential_name="${APP_NAME}-pull-requests"
    
    existing_cred=$(az ad app federated-credential list \
        --id "$object_id" \
        --query "[?name=='$pr_credential_name'].name" \
        -o tsv 2>/dev/null || echo "")
    
    if [[ -z "$existing_cred" ]]; then
        log_info "Creating federated credential for pull requests..."
        az ad app federated-credential create \
            --id "$object_id" \
            --parameters "{
                \"name\": \"$pr_credential_name\",
                \"issuer\": \"https://token.actions.githubusercontent.com\",
                \"subject\": \"$pr_subject\",
                \"description\": \"OIDC for GitHub Actions pull request workflows in $GITHUB_REPO\",
                \"audiences\": [
                    \"api://AzureADTokenExchange\"
                ]
            }" > /dev/null
        log_success "Created pull request federated credential"
    else
        log_warning "Pull request federated credential already exists, skipping creation"
    fi
}

get_tenant_id() {
    az account show --query tenantId -o tsv
}

output_configuration() {
    local app_id=$1
    local tenant_id=$2
    
    log_success "Setup complete!"
    echo ""
    echo "╔════════════════════════════════════════════════════════════════════════════╗"
    echo "║                     Configuration for GitHub Actions                      ║"
    echo "╚════════════════════════════════════════════════════════════════════════════╝"
    echo ""
    echo "Add the following secrets/variables to your GitHub repository:"
    echo "Repository: $GITHUB_REPO"
    echo ""
    echo "Settings → Secrets and variables → Actions → Secrets:"
    echo ""
    echo "  AZURE_CLIENT_ID: $app_id"
    echo "  AZURE_TENANT_ID: $tenant_id"
    echo "  AZURE_SUBSCRIPTION_ID: $AZURE_SUBSCRIPTION"
    echo ""
    echo "You can set these using the GitHub CLI:"
    echo ""
    echo "  gh secret set AZURE_CLIENT_ID --body \"$app_id\" --repo $GITHUB_REPO"
    echo "  gh secret set AZURE_TENANT_ID --body \"$tenant_id\" --repo $GITHUB_REPO"
    echo "  gh secret set AZURE_SUBSCRIPTION_ID --body \"$AZURE_SUBSCRIPTION\" --repo $GITHUB_REPO"
    echo ""
    echo "────────────────────────────────────────────────────────────────────────────"
    echo ""
    echo "Example GitHub Actions workflow using OIDC authentication:"
    echo ""
    cat <<'WORKFLOW'
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
          # API key will be retrieved via Azure CLI after OIDC auth
WORKFLOW
    echo ""
    echo "════════════════════════════════════════════════════════════════════════════"
    echo ""
    echo "Next Steps:"
    echo "  1. Set the GitHub secrets using the commands above (or manually)"
    echo "  2. Grant the service principal appropriate permissions in Azure"
    echo "     (e.g., Cognitive Services User role on your Azure AI Foundry resource)"
    echo "  3. Update your GitHub Actions workflow to use OIDC authentication"
    echo ""
}

#==============================================================================
# Argument Parsing
#==============================================================================

parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -n|--app-name)
                APP_NAME="$2"
                shift 2
                ;;
            -r|--repo)
                GITHUB_REPO="$2"
                shift 2
                ;;
            -e|--environment)
                GITHUB_ENVIRONMENT="$2"
                shift 2
                ;;
            -s|--subscription)
                AZURE_SUBSCRIPTION="$2"
                shift 2
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                echo ""
                usage
                exit 1
                ;;
        esac
    done
}

#==============================================================================
# Main Execution
#==============================================================================

main() {
    parse_args "$@"
    
    log_info "Starting Entra app registration setup for GitHub Actions OIDC..."
    echo ""
    
    # Run prerequisite checks
    check_prerequisites
    
    # Detect or validate configuration
    detect_github_repo
    get_azure_subscription
    
    echo ""
    log_info "Configuration:"
    log_info "  App Name: $APP_NAME"
    log_info "  GitHub Repository: $GITHUB_REPO"
    log_info "  Azure Subscription: $AZURE_SUBSCRIPTION"
    if [[ -n "$GITHUB_ENVIRONMENT" ]]; then
        log_info "  GitHub Environment: $GITHUB_ENVIRONMENT"
    fi
    echo ""
    
    # Get tenant ID early for later use
    local tenant_id
    tenant_id=$(get_tenant_id)
    
    # Create app registration
    local app_id
    app_id=$(create_app_registration)
    
    # Wait a moment for app to be fully created
    sleep 2
    
    # Get object ID for the app
    local object_id
    object_id=$(az ad app show --id "$app_id" --query id -o tsv)
    
    # Create service principal
    create_service_principal "$app_id"
    
    # Configure federated credentials for OIDC
    configure_federated_credentials "$app_id" "$object_id"
    
    echo ""
    # Output configuration information
    output_configuration "$app_id" "$tenant_id"
}

# Run main function with all script arguments
main "$@"
