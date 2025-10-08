# IssueAgent Development Guidelines

An AI-powered GitHub Action that analyzes, evaluates, and refactors GitHub issues into high-quality user stories using Microsoft Agent Framework and Azure AI Foundry.

Auto-generated from all feature plans. Last updated: 2025-10-04

## Project Overview

IssueAgent is a GitHub Action that automatically retrieves issue context, analyzes content using AI, and enhances issues into well-structured user stories. The action is built with:
- **Performance**: < 1 second cold start using .NET 9.0 AOT compilation
- **Security**: Minimal permissions (issues:read), redacted credential logging
- **Enterprise Ready**: Supports GitHub Enterprise Server and Azure AI Foundry
- **Observable**: Structured logging with performance metrics

## Active Technologies

### Core Stack
- **.NET 9.0 LTS** with AOT (Ahead-of-Time) compilation for minimal startup time
- **Microsoft Agent Framework SDK** (Microsoft.Agents.AI, Microsoft.Agents.AI.AzureAI)
- **Azure.AI.Projects** and **Azure.Identity** for AI Foundry connectivity
- **Octokit.GraphQL** (AOT-compatible) - Note: Using custom HttpClient-based GraphQL client due to Octokit.GraphQL 0.4.0-beta bug
- **GitHub Actions** metadata schema

### Testing
- **xUnit** for unit and integration tests
- **Docker-based integration tests** for end-to-end validation
- **Contract tests** for GraphQL queries

### Build & Runtime
- **Docker** (Alpine Linux runtime) for containerized execution
- **Multi-stage Dockerfile** for optimized build and minimal runtime image (~15MB)

## Project Structure

```
.
├── src/
│   ├── IssueAgent.Agent/          # Core agent logic and GraphQL client
│   ├── IssueAgent.Action/         # Action host (entrypoint)
│   └── IssueAgent.Shared/         # Shared models and contracts
├── tests/
│   ├── IssueAgent.UnitTests/      # Unit tests
│   ├── IssueAgent.ContractTests/  # GraphQL contract tests
│   └── IssueAgent.IntegrationTests/
│       └── Docker/                # End-to-end Docker integration tests
├── docs/
│   ├── operations/                # Operations runbook
│   └── releases/                  # Release checklist
├── specs/                         # Feature specifications
├── .devcontainer/                 # Dev Container configuration
├── .github/
│   ├── workflows/                 # CI/CD workflows
│   └── copilot-instructions.md    # This file
├── Dockerfile                     # Multi-stage Docker build
└── action.yml                     # GitHub Action metadata
```

## Commands

### Build and Test
```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test categories
dotnet test --filter "FullyQualifiedName~UnitTests"
dotnet test --filter "FullyQualifiedName~ContractTests"
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Run Docker integration tests (requires TEST_PAT and TEST_REPO env vars)
export TEST_PAT="your_github_token"
export TEST_REPO="owner/repo"
dotnet test --filter "FullyQualifiedName~Docker"
```

### Docker
```bash
# Build Docker image locally
docker build -f Dockerfile -t issueagent:local .

# Run action locally
docker run --rm \
  -e GITHUB_TOKEN="your_token" \
  -e GITHUB_REPOSITORY="owner/repo" \
  -e GITHUB_EVENT_NAME="issues" \
  -e GITHUB_EVENT_PATH="/app/event.json" \
  -v $(pwd)/event.json:/app/event.json \
  issueagent:local
```

## Code Style and Conventions

### .NET 9.0 AOT-Specific Requirements
- **Nullable Reference Types**: Always enabled (`Nullable=enable`)
- **Warnings as Errors**: All warnings treated as compilation errors (`TreatWarningsAsErrors=true`)
- **AOT Compatibility**: No reflection or dynamic code generation - use source generators where needed
- **Trimming**: `PublishTrimmed=true` and `InvariantGlobalization=true` for size optimization
- **Analysis Level**: Latest (`AnalysisLevel=latest`)

### GraphQL Client
⚠️ **IMPORTANT**: Use custom HttpClient-based GraphQL client (`src/IssueAgent.Agent/GraphQL/GitHubGraphQLClient.cs`) instead of Octokit.GraphQL due to bug in version 0.4.0-beta that sends malformed requests.

Our implementation:
- Properly wraps queries in JSON: `{"query": "..."}`
- Uses manual JSON escaping for AOT compatibility
- Minimizes dependencies and binary size

### Coding Standards
- Follow standard C# conventions (PascalCase for types/methods, camelCase for parameters/locals)
- Use `async`/`await` for all I/O operations
- Prefer readonly fields and properties
- Use dependency injection for all services
- Log all errors with structured logging (avoid PII in logs)

### Performance Requirements
- **Cold Start**: < 1 second (AOT compilation ensures this)
- **Total Execution**: < 30 seconds (GitHub Actions requirement)
- **Binary Size**: ~15MB (AOT-compiled, trimmed)
- Monitor `StartupDurationMs` metric in logs

## Instructions for GitHub Copilot

When working on this repository:

1. **Always use .NET 9.0** - Do not suggest older .NET versions
2. **AOT compatibility is critical** - Avoid suggesting reflection-based solutions, use source generators instead
3. **Use custom GraphQL client** - Never suggest Octokit.GraphQL for new GraphQL queries, extend `GitHubGraphQLClient.cs` instead
4. **Security first**:
   - Never log credentials or tokens (use RedactionMiddleware)
   - Always validate input parameters
   - Use GitHub Actions secrets for sensitive data
5. **Test coverage required** - All new code must have corresponding unit tests
6. **Docker compatibility** - Ensure all changes work in Alpine Linux containers
7. **Performance aware** - Keep startup time < 1 second, avoid unnecessary dependencies
8. **Follow Microsoft Agent Framework patterns** - Use official SDK patterns for AI interactions

### Dependency Management
- Managed centrally via `Directory.Packages.props`
- All package versions use `<PackageVersion>` elements
- Use `<PackageReference Include="..." />` without Version attribute in project files
- Only add new dependencies if absolutely necessary for AOT compatibility

### Testing Guidelines
All code changes must:
1. Include unit tests for logic
2. Include integration tests for end-to-end scenarios
3. Pass all existing tests (`dotnet test` must succeed with 100% pass rate)
4. Maintain performance (Docker integration tests validate startup time < 5s)
5. Handle errors gracefully (include negative test cases)

## Development Workflow

1. **Dev Container**: Use VS Code Dev Containers for consistent environment (includes .NET 9.0 SDK, Docker, specify-cli)
2. **MCP Servers**: MS Docs and NuGet MCP servers configured in `.vscode/github-copilot/mcp.json`
3. **Build locally first**: Always run `dotnet build` before committing
4. **Run tests**: Execute `dotnet test` to ensure all tests pass
5. **Docker validation**: Build and test Docker image locally for significant changes

## Important Documentation

- **Contributing Guide**: [CONTRIBUTING.md](../CONTRIBUTING.md) - Comprehensive development setup and guidelines
- **README**: [README.md](../README.md) - Project overview and usage instructions
- **Operations Runbook**: [docs/operations/issue-context-runbook.md](../docs/operations/issue-context-runbook.md)
- **Release Checklist**: [docs/releases/issue-context-checklist.md](../docs/releases/issue-context-checklist.md)
- **Feature Specs**: [specs/](../specs/) - Detailed feature implementation plans

## Recent Changes
- 002-ai-foundry-connectivity: Added Azure AI Foundry connectivity with API key authentication, Microsoft Agent Framework SDK integration
- 001-the-core-action: Initial GitHub Action with AOT .NET agent, custom GraphQL client, minimal container strategy

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
