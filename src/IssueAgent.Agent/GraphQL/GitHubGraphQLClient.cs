using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Octokit.GraphQL;

namespace IssueAgent.Agent.GraphQL;

public class GitHubGraphQLClient : IGraphQLClient
{
    private readonly Connection _connection;
    private readonly ILogger<GitHubGraphQLClient> _logger;

    public GitHubGraphQLClient(
        string token,
        ILogger<GitHubGraphQLClient> logger,
        string? productName = null,
        string? productVersion = null,
        Uri? endpoint = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("GitHub token must be provided.", nameof(token));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var header = new ProductHeaderValue(productName ?? "issueagent", productVersion ?? "1.0");
        _connection = endpoint is null
            ? new Connection(header, token)
            : new Connection(header, endpoint, token);
    }

    public async Task<T> QueryAsync<T>(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query must be provided.", nameof(query));
        }

        _logger.LogDebug("Executing GitHub GraphQL query: {Query}", query);

        var rawResponse = await _connection.Run(query, cancellationToken).ConfigureAwait(false);

        if (typeof(T) == typeof(IssueContextQueryResponse))
        {
            var materialized = DeserializeIssueContextResponse(rawResponse);
            return (T)(object)materialized;
        }

        throw new NotSupportedException($"GraphQL client does not support deserializing type {typeof(T)}.");
    }

    private static IssueContextQueryResponse DeserializeIssueContextResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var repository = root.TryGetProperty("data", out var dataElement)
            ? ParseRepository(dataElement)
            : null;

        var errors = root.TryGetProperty("errors", out var errorsElement)
            ? ParseErrors(errorsElement)
            : null;

        return new IssueContextQueryResponse
        {
            Repository = repository,
            Errors = errors
        };
    }

    private static IssueContextQueryResponse.RepositoryData? ParseRepository(JsonElement dataElement)
    {
        if (!dataElement.TryGetProperty("repository", out var repositoryElement) || repositoryElement.ValueKind == JsonValueKind.Null)
        {
            return new IssueContextQueryResponse.RepositoryData { Issue = null };
        }

        return new IssueContextQueryResponse.RepositoryData
        {
            Issue = ParseIssue(repositoryElement)
        };
    }

    private static IssueContextQueryResponse.Issue? ParseIssue(JsonElement repositoryElement)
    {
        if (!repositoryElement.TryGetProperty("issue", out var issueElement) || issueElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new IssueContextQueryResponse.Issue
        {
            Id = issueElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null,
            Number = issueElement.TryGetProperty("number", out var numberElement) ? numberElement.GetInt32() : 0,
            Title = issueElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null,
            Author = ParseActor(issueElement, "author"),
            Comments = ParseComments(issueElement)
        };
    }

    private static IssueContextQueryResponse.Actor? ParseActor(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var actorElement) || actorElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new IssueContextQueryResponse.Actor
        {
            Login = actorElement.TryGetProperty("login", out var loginElement) ? loginElement.GetString() : null
        };
    }

    private static IssueContextQueryResponse.CommentConnection? ParseComments(JsonElement issueElement)
    {
        if (!issueElement.TryGetProperty("comments", out var commentsElement) || commentsElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new IssueContextQueryResponse.CommentConnection
        {
            TotalCount = commentsElement.TryGetProperty("totalCount", out var totalCountElement) ? totalCountElement.GetInt32() : 0,
            Nodes = ParseCommentNodes(commentsElement)
        };
    }

    private static IReadOnlyList<IssueContextQueryResponse.CommentNode>? ParseCommentNodes(JsonElement commentsElement)
    {
        if (!commentsElement.TryGetProperty("nodes", out var nodesElement) || nodesElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var nodes = new List<IssueContextQueryResponse.CommentNode>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            if (nodeElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            nodes.Add(new IssueContextQueryResponse.CommentNode
            {
                Id = nodeElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null,
                BodyText = nodeElement.TryGetProperty("bodyText", out var bodyElement) ? bodyElement.GetString() : null,
                CreatedAt = ReadDateTime(nodeElement, "createdAt"),
                Author = ParseActor(nodeElement, "author")
            });
        }

        return nodes;
    }

    private static IReadOnlyList<GraphQLError>? ParseErrors(JsonElement errorsElement)
    {
        if (errorsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var errors = new List<GraphQLError>();

        foreach (var errorElement in errorsElement.EnumerateArray())
        {
            if (errorElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var message = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
            var code = ExtractErrorCode(errorElement);

            errors.Add(new GraphQLError(code, message));
        }

        return errors;
    }

    private static string? ExtractErrorCode(JsonElement errorElement)
    {
        if (errorElement.TryGetProperty("extensions", out var extensionsElement) && extensionsElement.ValueKind == JsonValueKind.Object)
        {
            if (extensionsElement.TryGetProperty("code", out var codeElement))
            {
                return codeElement.GetString();
            }
        }

        if (errorElement.TryGetProperty("type", out var typeElement))
        {
            return typeElement.GetString();
        }

        return null;
    }

    private static DateTime ReadDateTime(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return DateTime.MinValue;
        }

        if (value.TryGetDateTime(out var parsed))
        {
            return parsed;
        }

        if (DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out parsed))
        {
            return parsed;
        }

        return DateTime.MinValue;
    }
}
