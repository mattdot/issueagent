# Contract Tests — Issue Context Query

These tests will be implemented in `tests/IssueAgent.ContractTests/GraphQL/IssueContextQueryTests.cs`.

## Test: Should_ReturnIssueAndRecentComments
- **Arrange**: Mock GitHub GraphQL endpoint to respond with issue and two comments.
- **Act**: Execute issue-context query with variables derived from sample `issue_comment` event payload.
- **Assert**: Response contains matching `id`, `number`, `title`, comment `id`s, and `totalCount`. Test initially fails because runtime not yet implemented.

## Test: Should_ReturnPermissionDenied
- **Arrange**: Mock endpoint to return GraphQL error `INSUFFICIENT_SCOPES`.
- **Act**: Execute issue-context query using default `GITHUB_TOKEN`.
- **Assert**: Runtime maps error to `IssueContextResult.Status = PermissionDenied` and logs actionable remediation. Test fails until error handling implemented.

## Test: Should_HandleMissingIssue
- **Arrange**: Mock endpoint returns null for `issue`.
- **Act**: Execute issue-context query for an issue number not present.
- **Assert**: Runtime logs `GraphQLFailure` with message referencing missing issue number. Test fails until logic implemented.
