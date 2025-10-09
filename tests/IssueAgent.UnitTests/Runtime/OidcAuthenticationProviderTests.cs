using IssueAgent.Agent.Runtime;
using IssueAgent.Shared.Models;
using Xunit;
using Azure.AI.Agents.Persistent;

namespace IssueAgent.UnitTests.Runtime;

/// <summary>
/// Unit tests for OidcAuthenticationProvider.
/// Verifies OIDC-based authentication logic for Azure AI Foundry.
/// </summary>
public class OidcAuthenticationProviderTests
{
    [Fact]
    public async Task CreateClientAsync_WithValidCredentials_ShouldReturnClient()
    {
        // Arrange
        var provider = new OidcAuthenticationProvider(
            "12345678-1234-1234-1234-123456789012",
            "87654321-4321-4321-4321-210987654321");
        var endpoint = "https://test.services.ai.azure.com/api/projects/test";
        var cts = new CancellationTokenSource();

        // Act
        var client = await provider.CreateClientAsync(endpoint, cts.Token);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<PersistentAgentsClient>(client);
    }

    [Fact]
    public void GetAuthenticationMethodName_ShouldReturnOidcServicePrincipal()
    {
        // Arrange
        var provider = new OidcAuthenticationProvider(
            "12345678-1234-1234-1234-123456789012",
            "87654321-4321-4321-4321-210987654321");

        // Act
        var methodName = provider.GetAuthenticationMethodName();

        // Assert
        Assert.Equal("OIDC (Service Principal)", methodName);
    }

    [Fact]
    public void CreateProvider_WithNullClientId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new OidcAuthenticationProvider(null!, "87654321-4321-4321-4321-210987654321"));
    }

    [Fact]
    public void CreateProvider_WithEmptyClientId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new OidcAuthenticationProvider(string.Empty, "87654321-4321-4321-4321-210987654321"));
    }

    [Fact]
    public void CreateProvider_WithNullTenantId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new OidcAuthenticationProvider("12345678-1234-1234-1234-123456789012", null!));
    }

    [Fact]
    public void CreateProvider_WithEmptyTenantId_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new OidcAuthenticationProvider("12345678-1234-1234-1234-123456789012", string.Empty));
    }

    [Fact]
    public async Task CreateClientAsync_WithNullEndpoint_ShouldThrowArgumentException()
    {
        // Arrange
        var provider = new OidcAuthenticationProvider(
            "12345678-1234-1234-1234-123456789012",
            "87654321-4321-4321-4321-210987654321");
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.CreateClientAsync(null!, cts.Token));
    }

    [Fact]
    public async Task CreateClientAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var provider = new OidcAuthenticationProvider(
            "12345678-1234-1234-1234-123456789012",
            "87654321-4321-4321-4321-210987654321");
        var endpoint = "https://test.services.ai.azure.com/api/projects/test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.CreateClientAsync(endpoint, cts.Token));
    }
}
