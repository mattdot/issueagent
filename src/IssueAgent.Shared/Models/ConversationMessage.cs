using System;

namespace IssueAgent.Shared.Models;

public enum MessageRole
{
    User,
    Assistant
}

public record ConversationMessage
{
    public required string MessageId { get; init; }
    public required MessageRole Role { get; init; }
    public required string AuthorName { get; init; }
    public required string Text { get; init; }
    public required DateTime CreatedAtUtc { get; init; }

    public static ConversationMessage Create(string messageId, MessageRole role, string authorName, string text, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message id must be provided.", nameof(messageId));
        }

        if (string.IsNullOrWhiteSpace(authorName))
        {
            throw new ArgumentException("Author name must be provided.", nameof(authorName));
        }

        var normalizedId = messageId.Trim();
        var normalizedAuthor = authorName.Trim();
        var normalizedText = (text ?? string.Empty).Trim();

        var utcTimestamp = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : createdAtUtc.ToUniversalTime();

        if (utcTimestamp > DateTime.UtcNow.AddSeconds(1))
        {
            throw new ArgumentException("Message timestamps cannot be in the future.", nameof(createdAtUtc));
        }

        return new ConversationMessage
        {
            MessageId = normalizedId,
            Role = role,
            AuthorName = normalizedAuthor,
            Text = normalizedText,
            CreatedAtUtc = utcTimestamp
        };
    }
}
