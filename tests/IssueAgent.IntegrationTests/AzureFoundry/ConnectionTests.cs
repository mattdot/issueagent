using IssueAgent.Shared.Models;
using IssueAgent.Agent.Runtime;
using Xunit;

namespace IssueAgent.IntegrationTests.AzureAIFoundry;

/// <summary>
/// Integration tests for Azure AI Foundry connection scenarios.
/// These tests verify the complete connection flow including validation, authentication, and error handling.
/// Test scenarios defined in specs/002-ai-foundry-connectivity/quickstart.md
/// </summary>
public class ConnectionTests
{
    [Fact]
    public async Task SuccessfulConnection_WithValidCredentials_ShouldConnect()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = GetTestEndpoint(),
            ApiKey = GetTestApiKey(),
            ModelDeploymentName = "gpt-5-mini",
            ApiVersion = "2025-04-01-preview",
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Client);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Duration < TimeSpan.FromSeconds(3), "Connection should complete in <3 seconds");
    }

    [Fact]
    public async Task MissingEndpoint_ShouldFailWithClearError()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = null!,
            ApiKey = "abcdefghijklmnopqrstuvwxyz012345"
        };

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Azure AI Foundry endpoint is required", result.ErrorMessage);
        Assert.Equal(ConnectionErrorCategory.MissingConfiguration, result.ErrorCategory);
    }

    [Fact(Skip = "Requires a fake/invalid endpoint that returns authentication errors - hardcoded test endpoint may not behave as expected")]
    public async Task InvalidApiKey_ShouldFailWithAuthenticationError()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ApiKey = "invalid-api-key-0123456789abcdef",
            ModelDeploymentName = "gpt-5-mini",
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Authentication to Azure AI Foundry failed", result.ErrorMessage);
        Assert.Contains("verify API key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ConnectionErrorCategory.AuthenticationFailure, result.ErrorCategory);
    }

    [Fact]
    public async Task InvalidEndpointFormat_ShouldFailWithValidationError()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "http://example.com/wrong",
            ApiKey = "abcdefghijklmnopqrstuvwxyz012345",
            ModelDeploymentName = "gpt-5-mini"
        };

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("HTTPS URL", result.ErrorMessage);
        Assert.Equal(ConnectionErrorCategory.InvalidConfiguration, result.ErrorCategory);
    }

    [Fact(Skip = "Requires specific model deployment setup - test model may exist on real endpoint")]
    public async Task ModelDeploymentNotFound_ShouldFailWithClearError()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = GetTestEndpoint(),
            ApiKey = GetTestApiKey(),
            ModelDeploymentName = "nonexistent-model",
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Model deployment", result.ErrorMessage);
        Assert.Contains("nonexistent-model", result.ErrorMessage);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Equal(ConnectionErrorCategory.ModelNotFound, result.ErrorCategory);
    }

    [Fact(Skip = "Network timeout test requires unreachable endpoint - may not behave consistently")]
    public async Task NetworkTimeout_ShouldFailAfter30Seconds()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://unreachable.services.ai.azure.com/api/projects/test",
            ApiKey = "abcdefghijklmnopqrstuvwxyz012345",
            ModelDeploymentName = "gpt-5-mini",
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await AgentBootstrap.InitializeAzureAIFoundryAsync(config, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("timed out after 30 seconds", result.ErrorMessage);
        Assert.Contains("network connectivity", result.ErrorMessage);
        Assert.Equal(ConnectionErrorCategory.NetworkTimeout, result.ErrorCategory);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(30), "Should wait for full timeout");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(35), "Should not wait much longer than timeout");
    }

    private static string GetTestEndpoint()
    {
        return Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_ENDPOINT")
            ?? "https://test.services.ai.azure.com/api/projects/test";
    }

    private static string GetTestApiKey()
    {
        return Environment.GetEnvironmentVariable("AZURE_AI_FOUNDRY_API_KEY")
            ?? "test-api-key-0123456789abcdef0123456789abcdef";
    }
}
