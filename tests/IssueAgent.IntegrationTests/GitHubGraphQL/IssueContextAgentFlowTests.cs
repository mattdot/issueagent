using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using IssueAgent.Agent.Runtime;
using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.IntegrationTests.GitHubGraphQL;

public class IssueContextAgentFlowTests : IClassFixture<IssueContextAgentFixture>
{
    private readonly IssueContextAgentFixture _fixture;

    public IssueContextAgentFlowTests(IssueContextAgentFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ProduceSuccessResult_WithStartupMetrics()
    {
        _fixture.Reset();
        _fixture.Server.EnqueueResponse(GraphQlResponses.SuccessResponse());
        var agent = _fixture.CreateAgent();

        var request = CreateRequest();
        var result = await agent.ExecuteAsync(request, token: "token", CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.Success);
        _fixture.MetricsRecorder.Recorded.Should().BeTrue();
    }

    [Fact]
    public async Task Should_MapPermissionDeniedToGuidance()
    {
        _fixture.Reset();
        _fixture.Server.EnqueueResponse(GraphQlResponses.PermissionDeniedResponse());
        var agent = _fixture.CreateAgent();

        var request = CreateRequest();
        var result = await agent.ExecuteAsync(request, token: "token", CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.PermissionDenied);
        result.Message.Should().Contain("permissions");
    }

    [Fact]
    public async Task Should_MapMissingIssueToGraphQlFailure()
    {
        _fixture.Reset();
        _fixture.Server.EnqueueResponse(GraphQlResponses.MissingIssueResponse());
        var agent = _fixture.CreateAgent();

        var request = CreateRequest(issueNumber: 404);
        var result = await agent.ExecuteAsync(request, token: "token", CancellationToken.None);

        result.Status.Should().Be(IssueContextStatus.GraphQLFailure);
        result.Message.Should().Contain("404");
    }

    [Fact]
    public async Task Should_FailWhenTokenMissing()
    {
        _fixture.Reset();
        var agent = _fixture.CreateAgent();

        var request = CreateRequest();
        await Assert.ThrowsAsync<InvalidOperationException>(() => agent.ExecuteAsync(request, token: null, CancellationToken.None));
    }

    private static IssueContextRequest CreateRequest(int issueNumber = 42)
        => new(
            Owner: "octo-org",
            Name: "issue-agent",
            IssueNumber: issueNumber,
            CommentsPageSize: 5,
            RunId: "run-123",
            EventType: IssueEventType.IssueCommentCreated);

    private static class GraphQlResponses
    {
        public static object SuccessResponse()
        {
            var now = DateTime.UtcNow;
            return new
            {
                data = new
                {
                    repository = new
                    {
                        issue = new
                        {
                            id = "ISSUE_ID",
                            number = 42,
                            title = "Example Issue",
                            author = new { login = "octocat" },
                            comments = new
                            {
                                totalCount = 2,
                                nodes = new object[]
                                {
                                    new
                                    {
                                        id = "COMMENT_1",
                                        bodyText = "First comment",
                                        createdAt = now.AddMinutes(-5).ToString("O"),
                                        author = new { login = "hubot" }
                                    },
                                    new
                                    {
                                        id = "COMMENT_2",
                                        bodyText = "Second comment",
                                        createdAt = now.AddMinutes(-1).ToString("O"),
                                        author = new { login = "monalisa" }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public static object PermissionDeniedResponse()
            => new
            {
                errors = new object[]
                {
                    new
                    {
                        message = "read:issues permissions required",
                        extensions = new { code = "INSUFFICIENT_SCOPES" }
                    }
                }
            };

        public static object MissingIssueResponse()
            => new
            {
                data = new
                {
                    repository = new
                    {
                        issue = (object?)null
                    }
                }
            };
    }
}
