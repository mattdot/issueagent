namespace IssueAgent.Shared.Models;

public record IssueContextRequest(
    string Owner,
    string Name,
    int IssueNumber,
    int CommentsPageSize,
    string RunId,
    IssueEventType EventType);
