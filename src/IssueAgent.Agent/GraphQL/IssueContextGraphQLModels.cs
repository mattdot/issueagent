using System;
using System.Collections.Generic;

namespace IssueAgent.Agent.GraphQL;

public sealed record IssueContextQueryResponse
{
    public RepositoryData? Repository { get; init; }
    public IReadOnlyList<GraphQLError>? Errors { get; init; }

    public sealed record RepositoryData
    {
        public Issue? Issue { get; init; }
    }

    public sealed record Issue
    {
        public string? Id { get; init; }
        public int Number { get; init; }
        public string? Title { get; init; }
        public Actor? Author { get; init; }
        public CommentConnection? Comments { get; init; }
    }

    public sealed record Actor
    {
        public string? Login { get; init; }
    }

    public sealed record CommentConnection
    {
        public int TotalCount { get; init; }
        public IReadOnlyList<CommentNode>? Nodes { get; init; }
    }

    public sealed record CommentNode
    {
        public string? Id { get; init; }
        public Actor? Author { get; init; }
        public string? BodyText { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}

public sealed record GraphQLError(string? Code, string? Message);
