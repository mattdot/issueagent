using System;
using System.Threading.Tasks;
using IssueAgent.Agent.GraphQL;
using IssueAgent.Agent.Instrumentation;
using IssueAgent.Agent.Runtime;
using IssueAgent.Agent.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IssueAgent.IntegrationTests.GitHubGraphQL;

public sealed class IssueContextAgentFixture : IAsyncLifetime
{
    public IssueContextAgentFixture()
    {
        Server = new FakeGitHubGraphQLServer();
        MetricsRecorder = new RecordingStartupMetricsRecorder();
    }

    public FakeGitHubGraphQLServer Server { get; }

    public RecordingStartupMetricsRecorder MetricsRecorder { get; }

    public IssueContextAgent CreateAgent()
    {
        var client = new GitHubGraphQLClient(
            token: "test-token",
            logger: NullLogger<GitHubGraphQLClient>.Instance,
            productName: "issueagent-tests",
            productVersion: "1.0",
            endpoint: Server.Endpoint);

        var executor = new IssueContextQueryExecutor(client);
        var tokenGuard = new GitHubTokenGuard();
        var historyBuilder = new IssueAgent.Agent.Conversation.ConversationHistoryBuilder("github-actions[bot]");
        var decisionEngine = new IssueAgent.Agent.Conversation.ResponseDecisionEngine();
        var responseGenerator = new IssueAgent.Agent.Conversation.AgentResponseGenerator();

        return new IssueContextAgent(
            tokenGuard,
            executor,
            MetricsRecorder,
            historyBuilder,
            decisionEngine,
            responseGenerator,
            commentPoster: null,
            logger: null);
    }

    public void Reset()
    {
        MetricsRecorder.Reset();
        Server.Reset();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await Server.DisposeAsync();
    }

    public sealed class RecordingStartupMetricsRecorder : StartupMetricsRecorder
    {
        public bool Recorded { get; private set; }
        public TimeSpan LastDuration { get; private set; }

        public void Reset()
        {
            Recorded = false;
            LastDuration = TimeSpan.Zero;
        }

        public override IDisposable BeginMeasurement()
        {
            Reset();
            return base.BeginMeasurement();
        }

        protected override void Record(TimeSpan duration)
        {
            Recorded = true;
            LastDuration = duration;
        }
    }
}
