# Contributing to IssueAgent

Thank you for your interest in contributing to IssueAgent! This document provides guidelines and instructions for developing and testing this GitHub Action.

## Development Environment

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (for building and testing the action image)
- [VS Code](https://code.visualstudio.com/) with Dev Containers extension (recommended)

### Dev Container Setup

This repository includes a VS Code Dev Container that provides a complete development environment with:

- .NET 9.0 SDK
- Docker-in-Docker support
- GitHub spec-kit tooling (`specify-cli`)
- Common VS Code extensions for .NET development
- GitHub Copilot MCP Servers (MS Docs, NuGet)

**To use the Dev Container:**

1. Install the **Dev Containers** extension in VS Code
2. Open the repository and select **Reopen in Container** when prompted (or use Command Palette: `Dev Containers: Reopen in Container`)
3. The container will build and install all dependencies automatically

The devcontainer automatically verifies the installation by running `dotnet --version` and installs the specify-cli tool from the GitHub spec-kit repository.

### GitHub Copilot MCP Servers

This repository is configured to use Model Context Protocol (MCP) servers with GitHub Copilot:

- **MS Docs MCP Server** - Provides access to Microsoft documentation
- **NuGet MCP Server** - Provides access to NuGet package information

The MCP servers are configured in `.vscode/github-copilot/mcp.json` and are automatically available when using GitHub Copilot in this repository.

## Building and Testing

### Running Tests

Run the full test suite:

```bash
dotnet test
```

Run specific test categories:

```bash
# Unit tests only
dotnet test --filter "FullyQualifiedName~UnitTests"

# Contract tests only
dotnet test --filter "FullyQualifiedName~ContractTests"

# Integration tests (including Docker)
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Docker integration tests only
dotnet test --filter "FullyQualifiedName~Docker"
```

### Docker Integration Tests

The Docker integration tests validate the complete end-to-end functionality against the real GitHub API. They require:

**Environment Variables:**
- `TEST_PAT`: A GitHub Personal Access Token with `repo` scope
- `TEST_REPO`: Target repository in format `owner/repo` (e.g., `mattdot/issueagent-test`)

```bash
export TEST_PAT="your_github_token_here"
export TEST_REPO="owner/repo"
dotnet test --filter "FullyQualifiedName~Docker"
```

Tests will skip gracefully if these variables are not set. See `tests/IssueAgent.IntegrationTests/Docker/README.md` for detailed documentation.

### Building the Docker Image

Build the Docker image locally:

```bash
docker build -f Dockerfile -t issueagent:local .
```

The build uses a multi-stage Dockerfile:
- **Build stage**: Compiles with .NET 9 AOT (Ahead-of-Time) compilation
- **Runtime stage**: Minimal Alpine Linux image with the compiled binary

### Running the Action Locally

Test the action locally with Docker:

```bash
# Create a sample event file
cat > event.json << 'EOF'
{
  "action": "opened",
  "issue": {
    "number": 1
  },
  "repository": {
    "name": "test-repo",
    "full_name": "owner/test-repo",
    "owner": {
      "login": "owner"
    }
  }
}
EOF

# Run the container
docker run --rm \
  -e GITHUB_TOKEN="your_token_here" \
  -e GITHUB_REPOSITORY="owner/repo" \
  -e GITHUB_EVENT_NAME="issues" \
  -e GITHUB_EVENT_PATH="/app/event.json" \
  -v $(pwd)/event.json:/app/event.json \
  issueagent:local
```

## Repository Structure

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
├── Dockerfile                     # Multi-stage Docker build
├── action.yml                     # GitHub Action metadata
└── .devcontainer/                 # Dev Container configuration
```

## Performance Expectations

The action is designed for fast startup using .NET 9 AOT compilation:

- **Startup time**: < 1 second (cold start on GitHub-hosted runners)
- **Total execution**: < 30 seconds (GitHub Actions requirement)
- **Binary size**: ~15MB (AOT-compiled, trimmed)

The `StartupDurationMs` metric is logged by the agent. If startup times regress above 1 second, investigate:
- Docker layer caching
- Publish trimming configuration
- Runtime initialization

### AOT Compilation

The project uses Native AOT compilation for maximum performance:

- **Configuration**: `Directory.Build.props` sets `PublishAot=true` for Agent and Action projects
- **Compatibility**: All dependencies must be AOT-compatible (no reflection or dynamic code generation)
- **Size optimization**: Uses `PublishTrimmed=true` and `InvariantGlobalization=true`

## Code Quality

### Compilation Standards

- **Warnings as Errors**: The build treats all warnings as errors (`TreatWarningsAsErrors=true`)
- **Analysis Level**: Latest (`AnalysisLevel=latest`)
- **Nullable Reference Types**: Enabled (`Nullable=enable`)

### Testing Requirements

All code changes must:

1. **Include tests**: Unit tests for logic, integration tests for end-to-end scenarios
2. **Pass all tests**: `dotnet test` must succeed with 100% pass rate
3. **Maintain performance**: Docker integration tests validate startup time < 5s
4. **Handle errors gracefully**: Include negative test cases

### GraphQL Client Implementation

**Important**: We use a custom HttpClient-based GraphQL client instead of Octokit.GraphQL due to a bug in version 0.4.0-beta that sends malformed requests to GitHub's API.

Our implementation:
- Properly wraps queries in JSON: `{"query": "..."}`
- Uses manual JSON escaping for AOT compatibility
- Minimizes dependencies and binary size

See `src/IssueAgent.Agent/GraphQL/GitHubGraphQLClient.cs` for the implementation.

## Submitting Changes

1. **Fork** the repository
2. **Create a branch** for your changes (`git checkout -b feature/my-feature`)
3. **Make your changes** following the coding standards
4. **Run tests**: `dotnet test` (all tests must pass)
5. **Commit** with clear, descriptive messages
6. **Push** to your fork
7. **Open a Pull Request** with a clear description of the changes

## Support and Operations

- **Operations Runbook**: `docs/operations/issue-context-runbook.md`
- **Release Checklist**: `docs/releases/issue-context-checklist.md`

## Questions?

Open an issue or discussion in the repository for questions about contributing.
