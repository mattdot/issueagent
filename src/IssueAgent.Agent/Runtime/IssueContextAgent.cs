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
    private readonly GitHubCommentPoster? _commentPoster;
    private readonly ILogger<IssueContextAgent>? _logger;

    public IssueContextAgent(
        GitHubTokenGuard tokenGuard,
        IssueContextQueryExecutor queryExecutor,
        StartupMetricsRecorder metricsRecorder,
        ConversationHistoryBuilder historyBuilder,
        ResponseDecisionEngine decisionEngine,
        GitHubCommentPoster? commentPoster = null,
        ILogger<IssueContextAgent>? logger = null)
    {
        _tokenGuard = tokenGuard ?? throw new ArgumentNullException(nameof(tokenGuard));
        _queryExecutor = queryExecutor ?? throw new ArgumentNullException(nameof(queryExecutor));
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
        _historyBuilder = historyBuilder ?? throw new ArgumentNullException(nameof(historyBuilder));
        _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
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

            // Decide if we should respond
            var decision = _decisionEngine.ShouldRespond(history);
            _logger?.LogInformation("Response decision: {Decision} - {Reason}", decision.Decision, decision.Reason);

            if (decision.Decision == ResponseDecision.Skip)
            {
                _logger?.LogInformation("Skipping response for issue #{IssueNumber}", result.Issue.Number);
                return result;
            }

            // If we should respond, post a comment
            if (_commentPoster != null && decision.Decision != ResponseDecision.Skip)
            {
                _logger?.LogInformation("Posting response to issue #{IssueNumber}", result.Issue.Number);
                
                // For MVP, we'll generate a simple response
                // In the future, this would call Azure AI Foundry to generate a proper response
                var responseBody = GenerateSimpleResponse(history, decision);

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

    private string GenerateSimpleResponse(System.Collections.Generic.IReadOnlyList<ConversationMessage> history, ResponseDecisionResult decision)
    {
        // MVP: Generate a simple acknowledgment response
        // In the future, this would use Azure AI Foundry with the system prompt
        var latestMessage = history[^1];
        
        if (decision.Decision == ResponseDecision.MustRespond)
        {
            return $"Thanks for mentioning me! I'm here to help improve this issue.\n\n" +
                   $"I can see you're working on: {latestMessage.Text.Substring(0, Math.Min(100, latestMessage.Text.Length))}...\n\n" +
                   $"To provide better assistance, I'll need to understand:\n" +
                   $"- What is the user story or goal?\n" +
                   $"- What are the acceptance criteria?\n" +
                   $"- Are there any constraints or dependencies?";
        }
        else
        {
            return $"Thanks for the update! I've noted your response.\n\n" +
                   $"Let me know if you need help refining the requirements or acceptance criteria.";
        }
    }
}
