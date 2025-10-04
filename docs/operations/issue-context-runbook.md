# Issue Context Action Operations Runbook

## Purpose
This runbook describes how to triage and remediate incidents involving the Issue Agent Docker-based GitHub Action. The action retrieves issue metadata and recent comments through the GitHub GraphQL API and logs an `IssueContextResult` payload.

## Key Signals

| Signal | Source | Description |
| --- | --- | --- |
| `IssueContextResult.Status` | Workflow logs | Primary health indicator (`Success`, `GraphQLFailure`, `PermissionDenied`, `UnexpectedError`). |
| `IssueContextResult.Message` | Workflow logs | Human-readable explanation and remediation guidance. |
| `StartupDurationMs` | Workflow logs | Startup latency in milliseconds. Values consistently >1000ms warrant investigation. |
| GitHub Actions job duration | Workflow run | Increased job time hints at container startup or network slowness. |

## First Response Checklist

1. **Identify failing runs**: Open the GitHub Actions run that reported the incident and locate the "Issue Context" job.
2. **Capture context**: Record the workflow run URL, repository, and triggered event type.
3. **Review logs**: Expand the action step and search for `Issue context status:` entries to determine status and message.
4. **Classify error type**:
   - `PermissionDenied`: Token lacks `issues:read` scope.
   - `GraphQLFailure`: GraphQL query returned errors or the issue was missing.
   - `UnexpectedError`: Agent encountered an unhandled exception (review stack trace).
5. **Check recent changes**: Note any recent deployments or configuration changes to the action, repository workflow, or GitHub org policies.

## Remediation Steps

### PermissionDenied
- Confirm the workflow job grants `permissions: issues: read`.
- If the organization enforces restricted tokens, coordinate with admins to allow the scope or supply a PAT through `github-token` input.
- Re-run the workflow once permissions are corrected.

### GraphQLFailure
- Inspect the `Message` for GitHub error details (rate limiting, validation errors, or issue not found).
- If the issue number is missing, verify the triggering event payload includes an issue (e.g., skip non-issue comment events).
- For rate limits, delay retries or adjust workflow concurrency.

### UnexpectedError
- Capture the full log (including stack trace) for engineering analysis.
- Check whether GitHub GraphQL or API outages are ongoing via status.github.com.
- File an engineering ticket with reproduction details if the error persists.

### Slow Startup (>1000ms)
- Review recent image size changes or publish settings.
- Ensure Docker layer caching is available for self-hosted runners; GitHub-hosted runners will download the image each run.
- Validate that the action image was built with trimming and AOT enabled (see Dockerfile).

## Escalation

- **Primary contact**: Issue Agent engineering team (Slack `#issue-agent` / on-call rotation).
- **Secondary contact**: Platform Reliability (Slack `#platform-rel`).
- Escalate if:
  - Impact spans multiple repositories and lasts longer than 30 minutes.
  - Errors persist after workflow configuration fixes.
  - Startup latency remains >2s for three consecutive runs.

## Useful References

- [GitHub Action metadata](../../action.yml)
- [Dockerfile](../../Dockerfile)
- [GraphQL executor implementation](../../src/IssueAgent.Agent/GraphQL/IssueContextQueryExecutor.cs)
- [README](../../README.md) for inputs and usage
