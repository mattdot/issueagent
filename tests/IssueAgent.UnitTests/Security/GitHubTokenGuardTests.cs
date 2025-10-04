using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IssueAgent.Agent.Security;
using Xunit;

namespace IssueAgent.UnitTests.Security;

public class GitHubTokenGuardTests
{
    [Fact]
    public async Task EnsureTokenAsync_ShouldThrowWhenTokenMissing()
    {
        var guard = new GitHubTokenGuard();

        var act = async () => await guard.EnsureTokenAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*github-token*");
    }

    [Fact]
    public async Task EnsureTokenAsync_ShouldAllowNonEmptyToken()
    {
        var guard = new GitHubTokenGuard();

        var act = async () => await guard.EnsureTokenAsync("token", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
