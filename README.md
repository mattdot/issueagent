# Issue Agent

An AI-powered GitHub Action that analyzes, evaluates, and refactors GitHub issues into high-quality user stories. Transforms vague bug reports and feature requests into actionable, well-structured issues with consistent formatting, clear acceptance criteria, and proper context.

## Features

- 🤖 **AI-Powered Analysis**: Leverages Microsoft Agent Framework to understand issue intent and context
- ✨ **Automatic Enhancement**: Converts rough ideas into structured user stories with acceptance criteria
- 🏢 **Enterprise Ready**: Supports GitHub Enterprise Server via custom API endpoints
- ⚡ **Fast Startup**: < 1 second cold start with .NET 8 AOT compilation
- 🔒 **Secure**: Minimal permissions required (`issues: write`)
- 📊 **Observable**: Structured logging with performance metrics and AI reasoning traces

## What It Does

Issue Agent automatically:

1. **Retrieves** full issue context (description, comments, labels, references)
2. **Analyzes** the content using AI to understand intent and requirements
3. **Evaluates** clarity, completeness, and actionability
4. **Refactors** (optional) into well-structured user stories with:
   - Clear problem statement
   - Acceptance criteria
   - Technical context
   - Related issues and dependencies
   - Suggested labels and milestones

## Quickstart

Add this workflow to your repository:

```yaml
name: Issue Context
on:
  issues:
    types: [opened, reopened]
  issue_comment:
    types: [created]

jobs:
  analyze-issue:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - name: Retrieve Issue Context
        uses: mattdot/issueagent@v1
        with:
          github_token: ${{ github.token }}
```

## Configuration

### Inputs

| Name | Required | Default | Description |
| ---- | -------- | ------- | ----------- |
| `github_token` | No | `${{ github.token }}` | Token for GitHub API authentication with `issues:read` permission. |
| `azure_ai_foundry_endpoint` | No | - | Azure AI Foundry project endpoint URL (format: `https://<resource>.services.ai.azure.com/api/projects/<project>`). Falls back to `AZURE_AI_FOUNDRY_ENDPOINT` environment variable. |
| `azure_ai_foundry_api_key` | No | - | Azure AI Foundry API key for authentication. Falls back to `AZURE_AI_FOUNDRY_API_KEY` environment variable. Store in GitHub Secrets. |
| `azure_ai_foundry_model_deployment` | No | `gpt-5-mini` | Model deployment name in Azure AI Foundry project. Falls back to `AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT` environment variable. |
| `azure_ai_foundry_api_version` | No | `2025-04-01-preview` | Azure AI Foundry API version. Falls back to `AZURE_AI_FOUNDRY_API_VERSION` environment variable. |
| `enable_verbose_logging` | No | `false` | Enable verbose logging for troubleshooting. When enabled, logs detailed information about configuration, connections, and authentication at the Debug level. |

### Azure AI Foundry Configuration

Issue Agent uses Azure AI Foundry for AI-powered issue analysis. To enable AI features:

#### 1. Create Azure AI Foundry Project

