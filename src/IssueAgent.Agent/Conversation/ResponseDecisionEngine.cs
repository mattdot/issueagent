using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IssueAgent.Shared.Models;

namespace IssueAgent.Agent.Conversation;

public enum ResponseDecision
{
    MustRespond,
    Skip
}

public record ResponseDecisionResult
{
    public required ResponseDecision Decision { get; init; }
    public required string Reason { get; init; }
}

public class ResponseDecisionEngine
{
    private const string AgentName = "issueagent";

    private readonly string[] _mentionHandles;

    public ResponseDecisionEngine(string[]? mentionHandles = null)
    {
        _mentionHandles = mentionHandles ?? new[] { AgentName };
    }

    public ResponseDecisionResult ShouldRespond(IReadOnlyList<ConversationMessage> history)
    {
        if (history == null || history.Count == 0)
        {
            return new ResponseDecisionResult 
            { 
                Decision = ResponseDecision.Skip, 
                Reason = "No conversation history" 
            };
        }

        var latestMessage = history[^1];

        // Don't respond to our own messages
        if (latestMessage.Role == MessageRole.Assistant)
        {
            return new ResponseDecisionResult 
            { 
                Decision = ResponseDecision.Skip, 
                Reason = "Latest message is from the agent" 
            };
        }

        // Check for @mentions (must respond)
        if (ContainsMention(latestMessage.Text))
        {
            return new ResponseDecisionResult 
            { 
                Decision = ResponseDecision.MustRespond, 
                Reason = $"@mention of {AgentName} detected" 
            };
        }

        // Otherwise skip - the AI model will decide if it should respond based on context
        return new ResponseDecisionResult 
        { 
            Decision = ResponseDecision.Skip, 
            Reason = "No mention detected" 
        };
    }

    private bool ContainsMention(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        foreach (var handle in _mentionHandles)
        {
            // Check for @mention pattern (not in code blocks or backticks)
            var pattern = $@"@{Regex.Escape(handle)}(?=\s|$|[^\w])";
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
            {
                // Make sure it's not in a code block
                if (!IsInCodeBlock(text, $"@{handle}"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsInCodeBlock(string text, string searchTerm)
    {
        // Simple check: if there are backticks around the search term
        var index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
        {
            return false;
        }

        var beforeCount = text.Substring(0, index).Count(c => c == '`');
        return beforeCount % 2 == 1; // Odd number of backticks means we're inside a code block
    }
}
