using IssueAgent.Shared.Models;
using Xunit;

namespace IssueAgent.UnitTests.Shared;

/// <summary>
/// Unit tests for ConnectionErrorCategory enum and error categorization logic.
/// Verifies that errors are properly categorized for user-friendly messaging.
/// </summary>
public class ConnectionErrorCategoryTests
{
    [Fact]
    public void ConnectionErrorCategory_ShouldHaveAllExpectedValues()
    {
        // Assert - verify all categories from data-model.md exist
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.MissingConfiguration));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.InvalidConfiguration));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.AuthenticationFailure));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.NetworkTimeout));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.NetworkError));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.ModelNotFound));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.QuotaExceeded));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.ApiVersionUnsupported));
        Assert.True(Enum.IsDefined(typeof(ConnectionErrorCategory), ConnectionErrorCategory.UnknownError));
    }

    [Fact]
    public void ConnectionErrorCategory_ShouldHaveExactlyNineValues()
    {
        // Arrange
        var values = Enum.GetValues<ConnectionErrorCategory>();

        // Assert
        Assert.Equal(9, values.Length);
    }

    [Theory]
    [InlineData(ConnectionErrorCategory.MissingConfiguration, "MissingConfiguration")]
    [InlineData(ConnectionErrorCategory.InvalidConfiguration, "InvalidConfiguration")]
    [InlineData(ConnectionErrorCategory.AuthenticationFailure, "AuthenticationFailure")]
    [InlineData(ConnectionErrorCategory.NetworkTimeout, "NetworkTimeout")]
    [InlineData(ConnectionErrorCategory.NetworkError, "NetworkError")]
    [InlineData(ConnectionErrorCategory.ModelNotFound, "ModelNotFound")]
    [InlineData(ConnectionErrorCategory.QuotaExceeded, "QuotaExceeded")]
    [InlineData(ConnectionErrorCategory.ApiVersionUnsupported, "ApiVersionUnsupported")]
    [InlineData(ConnectionErrorCategory.UnknownError, "UnknownError")]
    public void ConnectionErrorCategory_ToString_ShouldReturnCorrectName(
        ConnectionErrorCategory category,
        string expectedName)
    {
        // Act
        var name = category.ToString();

        // Assert
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void AzureAIFoundryConnectionResult_WithSuccess_ShouldHaveClientAndNoError()
    {
        // Arrange & Act
        var result = new AzureAIFoundryConnectionResult
        {
            IsSuccess = true,
            Client = null, // Will be actual client in real implementation
            ErrorMessage = null,
            ErrorCategory = null,
            AttemptedEndpoint = "...test-project",
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(500)
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCategory);
    }

    [Fact]
    public void AzureAIFoundryConnectionResult_WithFailure_ShouldHaveErrorAndNoClient()
    {
        // Arrange & Act
        var result = new AzureAIFoundryConnectionResult
        {
            IsSuccess = false,
            Client = null,
            ErrorMessage = "Connection failed",
            ErrorCategory = ConnectionErrorCategory.NetworkTimeout,
            AttemptedEndpoint = "...test-project",
            AttemptedAt = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Client);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(ConnectionErrorCategory.NetworkTimeout, result.ErrorCategory);
    }
}
