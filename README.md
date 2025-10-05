# Issue Agent — Core Issue Context Retrieval Action

This repository packages a Docker-based GitHub Action that boots a precompiled .NET 8 agent to fetch issue context (issue details plus recent comments) through the GitHub GraphQL API. The action is designed for fast startup, minimal footprint, and clear diagnostics so downstream automation can make informed decisions.

## Quickstart

Add the workflow below to a repository that wants to consume the action once it is published to the Marketplace:

```yaml
name: Issue Context
on:
	issues:
		types: [opened, reopened]
	issue_comment:
		types: [created]

	  issues:
	    types: [opened, reopened]
	  issue_comment:
	    types: [created]
			issues: read
		steps:
	  issue-context:
	    runs-on: ubuntu-latest
	    permissions:
	      issues: read
	    steps:
	      - name: Retrieve issue context
	        uses: mattdot/issueagent@v1
	        with:
			  github_token: ${{ github.token }}
			  comments_page_size: 5
The action only needs the workflow-provided `GITHUB_TOKEN` with `issues: read` scope. If organizational policy restricts default permissions, grant the read scope explicitly in the workflow (as shown above).

### Inputs

| Name | Required | Default | Description |
| ---- | -------- | ------- | ----------- |
| `github_token` | No | `${{ github.token }}` | Token used to authenticate GraphQL calls. Must have `issues:read`. |
| `comments_page_size` | No | `5` | Number of most recent issue comments to include (1–20). |

### Environment Variables

The action respects standard GitHub environment variables for API endpoints, making it compatible with GitHub Enterprise Server:

| Variable | Description | Default |
| -------- | ----------- | ------- |
| `GITHUB_GRAPHQL_URL` | Custom GraphQL API endpoint | `https://api.github.com/graphql` |
| `GITHUB_API_URL` | Custom REST API base URL (GraphQL derived by appending `/graphql`) | `https://api.github.com` |

**Example for GitHub Enterprise Server:**
```yaml
env:
  GITHUB_GRAPHQL_URL: https://github.company.com/api/graphql
```

### Outputs and Logs

The action does not emit formal outputs, but it writes a structured `IssueContextResult` to the workflow logs, including:

- `Status` (`Success`, `GraphQLFailure`, `PermissionDenied`, `UnexpectedError`)
- `Message` with remediation guidance on failure
- `Issue` snapshot (ID, number, title, author login)
- Recent comment excerpts within the requested page size
- `StartupDurationMs` metric for cold-start insight

Monitor the logs for `Issue context status:` entries to integrate with downstream automation.

## Local Development

### Prerequisites

- .NET 8.0 SDK
- Docker (for building/testing the action image)

### Dev Container

This repository includes a VS Code Dev Container that provisions:

- .NET 8.0 SDK
- GitHub spec-kit tooling (`specify-cli`)
- Common VS Code extensions for .NET development

To use it:

1. Install the **Dev Containers** extension in VS Code.
2. Open the repository and pick **Reopen in Container** when prompted (or from the Command Palette).
3. The container build installs dependencies and verifies toolchain availability.

### Building & Testing

```bash
dotnet test
	The devcontainer automatically verifies the installation by running `dotnet --version` and `uv --version`, and installs the specify-cli tool from the GitHub spec-kit repository and the NuGet MCP Server tool after creation.

```

To build the Docker image locally:

```bash
docker build -f Dockerfile -t issueagent:local .
```

To run the published AOT binary manually (requires environment variables similar to GitHub Actions):

```bash
docker run --rm \
	-e GITHUB_TOKEN=<token> \
	-e GITHUB_REPOSITORY=<owner/repo> \
	-e GITHUB_EVENT_NAME=<event> \
	-e GITHUB_EVENT_PATH=/app/event.json \
	-v $(pwd)/samples/event.json:/app/event.json \
	issueagent:local
```

## Cold Start Expectations

The action publishes as a .NET 8 AOT single-file binary running on Alpine. Typical startup is under a few hundred milliseconds on GitHub-hosted runners. The `StartupDurationMs` metric logged by the agent should remain below ~1s; investigate docker layer caching or publish trimming if times regress.

## Repository Layout

- `src/IssueAgent.Agent` — Core agent logic and GraphQL client
- `src/IssueAgent.Action` — Action host that wires the agent into the Docker entrypoint
- `Dockerfile` — Multi-stage build producing the runtime image
- `tests/` — Unit, contract, and integration test suites
- `docs/operations` — Operations runbook for on-call responders
- `docs/releases` — Release checklist to publish new versions

## Support & Operations

See `docs/operations/issue-context-runbook.md` for guidance on investigating failures, reviewing telemetry, and escalating issues. Release engineering steps are documented in `docs/releases/issue-context-checklist.md`.
=======
The devcontainer automatically verifies the installation by running `dotnet --version` and `uv --version`, and installs the specify-cli tool from the GitHub spec-kit repository and the NuGet MCP Server tool after creation.

## GitHub Copilot MCP Servers

This repository is configured to use Model Context Protocol (MCP) servers with GitHub Copilot, providing enhanced capabilities:

- **MS Docs MCP Server** - Provides access to Microsoft documentation
- **NuGet MCP Server** - Provides access to NuGet package information

The MCP servers are configured in `.vscode/github-copilot/mcp.json` and are automatically available when using GitHub Copilot in this repository.
>>>>>>> origin/main
