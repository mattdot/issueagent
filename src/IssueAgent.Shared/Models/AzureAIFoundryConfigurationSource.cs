namespace IssueAgent.Shared.Models;

/// <summary>
/// Tracks where configuration values originated for debugging and error messages.
/// </summary>
public enum AzureAIFoundryConfigurationSource
{
    /// <summary>
    /// Value from GitHub Actions input parameter.
    /// </summary>
    ActionInput,

    /// <summary>
    /// Value from environment variable.
    /// </summary>
    EnvironmentVariable,

    /// <summary>
    /// System default value used.
    /// </summary>
    DefaultValue
}
