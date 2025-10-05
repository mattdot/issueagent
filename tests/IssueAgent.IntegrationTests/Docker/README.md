# Docker Integration Tests

These integration tests validate the complete IssueAgent Docker container against real GitHub API endpoints.

## Prerequisites

1. **Docker** must be running and accessible (uses `sudo docker`)
2. **Environment Variables** must be set:
   - `TEST_PAT`: A GitHub Personal Access Token with `repo` scope
   - `TEST_REPO`: A repository in `owner/repo` format that the PAT has access to

## Running the Tests

### In Development Environment

```bash
# Set environment variables
export TEST_PAT="your_github_token_here"
export TEST_REPO="owner/repo"

# Run all tests including Docker integration tests
dotnet test

# Run only Docker integration tests
dotnet test --filter "FullyQualifiedName~Docker"
```

### In CI/CD

The tests automatically skip if `TEST_PAT` or `TEST_REPO` are not set, making them safe to run in any environment.

## What These Tests Validate

### 1. `DockerContainer_ShouldProcessIssueOpenedEvent_WithRealGitHubAPI`
- Builds the Docker image
- Runs the container with a real GitHub token
- Fetches issue context from issue #1 in the test repository
- Verifies successful execution (exit code 0)
- Validates log output contains success indicators
- Checks that startup metrics are reported

### 2. `DockerContainer_ShouldHandleInvalidToken_Gracefully`
- Tests error handling with an invalid GitHub token
- Verifies the agent fails gracefully (non-zero exit code)
- Ensures error messages are reported properly

### 3. `DockerContainer_ShouldValidateStartupPerformance`
- Measures total container execution time
- Validates AOT startup performance (< 5 seconds cold start)
- Ensures total execution completes within 30 seconds (GitHub Actions requirement)

## Test Architecture

These tests use the Docker CLI (`sudo docker`) to:
1. Build the image from the workspace Dockerfile
2. Create and run containers with proper environment variables
3. Mount the GitHub event payload as a volume
4. Capture stdout/stderr output
5. Verify exit codes and log content

## Troubleshooting

**Docker permission denied**:
```bash
sudo service docker start
```

**Tests are skipped**:
- Verify `TEST_PAT` and `TEST_REPO` environment variables are set
- Check that the variables are exported in your current shell session

**Build failures**:
- Ensure you're running from the workspace root
- Verify the Dockerfile exists and is valid
- Check that all project dependencies are restored

## Implementation Notes

### Octokit.GraphQL Bug Fix (October 2025)

During development of these tests, we discovered a critical bug in Octokit.GraphQL 0.4.0-beta: the library was sending raw GraphQL query strings directly in the HTTP request body, but GitHub's GraphQL API expects queries to be wrapped in a JSON object with a `"query"` field.

**Problem**:
```http
POST /graphql
Content-Type: application/json

query IssueContextQuery { ... }
```

**Expected**:
```http
POST /graphql
Content-Type: application/json

{"query": "query IssueContextQuery { ... }"}
```

GitHub's API would return `400 Bad Request` with "Problems parsing JSON" when receiving the malformed request.

**Solution**: We replaced Octokit.GraphQL with a custom HttpClient-based GraphQL client that:
- Properly wraps queries in JSON objects
- Uses manual JSON escaping for AOT compatibility
- Eliminates dependency on a buggy third-party library
- Reduces binary size and improves performance

This fix is implemented in `src/IssueAgent.Agent/GraphQL/GitHubGraphQLClient.cs`.
