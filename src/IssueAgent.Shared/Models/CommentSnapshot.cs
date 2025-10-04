using System;

namespace IssueAgent.Shared.Models;

public record CommentSnapshot
{
    public required string Id { get; init; }

    public required string AuthorLogin { get; init; }

    public required string BodyExcerpt { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    private const int MaxExcerptLength = 280;

    public static CommentSnapshot Create(string id, string authorLogin, string bodyExcerpt, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Comment id must be provided.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(authorLogin))
        {
            throw new ArgumentException("Author login must be provided.", nameof(authorLogin));
        }

        var normalizedId = id.Trim();
        var normalizedAuthor = authorLogin.Trim();
        var normalizedExcerpt = (bodyExcerpt ?? string.Empty).Trim();

        if (normalizedExcerpt.Length > MaxExcerptLength)
        {
            normalizedExcerpt = normalizedExcerpt[..MaxExcerptLength];
        }

        var utcTimestamp = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : createdAtUtc.ToUniversalTime();

        if (utcTimestamp > DateTime.UtcNow.AddSeconds(1))
        {
            throw new ArgumentException("Comment timestamps cannot be in the future.", nameof(createdAtUtc));
        }

        return new CommentSnapshot
        {
            Id = normalizedId,
            AuthorLogin = normalizedAuthor,
            BodyExcerpt = normalizedExcerpt,
            CreatedAtUtc = utcTimestamp
        };
    }
}
