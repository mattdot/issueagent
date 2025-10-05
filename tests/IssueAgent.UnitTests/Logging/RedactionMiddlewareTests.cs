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

    [Theory]
    [InlineData("azure_foundry_api_key")]
    [InlineData("azure-foundry-api-key")]
    [InlineData("azure_ai_foundry_api_key")]
    [InlineData("azure-ai-foundry-api-key")]
    [InlineData("input_azure_foundry_api_key")]
    [InlineData("api_key")]
    [InlineData("api-key")]
    [InlineData("apikey")]
    public void RedactPayload_ShouldMaskAzureFoundryApiKeys(string keyName)
    {
        // Arrange
        var payload = new Dictionary<string, object?>
        {
            [keyName] = "supersecretapikey12345678",
            ["safe_field"] = "this is public"
        };

        // Act
        var redacted = RedactionMiddleware.RedactPayload(payload);

        // Assert
        redacted[keyName].Should().Be("[REDACTED]", $"because {keyName} should be redacted");
        redacted["safe_field"].Should().Be("this is public", "because safe_field should not be redacted");
    }

    [Fact]
    public void RedactPayload_ShouldBeCaseInsensitive()
    {
        // Arrange
        var payload = new Dictionary<string, object?>
        {
            ["AZURE_FOUNDRY_API_KEY"] = "key123",
            ["Azure_Foundry_Api_Key"] = "key456"
        };

        // Act
        var redacted = RedactionMiddleware.RedactPayload(payload);

        // Assert
        redacted["AZURE_FOUNDRY_API_KEY"].Should().Be("[REDACTED]");
        redacted["Azure_Foundry_Api_Key"].Should().Be("[REDACTED]");
    }

    [Fact]
    public void RedactPayload_ShouldHandleEmptyPayload()
    {
        // Arrange
        var payload = new Dictionary<string, object?>();

        // Act
        var redacted = RedactionMiddleware.RedactPayload(payload);

        // Assert
        redacted.Should().NotBeNull();
        redacted.Should().BeEmpty();
    }

    [Fact]
    public void RedactPayload_ShouldHandleNullPayload()
    {
        // Act
        var redacted = RedactionMiddleware.RedactPayload(null!);

        // Assert
        redacted.Should().NotBeNull();
        redacted.Should().BeEmpty();
    }
}
