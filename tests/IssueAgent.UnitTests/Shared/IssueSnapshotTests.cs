using System;
using System.Linq;
using FluentAssertions;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Shared;

public class IssueSnapshotTests
{
    [Fact]
    public void Create_ShouldEnforceNonEmptyId()
    {
        var act = () => IssueSnapshot.Create(string.Empty, 1, "Title", "octocat", Array.Empty<CommentSnapshot>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldLimitCommentsToFive()
    {
        var comments = Enumerable.Range(0, 10)
            .Select(i => CommentSnapshot.Create($"C{i}", "octocat", $"Comment {i}", DateTime.UtcNow))
            .ToList();

        var snapshot = IssueSnapshot.Create("ID", 1, "Title", "octocat", comments);

        snapshot.LatestComments.Should().HaveCount(5);
    }
}
