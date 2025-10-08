using System;
using System.Collections.Generic;
using System.Linq;
using IssueAgent.Shared.Models;

namespace IssueAgent.Agent.Conversation;

public class ConversationHistoryBuilder
{
    private const string AgentName = "issueagent";
    private const string SignatureMarker = "<!-- issueagent-signature -->";

    private readonly string _botLogin;

    public ConversationHistoryBuilder(string botLogin)
    {
        if (string.IsNullOrWhiteSpace(botLogin))
        {
            throw new ArgumentException("Bot login must be provided.", nameof(botLogin));
        }

        _botLogin = botLogin.Trim();
    }

    public IReadOnlyList<ConversationMessage> BuildHistory(IssueSnapshot issue)
    {
        if (issue == null)
        {
            throw new ArgumentNullException(nameof(issue));
        }

        var messages = new List<ConversationMessage>();

        // Add issue as the first user message
        var issueText = $"{issue.Title}\n\n{issue.Body}";
        messages.Add(ConversationMessage.Create(
            issue.Id,
            MessageRole.User,
            issue.AuthorLogin,
            issueText,
            issue.CreatedAtUtc));

        // Add comments
        if (issue.LatestComments != null)
        {
            foreach (var comment in issue.LatestComments)
            {
                var isAgentMessage = IsAgentComment(comment);
                messages.Add(ConversationMessage.Create(
                    comment.Id,
                    isAgentMessage ? MessageRole.Assistant : MessageRole.User,
                    isAgentMessage ? AgentName : comment.AuthorLogin,
                    comment.Body,
                    comment.CreatedAtUtc));
            }
        }

        return messages;
    }

    private bool IsAgentComment(CommentSnapshot comment)
    {
        // Check if authored by the workflow identity (bot)
        if (string.Equals(comment.AuthorLogin, _botLogin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if contains the signature marker
        if (comment.Body.Contains(SignatureMarker, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}
