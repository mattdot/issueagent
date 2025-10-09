using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Shared;

/// <summary>
/// Unit tests for AzureAIFoundryConfiguration validation logic.
/// Verifies that configuration rules are properly enforced.
/// </summary>
public class AzureAIFoundryConfigurationTests
{
    [Theory]
    [InlineData("https://test.services.ai.azure.com/api/projects/test")]
    [InlineData("https://my-resource.services.ai.azure.com/api/projects/my-project")]
    [InlineData("https://test-123.services.ai.azure.com/api/projects/project-456")]
    public void Validate_WithValidEndpoint_ShouldNotThrow(string endpoint)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = endpoint,
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act & Assert
        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyEndpoint_ShouldThrow(string? endpoint)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = endpoint!,
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("endpoint is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://test.services.ai.azure.com/api/projects/test")] // HTTP not HTTPS
    [InlineData("https://example.com/api")] // Wrong domain
    [InlineData("https://test.azure.com/api/projects/test")] // Missing services.ai subdomain
    [InlineData("ftp://test.services.ai.azure.com/api/projects/test")] // Wrong protocol
    public void Validate_WithInvalidEndpointFormat_ShouldThrow(string endpoint)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = endpoint,
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("HTTPS", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithValidClientIdAndTenantId_ShouldNotThrow()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act & Assert
        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyClientId_ShouldThrow(string? clientId)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = clientId!,
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("client ID is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyTenantId_ShouldThrow(string? tenantId)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = tenantId!
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("tenant ID is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("gpt-5-mini")]
    [InlineData("gpt-4o-mini")]
    [InlineData("Phi-4-mini-instruct")]
    [InlineData("model-deployment-123")]
    public void Validate_WithValidModelDeploymentName_ShouldNotThrow(string modelName)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = modelName
        };

        // Act & Assert
        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("gpt-5-mini@latest")] // @ not allowed
    [InlineData("model_deployment")] // underscore not allowed
    [InlineData("model.deployment")] // period not allowed
    [InlineData("model deployment")] // space not allowed
    public void Validate_WithInvalidModelDeploymentName_ShouldThrow(string modelName)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = modelName
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("alphanumeric characters and hyphens", exception.Message);
    }

    [Theory]
    [InlineData("2025-04-01-preview")]
    [InlineData("2024-10-21")]
    [InlineData("2024-06-01")]
    public void Validate_WithValidApiVersion_ShouldNotThrow(string apiVersion)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ApiVersion = apiVersion
        };

        // Act & Assert
        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("2024.10.21")] // Wrong format
    [InlineData("2024/10/21")] // Wrong separator
    [InlineData("10-21-2024")] // Wrong order
    [InlineData("invalid")] // Not a date
    public void Validate_WithInvalidApiVersionFormat_ShouldThrow(string apiVersion)
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ApiVersion = apiVersion
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("YYYY-MM-DD", exception.Message);
    }

    [Fact]
    public void Validate_WithNullModelDeployment_ShouldApplyDefault()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = null!
        };

        // Act
        config.Validate();

        // Assert
        Assert.Equal("gpt-5-mini", config.ModelDeploymentName);
    }

    [Fact]
    public void Validate_WithMinimalConfiguration_ShouldApplyAllDefaults()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321"
        };

        // Act
        config.Validate();

        // Assert
        Assert.Equal("gpt-5-mini", config.ModelDeploymentName);
        Assert.Equal("2025-04-01-preview", config.ApiVersion);
        Assert.Equal(TimeSpan.FromSeconds(30), config.ConnectionTimeout);
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ShouldThrow()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ConnectionTimeout = TimeSpan.FromSeconds(-5)
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("greater than 0", exception.Message);
    }

    [Fact]
    public void Validate_WithExcessiveTimeout_ShouldThrow()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://test.services.ai.azure.com/api/projects/test",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ConnectionTimeout = TimeSpan.FromMinutes(10)
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("5 minutes", exception.Message);
    }
}
