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

    public required string AuthorLogin { get; init; }

    public IReadOnlyList<CommentSnapshot>? LatestComments { get; init; }

    private const int MaxTitleLength = 256;
    private const int MaxCommentCount = 5;

    public static IssueSnapshot Create(string id, int number, string title, string authorLogin, IReadOnlyList<CommentSnapshot>? latestComments)
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

        var normalizedAuthor = authorLogin.Trim();

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
            AuthorLogin = normalizedAuthor,
            LatestComments = limitedComments
        };
    }
}
