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
    public void ShouldRespond_NoMention_ReturnsSkip()
    {
        var engine = new ResponseDecisionEngine();
        var history = new List<ConversationMessage>
        {
            ConversationMessage.Create("M1", MessageRole.User, "user1", "Just a comment", DateTime.UtcNow)
        };

        var decision = engine.ShouldRespond(history);

        decision.Decision.Should().Be(ResponseDecision.Skip);
        decision.Reason.Should().Contain("No mention");
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
