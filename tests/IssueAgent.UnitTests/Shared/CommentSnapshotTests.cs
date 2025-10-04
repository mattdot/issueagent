using System;
using FluentAssertions;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Shared;

public class CommentSnapshotTests
{
    [Fact]
    public void Create_ShouldTrimBodyTo280Characters()
    {
        var longBody = new string('a', 500);

        var snapshot = CommentSnapshot.Create("CID", "octocat", longBody, DateTime.UtcNow);

        snapshot.BodyExcerpt.Should().HaveLength(280);
    }

    [Fact]
    public void Create_ShouldRejectFutureTimestamp()
    {
        var act = () => CommentSnapshot.Create("CID", "octocat", "body", DateTime.UtcNow.AddMinutes(1));

        act.Should().Throw<ArgumentException>();
    }
}
