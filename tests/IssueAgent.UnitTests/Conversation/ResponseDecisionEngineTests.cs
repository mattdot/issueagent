using System;
using System.Collections.Generic;
using FluentAssertions;
using IssueAgent.Agent.Conversation;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Conversation;

public class ResponseDecisionEngineTests
{
    [Fact]
    public void ShouldRespond_WithMention_ReturnsMustRespond()
    {
        var engine = new ResponseDecisionEngine();
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Hello @issueagent can you help?", DateTime.UtcNow)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.MustRespond);
        decision.Reason.Should().Contain("@mention");
    }

    [Fact]
    public void ShouldRespond_LatestIsAssistant_ReturnsSkip()
    {
        var engine = new ResponseDecisionEngine();
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Hello", DateTime.UtcNow.AddMinutes(-5)),
            ConversationMessage.Create("M2", MessageRole.Assistant, "issueagent", "Hi there!", DateTime.UtcNow)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.Skip);
        decision.Reason.Should().Contain("Latest message is from the agent");
    }

    [Fact]
    public void ShouldRespond_WithSemanticFollowUp_ReturnsShouldRespond()
    {
        var engine = new ResponseDecisionEngine();
        var now = DateTime.UtcNow;
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Need help with issue", now.AddMinutes(-10)),
            ConversationMessage.Create("M2", MessageRole.Assistant, "issueagent", "Can you provide the acceptance criteria?", now.AddMinutes(-5)),
            ConversationMessage.Create("M3", MessageRole.User, "user1", "Yes, here are the acceptance criteria: 1. Must support login 2. Must be secure", now)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.ShouldRespond);
        decision.Reason.Should().Contain("semantic follow-up");
    }

    [Fact]
    public void ShouldRespond_AckOnly_ReturnsSkip()
    {
        var engine = new ResponseDecisionEngine();
        var now = DateTime.UtcNow;
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.Assistant, "issueagent", "What do you think?", now.AddMinutes(-5)),
            ConversationMessage.Create("M2", MessageRole.User, "user1", "thanks", now)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.Skip);
    }

    [Fact]
    public void ShouldRespond_NoMentionNoSemanticFollowUp_ReturnsSkip()
    {
        var engine = new ResponseDecisionEngine();
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Just a comment", DateTime.UtcNow)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.Skip);
        decision.Reason.Should().Contain("No mention or semantic follow-up");
    }

    [Fact]
    public void ShouldRespond_MentionInCodeBlock_ReturnsSkip()
    {
        var engine = new ResponseDecisionEngine();
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Check this: `@issueagent` in code", DateTime.UtcNow)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.Skip);
    }
}
