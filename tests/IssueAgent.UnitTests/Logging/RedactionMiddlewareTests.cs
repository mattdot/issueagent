using System.Collections.Generic;
using FluentAssertions;
using IssueAgent.Agent.Logging;
using Xunit;

namespace IssueAgent.UnitTests.Logging;

public class RedactionMiddlewareTests
{
    [Fact]
    public void RedactPayload_ShouldMaskTokenFields()
    {
        var payload = new Dictionary<string, object?>
        {
            ["authorization"] = "Bearer 123",
            ["query"] = "mutation { }"
        };

        var redacted = RedactionMiddleware.RedactPayload(payload);

        redacted["authorization"].Should().Be("[REDACTED]");
        redacted["query"].Should().Be(payload["query"]);
    }
}
