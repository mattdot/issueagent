using System;
using FluentAssertions;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Shared;

public class IssueContextResultTests
{
    [Fact]
    public void Success_ShouldPopulateAllFields()
    {
        var issue = IssueSnapshot.Create("ISSUE_ID", 42, "Title", "Body", DateTime.UtcNow, "octocat", Array.Empty<CommentSnapshot>());

        var result = IssueContextResult.Success("run-1", IssueEventType.IssueOpened, issue, DateTime.UtcNow);

        result.Status.Should().Be(IssueContextStatus.Success);
        result.Issue.Should().Be(issue);
        result.Message.Should().Contain("Success");
    }

    [Fact]
    public void PermissionDenied_ShouldIncludeGuidance()
    {
        var result = IssueContextResult.PermissionDenied("run-1", IssueEventType.IssueCommentCreated, "Missing scope");

        result.Status.Should().Be(IssueContextStatus.PermissionDenied);
        result.Message.Should().Contain("Missing scope");
    }

    [Fact]
    public void Success_ShouldThrowWhenIssueMissing()
    {
        Action act = () => IssueContextResult.Success("run-1", IssueEventType.IssueOpened, issue: null!, DateTime.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GraphQlFailure_ShouldCaptureMessage()
    {
        var result = IssueContextResult.GraphQlFailure("run-1", IssueEventType.IssueReopened, "Not found");

        result.Status.Should().Be(IssueContextStatus.GraphQLFailure);
        result.Message.Should().Contain("Not found");
    }

    [Fact]
    public void Skipped_ShouldCaptureReason()
    {
        var result = IssueContextResult.Skipped("run-1", IssueEventType.IssueCommentCreated, "Bot comment");

        result.Status.Should().Be(IssueContextStatus.Skipped);
        result.Message.Should().Contain("Bot comment");
        result.Issue.Should().BeNull();
    }
}
