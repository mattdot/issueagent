using System;

namespace IssueAgent.Shared.Models;

public enum IssueContextStatus
{
    Success,
    GraphQLFailure,
    PermissionDenied,
    UnexpectedError,
    Skipped
}

public enum IssueEventType
{
    IssueOpened,
    IssueReopened,
    IssueCommentCreated
}

public record IssueContextResult
{
    public required string RunId { get; init; }

    public required IssueEventType EventType { get; init; }

    public IssueSnapshot? Issue { get; init; }

    public required DateTime RetrievedAtUtc { get; init; }

    public required IssueContextStatus Status { get; init; }

    public required string Message { get; init; }

    public static IssueContextResult Success(string runId, IssueEventType eventType, IssueSnapshot issue, DateTime retrievedAtUtc)
    {
        if (issue is null)
        {
            throw new ArgumentNullException(nameof(issue));
        }

        return Create(
            runId,
            eventType,
            issue,
            EnsureUtc(retrievedAtUtc),
            IssueContextStatus.Success,
            $"Success: Issue #{issue.Number} retrieved.");
    }

    public static IssueContextResult GraphQlFailure(string runId, IssueEventType eventType, string message)
        => Create(
            runId,
            eventType,
            issue: null,
            DateTime.UtcNow,
            IssueContextStatus.GraphQLFailure,
            FormatMessage("GraphQL failure", message));

    public static IssueContextResult PermissionDenied(string runId, IssueEventType eventType, string message)
        => Create(
            runId,
            eventType,
            issue: null,
            DateTime.UtcNow,
            IssueContextStatus.PermissionDenied,
            FormatMessage("Permission denied", message) + " Ensure workflow permissions allow issues:read access.");

    public static IssueContextResult UnexpectedError(string runId, IssueEventType eventType, string message)
        => Create(
            runId,
            eventType,
            issue: null,
            DateTime.UtcNow,
            IssueContextStatus.UnexpectedError,
            FormatMessage("Unexpected error", message));

    public static IssueContextResult Skipped(string runId, IssueEventType eventType, string reason)
        => Create(
            runId,
            eventType,
            issue: null,
            DateTime.UtcNow,
            IssueContextStatus.Skipped,
            FormatMessage("Skipped", reason));

    private static IssueContextResult Create(
        string runId,
        IssueEventType eventType,
        IssueSnapshot? issue,
        DateTime retrievedAtUtc,
        IssueContextStatus status,
        string message)
    {
        var normalizedRunId = NormalizeNonEmpty(runId, nameof(runId));
        var normalizedMessage = NormalizeNonEmpty(message, nameof(message));

        return new IssueContextResult
        {
            RunId = normalizedRunId,
            EventType = eventType,
            Issue = issue,
            RetrievedAtUtc = EnsureUtc(retrievedAtUtc),
            Status = status,
            Message = normalizedMessage
        };
    }

    private static string NormalizeNonEmpty(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} must be provided.", parameterName);
        }

        return value.Trim();
    }

    private static string FormatMessage(string prefix, string detail)
    {
        var detailText = string.IsNullOrWhiteSpace(detail) ? "No additional details." : detail.Trim();
        return $"{prefix}: {detailText}";
    }

    private static DateTime EnsureUtc(DateTime timestamp)
    {
        var utc = timestamp.Kind == DateTimeKind.Utc ? timestamp : timestamp.ToUniversalTime();
        return utc;
    }
}
