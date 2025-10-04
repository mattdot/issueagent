using System;
using System.Threading;
using System.Threading.Tasks;
using IssueAgent.Agent.GraphQL;
using IssueAgent.Agent.Instrumentation;
using IssueAgent.Agent.Security;
using IssueAgent.Shared.Models;

namespace IssueAgent.Agent.Runtime;

public class IssueContextAgent
{
    private readonly GitHubTokenGuard _tokenGuard;
    private readonly IssueContextQueryExecutor _queryExecutor;
    private readonly StartupMetricsRecorder _metricsRecorder;

    public IssueContextAgent(
        GitHubTokenGuard tokenGuard,
        IssueContextQueryExecutor queryExecutor,
        StartupMetricsRecorder metricsRecorder)
    {
        _tokenGuard = tokenGuard ?? throw new ArgumentNullException(nameof(tokenGuard));
        _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
    }

    public Task<IssueContextResult> ExecuteAsync(IssueContextRequest request, string? token, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ExecuteInternalAsync(request, token, cancellationToken);
    }

    private async Task<IssueContextResult> ExecuteInternalAsync(IssueContextRequest request, string? token, CancellationToken cancellationToken)
    {
        await _tokenGuard.EnsureTokenAsync(token, cancellationToken).ConfigureAwait(false);

        using var measurement = _metricsRecorder.BeginMeasurement();

        try
        {
            return await _queryExecutor.FetchIssueContextAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return IssueContextResult.UnexpectedError(request.RunId, request.EventType, ex.Message);
        }
    }
}
