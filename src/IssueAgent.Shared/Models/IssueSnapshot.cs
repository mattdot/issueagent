using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace IssueAgent.Shared.Models;

public record IssueSnapshot
{
    public required string Id { get; init; }

    public required int Number { get; init; }

    public required string Title { get; init; }

    public required string Body { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required string AuthorLogin { get; init; }

    public IReadOnlyList<CommentSnapshot>? LatestComments { get; init; }

    private const int MaxTitleLength = 256;
    private const int MaxCommentCount = 5;

    public static IssueSnapshot Create(string id, int number, string title, string body, DateTime createdAtUtc, string authorLogin, IReadOnlyList<CommentSnapshot>? latestComments)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Issue id must be provided.", nameof(id));
        }

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Issue number must be positive.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Issue title must be provided.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(authorLogin))
        {
            throw new ArgumentException("Issue author login must be provided.", nameof(authorLogin));
        }

        var normalizedId = id.Trim();
        var normalizedTitle = title.Trim();
        if (normalizedTitle.Length > MaxTitleLength)
        {
            normalizedTitle = normalizedTitle[..MaxTitleLength];
        }

        var normalizedBody = (body ?? string.Empty).Trim();
        var normalizedAuthor = authorLogin.Trim();

        var utcTimestamp = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : createdAtUtc.ToUniversalTime();

        if (utcTimestamp > DateTime.UtcNow.AddSeconds(1))
        {
            throw new ArgumentException("Issue timestamps cannot be in the future.", nameof(createdAtUtc));
        }

        ReadOnlyCollection<CommentSnapshot>? limitedComments = null;
        if (latestComments is not null)
        {
            var filtered = latestComments
                .Where(c => c is not null)
                .Take(MaxCommentCount)
                .ToList();

            limitedComments = filtered.AsReadOnly();
        }

        return new IssueSnapshot
        {
            Id = normalizedId,
            Number = number,
            Title = normalizedTitle,
            Body = normalizedBody,
            CreatedAtUtc = utcTimestamp,
            AuthorLogin = normalizedAuthor,
            LatestComments = limitedComments
        };
    }
}
