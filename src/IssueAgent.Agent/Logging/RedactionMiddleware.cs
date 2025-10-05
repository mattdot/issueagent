using System;
using System.Collections.Generic;

namespace IssueAgent.Agent.Logging;

public static class RedactionMiddleware
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "authorization",
        "auth",
        "token",
        "github_token",
        "access_token",
        "api_key",
        "apikey",
        "azure_ai_foundry_api_key",
        "input_azure_ai_foundry_api_key"
    };

    private const string RedactedValue = "[REDACTED]";

    public static IReadOnlyDictionary<string, object?> RedactPayload(IReadOnlyDictionary<string, object?> payload)
    {
        if (payload is null || payload.Count == 0)
        {
            return payload ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var sanitized = new Dictionary<string, object?>(payload.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in payload)
        {
            if (SensitiveKeys.Contains(key))
            {
                sanitized[key] = RedactedValue;
                continue;
            }

            sanitized[key] = value;
        }

        return sanitized;
    }
}
