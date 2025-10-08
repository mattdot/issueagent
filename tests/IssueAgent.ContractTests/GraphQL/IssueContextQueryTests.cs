using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IssueAgent.Agent.GraphQL;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.ContractTests.GraphQL;

public class IssueContextQueryTests
{
    [Fact]
    public async Task Should_ReturnIssueAndRecentComments()
    {
        var graphQlResponse = IssueResponseFactory.CreateSuccess();
        var client = new FakeGraphQLClient(graphQlResponse);
        var executor = new IssueContextQueryExecutor(client);

        var request = new IssueContextRequest(
            Owner: "octo-org",
            Name: "issue-agent",
            IssueNumber: 42,
            CommentsPageSize: 5,
            RunId: "run-123",
            EventType: IssueEventType.IssueCommentCreated);

        var result = await executor.FetchIssueContextAsync(request, CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.Success);
        result.Issue.Should().NotBeNull();
        result.Issue!.Id.Should().Be("ISSUE_ID");
        result.Issue!.LatestComments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_ReturnPermissionDenied()
    {
        var graphQlResponse = IssueResponseFactory.CreatePermissionDenied();
        var client = new FakeGraphQLClient(graphQlResponse);
        var executor = new IssueContextQueryExecutor(client);

        var request = new IssueContextRequest(
            Owner: "octo-org",
            Name: "issue-agent",
            IssueNumber: 42,
            CommentsPageSize: 5,
            RunId: "run-123",
            EventType: IssueEventType.IssueCommentCreated);

        var result = await executor.FetchIssueContextAsync(request, CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.PermissionDenied);
        result.Message.Should().Contain("workflow permissions");
    }

    [Fact]
    public async Task Should_HandleMissingIssue()
    {
        var graphQlResponse = IssueResponseFactory.CreateMissingIssue();
        var client = new FakeGraphQLClient(graphQlResponse);
        var executor = new IssueContextQueryExecutor(client);

        var request = new IssueContextRequest(
            Owner: "octo-org",
            Name: "issue-agent",
            IssueNumber: 99,
            CommentsPageSize: 5,
            RunId: "run-999",
            EventType: IssueEventType.IssueOpened);

        var result = await executor.FetchIssueContextAsync(request, CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.GraphQLFailure);
        result.Message.Should().Contain("not found");
    }

    private sealed class FakeGraphQLClient : IGraphQLClient
    {
        private readonly object _response;

        public FakeGraphQLClient(object response)
        {
            _response = response;
        }

        public Task<T> QueryAsync<T>(string query, CancellationToken cancellationToken)
            => Task.FromResult((T)_response);
    }

    private static class IssueResponseFactory
    {
        public static IssueContextQueryResponse CreateSuccess()
            => new()
            {
                Repository = new IssueContextQueryResponse.RepositoryData
                {
                    Issue = new IssueContextQueryResponse.Issue
                    {
                        Id = "ISSUE_ID",
                        Number = 42,
                        Title = "Example Issue",
                        Author = new IssueContextQueryResponse.Actor { Login = "octocat" },
                        Comments = new IssueContextQueryResponse.CommentConnection
                        {
                            TotalCount = 2,
                            Nodes = new List<IssueContextQueryResponse.CommentNode>
                            {
                                new()
                                {
                                    Id = "COMMENT_1",
                                    Author = new IssueContextQueryResponse.Actor { Login = "monalisa" },
                                    Body = "First comment",
                                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                                },
                                new()
                                {
                                    Id = "COMMENT_2",
                                    Author = new IssueContextQueryResponse.Actor { Login = "hubot" },
                                    Body = "Second comment",
                                    CreatedAt = DateTime.UtcNow.AddMinutes(-1)
                                }
                            }
                        }
                    }
                }
            };

        public static IssueContextQueryResponse CreatePermissionDenied()
            => new()
            {
                Errors = new[] { new GraphQLError("INSUFFICIENT_SCOPES", "read issues scope required") }
            };

        public static IssueContextQueryResponse CreateMissingIssue()
            => new()
            {
                Repository = new IssueContextQueryResponse.RepositoryData
                {
                    Issue = null
                }
            };
    }
}
