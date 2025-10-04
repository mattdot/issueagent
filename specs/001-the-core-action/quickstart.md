# Quickstart — Core GitHub Action Issue Context Retrieval

Follow these steps to validate the action from a consumer repository once the Marketplace listing is live.

## 1. Add workflow
```yaml
name: Issue Agent Context
on:
  issues:
    types: [opened, reopened]
  issue_comment:
    types: [created]

jobs:
  issue-context:
    runs-on: ubuntu-latest
    permissions:
      issues: read
    steps:
      - name: Execute issue context agent
        uses: mattdot/issueagent@v1
        with:
          github_token: ${{ github.token }}
```

## 2. Expected behavior
- Workflow downloads the prebuilt Docker image and boots the AOT .NET agent.
- Agent issues a GraphQL query to retrieve the triggering issue and recent comments.
- Logs include an `IssueContextResult` entry with `Status=Success` when the call succeeds.
- On permission or network failures, logs surface `Status=PermissionDenied` or `Status=GraphQLFailure` with remediation steps.

## 3. Validating outputs
- Inspect workflow logs for the serialized `IssueContextResult` JSON payload.
- Confirm `Issue.Number` and `LatestComments` match the triggering issue/comment.
- Verify cold-start timing metrics (`StartupDurationMs`) are reported even though no SLA is enforced yet.

## 4. Troubleshooting
- **Permission errors**: Ensure the workflow includes `permissions: issues: read` or higher.
- **GraphQL errors**: Confirm repository has issues enabled and `GITHUB_TOKEN` has not been restricted by org policy.
- **Slow startup**: Review Docker layer cache usage; rebuild image with trimming enabled if startup exceeds 3 seconds consistently.
