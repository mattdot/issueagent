using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IssueAgent.Shared.Models;

namespace IssueAgent.Agent.GraphQL;

public class IssueContextQueryExecutor
{
    private readonly IGraphQLClient _client;

    public IssueContextQueryExecutor(IGraphQLClient client)
    {
        _client = client;
    }

    public async Task<IssueContextResult> FetchIssueContextAsync(IssueContextRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IssueContextQueryResponse response;

        try
        {
            var query = BuildQuery(request);
            response = await _client.QueryAsync<IssueContextQueryResponse>(query, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return IssueContextResult.UnexpectedError(request.RunId, request.EventType, ex.Message);
        }

        if (response is null)
        {
            return IssueContextResult.UnexpectedError(request.RunId, request.EventType, "GraphQL returned an empty response.");
        }

        if (response.Errors is { Count: > 0 })
        {
            if (response.Errors.Any(IsInsufficientScopes))
            {
                var message = response.Errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Message))?.Message
                    ?? "GitHub returned insufficient scopes.";
                return IssueContextResult.PermissionDenied(request.RunId, request.EventType, message);
            }

            var detail = string.Join("; ", response.Errors
                .Select(e => e?.Message)
                .Where(m => !string.IsNullOrWhiteSpace(m)));

            var failureMessage = string.IsNullOrWhiteSpace(detail)
                ? "GraphQL query failed without details."
                : detail;

            return IssueContextResult.GraphQlFailure(request.RunId, request.EventType, failureMessage);
        }

        var issue = response.Repository?.Issue;
        if (issue is null)
        {
            return IssueContextResult.GraphQlFailure(request.RunId, request.EventType, $"Issue #{request.IssueNumber} not found.");
        }

        if (string.IsNullOrWhiteSpace(issue.Id) || string.IsNullOrWhiteSpace(issue.Title))
        {
            return IssueContextResult.GraphQlFailure(request.RunId, request.EventType, "Issue payload missing required fields.");
        }

        var authorLogin = issue.Author?.Login;
        if (string.IsNullOrWhiteSpace(authorLogin))
        {
            return IssueContextResult.GraphQlFailure(request.RunId, request.EventType, "Issue author login missing from GraphQL response.");
        }

        IReadOnlyList<CommentSnapshot>? comments = null;
        if (issue.Comments?.Nodes is { Count: > 0 } nodes)
        {
            var snapshots = new List<CommentSnapshot>();
            foreach (var node in nodes)
            {
                if (node is null || string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Author?.Login))
                {
                    continue;
                }

                var body = node.BodyText ?? string.Empty;
                snapshots.Add(CommentSnapshot.Create(node.Id, node.Author.Login, body, node.CreatedAt));
            }

            comments = snapshots;
        }

        var issueSnapshot = IssueSnapshot.Create(
            issue.Id,
            issue.Number,
            issue.Title,
            authorLogin,
            comments);

        return IssueContextResult.Success(request.RunId, request.EventType, issueSnapshot, DateTime.UtcNow);
    }

    private static bool IsInsufficientScopes(GraphQLError? error)
        => error?.Code is not null && error.Code.Equals("INSUFFICIENT_SCOPES", StringComparison.OrdinalIgnoreCase);

        private static string BuildQuery(IssueContextRequest request)
        {
                static string Quote(string value)
                {
                    var encoded = JsonEncodedText.Encode(value ?? string.Empty);
                    return $"\"{encoded}\"";
                }

                var ownerLiteral = Quote(request.Owner);
                var nameLiteral = Quote(request.Name);
                var commentsPageSize = Math.Clamp(request.CommentsPageSize, 1, 20);

                var query = $$"""
                        query IssueContextQuery {
                            repository(owner: {{ownerLiteral}}, name: {{nameLiteral}}) {
                                issue(number: {{request.IssueNumber}}) {
                                    id
                                    number
                                    title
                                    author {
                                        login
                                    }
                                    comments(last: {{commentsPageSize}}) {
                                        totalCount
                                        nodes {
                                            id
                                            author {
                                                login
                                            }
                                            bodyText
                                            createdAt
                                        }
                                    }
                                }
                            }
                        }
                        """;

                return query.Trim();
        }
}
