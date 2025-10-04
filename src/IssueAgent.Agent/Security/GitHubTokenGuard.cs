using System;
using System.Threading;
using System.Threading.Tasks;

namespace IssueAgent.Agent.Security;

public class GitHubTokenGuard
{
    private const string Guidance = "Workflow must provide github-token input (uses: github.token).";

    public virtual Task EnsureTokenAsync(string? token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(Guidance);
        }

        return Task.CompletedTask;
    }
}