1. Go to [Azure AI Foundry portal](https://ai.azure.com)
2. Create a new project or use an existing one
3. Note the project endpoint URL (Settings → Overview)
4. Generate an API key (Settings → Keys and Endpoints)

#### 2. Add Secrets to GitHub

Add these secrets to your repository (Settings → Secrets and variables → Actions):

- `AZURE_AI_FOUNDRY_ENDPOINT`: Your project endpoint URL
- `AZURE_AI_FOUNDRY_API_KEY`: Your API key

#### 3. Update Workflow

```yaml
name: Issue Context
on:
  issues:
    types: [opened, reopened]

jobs:
  analyze-issue:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - name: Analyze Issue
        uses: mattdot/issueagent@v1
        with:
          github_token: ${{ github.token }}
          azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
          azure_ai_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
          azure_ai_foundry_model_deployment: gpt-4o-mini  # Optional: specify your model
```

#### How Environment Variables Work

The action passes inputs to the Docker container as environment variables. You have two options:

**Option 1: Use inputs (recommended)**
```yaml
steps:
  - name: Analyze Issue
    uses: mattdot/issueagent@v1
    with:
      github_token: ${{ github.token }}
      azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
      azure_ai_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
```

**Option 2: Mix inputs and environment variables**
```yaml
steps:
  - name: Analyze Issue
    uses: mattdot/issueagent@v1
    with:
      github_token: ${{ github.token }}
      # Inputs take precedence; if not provided, falls back to env vars
      azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
```

The action internally checks inputs first, then falls back to `AZURE_AI_FOUNDRY_*` environment variables if inputs are not provided.

#### Connection Validation

The action validates the Azure AI Foundry connection during startup:
- Validates endpoint URL format
- Checks API key length (minimum 32 characters)
- Establishes connection within 30 seconds
- Logs connection success with duration metrics

Failed connections will cause the action to fail with a descriptive error message.

## Troubleshooting

### Enable Verbose Logging

If you're experiencing issues with the action (such as authentication failures or connection problems), enable verbose logging to get detailed diagnostic information:

```yaml
steps:
  - name: Analyze Issue
    uses: mattdot/issueagent@v1
    with:
      github_token: ${{ github.token }}
      azure_ai_foundry_endpoint: ${{ secrets.AZURE_AI_FOUNDRY_ENDPOINT }}
      azure_ai_foundry_api_key: ${{ secrets.AZURE_AI_FOUNDRY_API_KEY }}
      enable_verbose_logging: true
```

When verbose logging is enabled, the action will output:
- Configuration loading details (which environment variables are set)
- Azure AI Foundry connection attempts and responses
- Authentication provider initialization
- API request/response status codes
- Detailed error messages for troubleshooting

**Security Note**: Verbose logs do NOT contain API keys or tokens - these are always redacted for security.

### Common Issues

**Authentication Error (401)**:
- Verify your API key is correct and has not expired
- Check that the API key has access to the specified endpoint
- Ensure the endpoint URL is correct (format: `https://<resource>.services.ai.azure.com/api/projects/<project>`)

**Connection Timeout**:
- Check network connectivity to Azure AI Foundry
- Verify the endpoint URL is accessible from GitHub Actions runners
- Consider firewall or proxy settings in your organization

**Model Not Found (404)**:
- Verify the model deployment name matches your Azure AI Foundry configuration
- Check that the model is deployed and available in your project

## Current Status & Roadmap

**✅ Currently Implemented:**
- Core issue context retrieval via GitHub GraphQL API
- Fast, AOT-compiled Docker action
- GitHub Enterprise Server support
- Comprehensive test suite (24 tests including Docker integration)

**🚧 In Development:**
- Microsoft Agent Framework integration
- AI-powered analysis and evaluation
- Issue enhancement and refactoring
- Quality scoring system

**📋 Planned Features:**
- Support for custom issue templates
- Multi-language issue analysis
- Configurable AI prompts and templates
- Epic and milestone suggestions
- Code context integration (analyze referenced files)
- Team-specific conventions and formatting

## Performance

Optimized for GitHub Actions environments:

- **Cold Start**: < 1 second (AOT compilation)
- **Issue Retrieval**: < 2 seconds
- **AI Analysis**: 2-5 seconds (when implemented)
- **Total Execution**: < 10 seconds (typical)
- **Binary Size**: ~15MB (AOT-compiled, trimmed)

Performance metrics are logged including startup time and (future) AI reasoning time.

## Privacy & Security

- **Data Handling**: Issue content is sent to configured AI endpoint (Azure OpenAI or OpenAI)
- **Token Security**: GitHub tokens are redacted from all logs
- **Minimal Permissions**: Only requires `issues:read` for evaluation, `issues:write` for updates
- **Enterprise Deployment**: Can be fully air-gapped with Azure OpenAI in your tenant

## Support

- 📖 **Documentation**: See [docs/](docs/) for detailed guides
- 🐛 **Bug Reports**: [Open an issue](https://github.com/mattdot/issueagent/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/mattdot/issueagent/discussions)
- 🔧 **Troubleshooting**: Check workflow logs for detailed error messages

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development environment setup
- Testing guidelines (including Docker integration tests)
- Code standards and architecture
- Submission process

## License

[MIT License](LICENSE) - See LICENSE file for details.

---

**Note**: This action is currently under active development. The foundation (fast issue context retrieval with GitHub Enterprise support) is complete and tested. AI analysis and enhancement features are being added in upcoming releases. Watch this repository for updates!
