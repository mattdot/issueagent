using System;
using System.Linq;
using FluentAssertions;
using IssueAgent.Agent.Conversation;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Conversation;

public class ConversationHistoryBuilderTests
{
    [Fact]
    public void BuildHistory_ShouldIncludeIssueAsFirstUserMessage()
    {
        var builder = new ConversationHistoryBuilder("github-actions[bot]");
        var issue = IssueSnapshot.Create(
            "ISSUE_1",
            42,
            "Test Issue",
            "Issue body content",
            DateTime.UtcNow,
            "testuser",
            null);

        var history = builder.BuildHistory(issue);

        history.Should().HaveCount(1);
        history[0].Role.Should().Be(MessageRole.User);
        history[0].AuthorName.Should().Be("testuser");
        history[0].Text.Should().Contain("Test Issue");
        history[0].Text.Should().Contain("Issue body content");
    }

    [Fact]
    public void BuildHistory_ShouldIdentifyBotCommentsAsAssistant()
    {
        var builder = new ConversationHistoryBuilder("github-actions[bot]");
        var comments = new[]
        {
            CommentSnapshot.Create("C1", "github-actions[bot]", "Bot comment <!-- issueagent-signature -->", DateTime.UtcNow)
        };
        
        var issue = IssueSnapshot.Create(
            "ISSUE_1",
            42,
            "Test",
            "Body",
            DateTime.UtcNow,
            "user1",
            comments);

        var history = builder.BuildHistory(issue);

        history.Should().HaveCount(2);
        history[1].Role.Should().Be(MessageRole.Assistant);
        history[1].AuthorName.Should().Be("issueagent");
    }

    [Fact]
    public void BuildHistory_ShouldNotIdentifyBotCommentWithoutSignatureAsAssistant()
    {
        var builder = new ConversationHistoryBuilder("github-actions[bot]");
        var comments = new[]
        {
            CommentSnapshot.Create("C1", "github-actions[bot]", "Bot comment without signature", DateTime.UtcNow)
        };
        
        var issue = IssueSnapshot.Create(
            "ISSUE_1",
            42,
            "Test",
            "Body",
            DateTime.UtcNow,
            "user1",
            comments);

        var history = builder.BuildHistory(issue);

        history.Should().HaveCount(2);
        history[1].Role.Should().Be(MessageRole.User);
        history[1].AuthorName.Should().Be("github-actions[bot]");
    }

    [Fact]
    public void BuildHistory_ShouldIdentifySignatureCommentsFromOtherBotAsUser()
    {
        var builder = new ConversationHistoryBuilder("github-actions[bot]");
        var comments = new[]
        {
            CommentSnapshot.Create("C1", "some-other-bot", "Comment with <!-- issueagent-signature --> marker", DateTime.UtcNow)
        };
        
        var issue = IssueSnapshot.Create(
            "ISSUE_1",
            42,
            "Test",
            "Body",
            DateTime.UtcNow,
            "user1",
            comments);

        var history = builder.BuildHistory(issue);

        history.Should().HaveCount(2);
        history[1].Role.Should().Be(MessageRole.User);
        history[1].AuthorName.Should().Be("some-other-bot");
    }

    [Fact]
    public void BuildHistory_ShouldPreserveCommentOrder()
    {
        var builder = new ConversationHistoryBuilder("github-actions[bot]");
        var now = DateTime.UtcNow;
        var comments = new[]
        {
            CommentSnapshot.Create("C1", "user1", "First", now.AddMinutes(-10)),
            CommentSnapshot.Create("C2", "user2", "Second", now.AddMinutes(-5)),
            CommentSnapshot.Create("C3", "user3", "Third", now)
        };
        
        var issue = IssueSnapshot.Create(
            "ISSUE_1",
            42,
            "Test",
            "Body",
            now.AddMinutes(-20),
            "author",
            comments);

        var history = builder.BuildHistory(issue);

        history.Should().HaveCount(4);
        history[0].Text.Should().Contain("Test");
        history[1].Text.Should().Be("First");
        history[2].Text.Should().Be("Second");
        history[3].Text.Should().Be("Third");
    }
}
