using IssueAgent.Agent.Runtime;
using IssueAgent.Shared.Models;
using Xunit;
using Azure.AI.Agents.Persistent;

namespace IssueAgent.UnitTests.Runtime;

/// <summary>
/// Unit tests for ApiKeyAuthenticationProvider.
/// Verifies API key-based authentication logic for Azure AI Foundry.
/// </summary>
public class ApiKeyAuthenticationProviderTests
{
    [Fact]
    public async Task CreateClientAsync_WithValidCredentials_ShouldReturnClient()
    {
        // Arrange
        var provider = new ApiKeyAuthenticationProvider("test-api-key-0123456789abcdef0123456789abcdef");
        var endpoint = "https://test.services.ai.azure.com/api/projects/test";
        var cts = new CancellationTokenSource();

        // Act
        var client = await provider.CreateClientAsync(endpoint, cts.Token);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<PersistentAgentsClient>(client);
    }

    [Fact]
    public void GetAuthenticationMethodName_ShouldReturnApiKey()
    {
        // Arrange
        var provider = new ApiKeyAuthenticationProvider("test-api-key-0123456789abcdef0123456789abcdef");

        // Act
        var methodName = provider.GetAuthenticationMethodName();

        // Assert
        Assert.Equal("API Key", methodName);
    }

    [Fact]
    public void CreateClientAsync_WithNullApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ApiKeyAuthenticationProvider(null!));
    }

    [Fact]
    public void CreateClientAsync_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ApiKeyAuthenticationProvider(string.Empty));
    }

    [Fact]
    public async Task CreateClientAsync_WithNullEndpoint_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new ApiKeyAuthenticationProvider("test-api-key-0123456789abcdef0123456789abcdef");
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.CreateClientAsync(null!, cts.Token));
    }

    [Fact]
    public async Task CreateClientAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var provider = new ApiKeyAuthenticationProvider("test-api-key-0123456789abcdef0123456789abcdef");
        var endpoint = "https://test.services.ai.azure.com/api/projects/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CreateClientAsync(endpoint, cts.Token));
    }
}
