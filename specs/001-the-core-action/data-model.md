# Data Model — Core GitHub Action Issue Context Scope

## IssueContextResult
- **Purpose**: Captures the retrieved issue context that downstream automation will consume.
- **Fields**:
  - `RunId` (string, required): Workflow run identifier used for traceability.
  - `EventType` (enum: `IssueOpened`, `IssueReopened`, `IssueCommentCreated`): Derived from GitHub event payload.
  - `Issue` (`IssueSnapshot`, required): Snapshot of the issue returned by GraphQL.
  - `RetrievedAtUtc` (DateTime, required): Timestamp when the GraphQL response was received.
  - `Status` (enum: `Success`, `GraphQLFailure`, `PermissionDenied`, `UnexpectedError`): Result category surfaced to logs.
  - `Message` (string, required): Human-readable status or remediation guidance.

## IssueSnapshot
- **Purpose**: Minimal representation of the issue and comments fetched from GraphQL.
- **Fields**:
  - `Id` (string, required): Node ID returned by GitHub GraphQL API.
  - `Number` (int, required): Issue number.
  - `Title` (string, required): Issue title (trimmed to 256 characters for logging).
  - `AuthorLogin` (string, required): GitHub login of the issue author.
  - `LatestComments` (collection of `CommentSnapshot`, optional): Recent comments included in query response.

## CommentSnapshot
- **Purpose**: Represents individual comments returned alongside the issue.
- **Fields**:
  - `Id` (string, required): Comment node ID.
  - `AuthorLogin` (string, required): Comment author login.
  - `BodyExcerpt` (string, required): First 280 characters of comment body for diagnostics.
  - `CreatedAtUtc` (DateTime, required): Comment creation timestamp.

## Validation Rules
- All IDs must be non-empty GraphQL node identifiers.
- If `Status` is `Success`, `Issue` and `LatestComments` must contain data from the GraphQL response; otherwise they may be null.
- When `Status` is `PermissionDenied`, include remediation guidance in `Message` directing maintainers to adjust workflow permissions.
- `LatestComments` limited to the 5 most recent entries to avoid excessive logging.
