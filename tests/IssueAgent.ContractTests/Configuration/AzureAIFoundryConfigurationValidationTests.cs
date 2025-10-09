using IssueAgent.Shared.Models;
using Xunit;
using ValidationException = IssueAgent.Shared.Models.ValidationException;

namespace IssueAgent.ContractTests.Configuration;

/// <summary>
/// Contract tests for Azure AI Foundry configuration validation.
/// These tests verify that configuration validation follows the contract defined in
/// specs/002-ai-foundry-connectivity/contracts/configuration-validation-contract.md
/// </summary>
public class AzureAIFoundryConfigurationValidationTests
{
    [Fact]
    public void ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini",
            ApiVersion = "2025-04-01-preview",
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };

        // Act & Assert
        var exception = Record.Exception(() => config.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void MissingEndpoint_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = null!,
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-4o-mini"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure AI Foundry endpoint is required", exception.Message);
        Assert.Contains("azure_foundry_endpoint", exception.Message);
        Assert.Contains("AZURE_AI_FOUNDRY_ENDPOINT", exception.Message);
    }

    [Fact]
    public void InvalidEndpointFormat_HttpInsteadOfHttps_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "http://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-4o-mini"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure AI Foundry endpoint must be a valid HTTPS URL", exception.Message);
        Assert.Contains("https://<resource>.services.ai.azure.com/api/projects/<project>", exception.Message);
    }

    [Fact]
    public void InvalidEndpointDomain_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://example.com/wrong",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure AI Foundry endpoint must end with", exception.Message);
        Assert.Contains(".services.ai.azure.com/api/projects/", exception.Message);
    }

    [Fact]
    public void MissingClientId_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure client ID is required", exception.Message);
        Assert.Contains("azure_client_id", exception.Message);
        Assert.Contains("AZURE_CLIENT_ID", exception.Message);
    }

    [Fact]
    public void MissingTenantId_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "",
            ModelDeploymentName = "gpt-5-mini"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure tenant ID is required", exception.Message);
        Assert.Contains("azure_tenant_id", exception.Message);
        Assert.Contains("AZURE_TENANT_ID", exception.Message);
    }

    [Fact]
    public void InvalidModelDeploymentName_SpecialCharacters_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini@latest"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Model deployment name must contain only alphanumeric characters and hyphens", exception.Message);
    }

    [Fact]
    public void InvalidApiVersionFormat_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini",
            ApiVersion = "2024.10.21"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Azure AI Foundry API version must be in format YYYY-MM-DD", exception.Message);
        Assert.Contains("2025-04-01-preview", exception.Message);
    }

    [Fact]
    public void FutureApiVersionDate_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini",
            ApiVersion = "2099-12-31"
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("API version date cannot be in the future", exception.Message);
    }

    [Fact]
    public void InvalidConnectionTimeout_Negative_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini",
            ConnectionTimeout = TimeSpan.FromSeconds(-5)
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Connection timeout must be greater than 0", exception.Message);
    }

    [Fact]
    public void ExcessiveConnectionTimeout_ShouldThrowValidationException()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
            ClientId = "12345678-1234-1234-1234-123456789012",
            TenantId = "87654321-4321-4321-4321-210987654321",
            ModelDeploymentName = "gpt-5-mini",
            ConnectionTimeout = TimeSpan.FromMinutes(6)
        };

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => config.Validate());
        Assert.Contains("Connection timeout must not exceed 5 minutes", exception.Message);
    }

    [Fact]
    public void NullModelDeployment_ShouldUseDefault()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
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
    public void MinimalValidConfiguration_ShouldApplyDefaults()
    {
        // Arrange
        var config = new AzureAIFoundryConfiguration
        {
            Endpoint = "https://ai-foundry-test.services.ai.azure.com/api/projects/test-project",
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
}
