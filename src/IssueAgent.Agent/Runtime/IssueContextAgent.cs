using System;
using System.Threading;
using System.Threading.Tasks;
using IssueAgent.Agent.Conversation;
using IssueAgent.Agent.GitHub;
using IssueAgent.Agent.GraphQL;
using IssueAgent.Agent.Instrumentation;
using IssueAgent.Agent.Security;
using IssueAgent.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IssueAgent.Agent.Runtime;

public class IssueContextAgent
{
    private readonly GitHubTokenGuard _tokenGuard;
    private readonly IssueContextQueryExecutor _queryExecutor;
    private readonly StartupMetricsRecorder _metricsRecorder;
    private readonly ConversationHistoryBuilder _historyBuilder;
    private readonly ResponseDecisionEngine _decisionEngine;
    private readonly AgentResponseGenerator _responseGenerator;
    private readonly GitHubCommentPoster? _commentPoster;
    private readonly ILogger<IssueContextAgent>? _logger;

    public IssueContextAgent(
        GitHubTokenGuard tokenGuard,
        IssueContextQueryExecutor queryExecutor,
        StartupMetricsRecorder metricsRecorder,
        ConversationHistoryBuilder historyBuilder,
        ResponseDecisionEngine decisionEngine,
        AgentResponseGenerator responseGenerator,
        GitHubCommentPoster? commentPoster = null,
        ILogger<IssueContextAgent>? logger = null)
    {
        _tokenGuard = tokenGuard ?? throw new ArgumentNullException(nameof(tokenGuard));
        _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
        _historyBuilder = historyBuilder ?? throw new ArgumentNullException(nameof(historyBuilder));
        _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
        _responseGenerator = responseGenerator ?? throw new ArgumentNullException(nameof(responseGenerator));
        _commentPoster = commentPoster;
        _logger = logger;
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
            var result = await _queryExecutor.FetchIssueContextAsync(request, cancellationToken).ConfigureAwait(false);

            if (result.Status != IssueContextStatus.Success || result.Issue == null)
            {
                return result;
            }

            // Build conversation history
            var history = _historyBuilder.BuildHistory(result.Issue);
            _logger?.LogInformation("Built conversation history with {MessageCount} messages", history.Count);

            // Decide if we should respond based on @mentions
            var decision = _decisionEngine.ShouldRespond(history);
            _logger?.LogInformation("Response decision: {Decision} - {Reason}", decision.Decision, decision.Reason);

            if (decision.Decision == ResponseDecision.Skip)
            {
                _logger?.LogInformation("Skipping response for issue #{IssueNumber}", result.Issue.Number);
                return result;
            }

            // If we should respond, generate response using AI and post comment
            if (_commentPoster != null)
            {
                _logger?.LogInformation("Generating AI response for issue #{IssueNumber}", result.Issue.Number);
                
                var responseBody = await _responseGenerator.GenerateResponseAsync(history, decision, cancellationToken).ConfigureAwait(false);

                var postResult = await _commentPoster.PostCommentAsync(
                    request.Owner,
                    request.Name,
                    request.IssueNumber,
                    responseBody,
                    cancellationToken).ConfigureAwait(false);

                if (postResult.Success)
                {
                    _logger?.LogInformation("Successfully posted comment: {CommentUrl}", postResult.CommentUrl);
                }
                else
                {
                    _logger?.LogError("Failed to post comment: {ErrorMessage}", postResult.ErrorMessage);
                }
            }

            return result;
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
