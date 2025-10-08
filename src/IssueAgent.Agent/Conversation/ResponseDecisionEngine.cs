using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IssueAgent.Shared.Models;

namespace IssueAgent.Agent.Conversation;

public enum ResponseDecision
{
    MustRespond,
    ShouldRespond,
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
    private static readonly TimeSpan SemanticWindowDefault = TimeSpan.FromHours(48);

    private readonly string[] _mentionHandles;
    private readonly TimeSpan _semanticWindow;

    public ResponseDecisionEngine(string[]? mentionHandles = null, TimeSpan? semanticWindow = null)
    {
        _mentionHandles = mentionHandles ?? new[] { AgentName };
        _semanticWindow = semanticWindow ?? SemanticWindowDefault;
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

        // Check for semantic follow-up (should respond)
        var semanticFollowUp = IsSemanticFollowUp(history);
        if (semanticFollowUp)
        {
            return new ResponseDecisionResult 
            { 
                Decision = ResponseDecision.ShouldRespond, 
                Reason = "Detected semantic follow-up to agent's last question" 
            };
        }

        return new ResponseDecisionResult 
        { 
            Decision = ResponseDecision.Skip, 
            Reason = "No mention or semantic follow-up detected" 
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

    private bool IsSemanticFollowUp(IReadOnlyList<ConversationMessage> history)
    {
        // Find the most recent assistant message
        ConversationMessage? lastAgentMessage = null;
        int lastAgentIndex = -1;

        for (int i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].Role == MessageRole.Assistant)
            {
                lastAgentMessage = history[i];
                lastAgentIndex = i;
                break;
            }
        }

        if (lastAgentMessage == null)
        {
            return false;
        }

        // Check if agent asked a question last
        if (!AgentAskedQuestion(lastAgentMessage.Text))
        {
            return false;
        }

        var latestMessage = history[^1];

        // Check adjacency & recency: latest human comment occurs after agent's last message
        if (lastAgentIndex >= history.Count - 1)
        {
            return false;
        }

        // Check time window
        var timeSinceAgentMessage = latestMessage.CreatedAtUtc - lastAgentMessage.CreatedAtUtc;
        if (timeSinceAgentMessage > _semanticWindow)
        {
            return false;
        }

        // Check if answer-like content
        if (IsAckOnly(latestMessage.Text))
        {
            return false;
        }

        if (IsAnswerLike(latestMessage.Text))
        {
            return true;
        }

        return false;
    }

    private bool AgentAskedQuestion(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Check for question marks
        if (text.Contains('?'))
        {
            return true;
        }

        // Check for request cues
        var requestCues = new[]
        {
            "please provide",
            "can you share",
            "could you add",
            "add",
            "list",
            "fill in",
            "acceptance criteria",
            "actors",
            "constraints",
            "steps",
            "repro",
            "link",
            "PR",
            "commit",
            "screenshot"
        };

        return requestCues.Any(cue => text.Contains(cue, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsAnswerLike(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Check for confirmation + substance patterns
        var confirmationPatterns = new[]
        {
            "yes", "no", "done", "updated", "pushed", "added", "completed"
        };

        bool hasConfirmation = confirmationPatterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

        // Check for links, code blocks, or enumerated lists
        bool hasSubstance = text.Contains("http", StringComparison.OrdinalIgnoreCase)
            || text.Contains("```")
            || text.Contains("1.")
            || text.Contains("- ")
            || text.Contains("* ");

        if (hasConfirmation && hasSubstance)
        {
            return true;
        }

        // Check for follow-through phrases
        var followThroughPhrases = new[]
        {
            "per your request",
            "as you suggested",
            "I added",
            "details below",
            "AC:"
        };

        if (followThroughPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check for enumerated or structured answers
        if (Regex.IsMatch(text, @"^\d+\.", RegexOptions.Multiline))
        {
            return true;
        }

        if (Regex.IsMatch(text, @"^- \[[ x]\]", RegexOptions.Multiline))
        {
            return true;
        }

        return false;
    }

    private bool IsAckOnly(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalizedText = text.Trim().ToLowerInvariant();

        // Pure acknowledgment patterns with no substance
        var ackOnlyPatterns = new[]
        {
            "thanks",
            "thank you",
            "sgtm",
            "lgtm",
            "ok",
            "okay",
            "👍",
            "👌",
            ":+1:",
            ":thumbsup:"
        };

        if (ackOnlyPatterns.Contains(normalizedText))
        {
            return true;
        }

        // If it's very short and contains only ack words
        if (normalizedText.Length < 20)
        {
            return ackOnlyPatterns.Any(p => normalizedText.Contains(p));
        }

        return false;
    }
}
