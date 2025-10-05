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
          comments_page_size: 10  # Capture last 10 comments
```

## Configuration

### Inputs

| Name | Required | Default | Description |
| ---- | -------- | ------- | ----------- |
| `github_token` | No | `${{ github.token }}` | Token for GitHub API authentication with `issues:read` permission. |
| `comments_page_size` | No | `5` | Number of recent comments to capture (1-20). |

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
