namespace IssueAgent.Shared.Models;

/// <summary>
/// Categorizes connection failures for error handling and logging.
/// Maps to specific error messages to provide actionable guidance to users.
/// </summary>
public enum ConnectionErrorCategory
{
    /// <summary>
    /// Required configuration parameter not provided (e.g., endpoint or API key missing).
    /// </summary>
    MissingConfiguration,

    /// <summary>
    /// Configuration parameter format invalid (e.g., malformed endpoint URL).
    /// </summary>
    InvalidConfiguration,

    /// <summary>
    /// API key rejected by Azure AI Foundry (expired or invalid key).
    /// </summary>
    AuthenticationFailure,

    /// <summary>
    /// Connection attempt exceeded timeout (unreachable endpoint).
    /// </summary>
    NetworkTimeout,

    /// <summary>
    /// Network-level connectivity failure (e.g., DNS resolution failure).
    /// </summary>
    NetworkError,

    /// <summary>
    /// Specified model deployment doesn't exist in the Azure AI Foundry project.
    /// </summary>
    ModelNotFound,

    /// <summary>
    /// Azure AI Foundry service quota exceeded (rate limiting).
    /// </summary>
    QuotaExceeded,

    /// <summary>
    /// API version deprecated or not supported.
    /// </summary>
    ApiVersionUnsupported,

    /// <summary>
    /// Unexpected error not categorized above.
    /// </summary>
    UnknownError
}
