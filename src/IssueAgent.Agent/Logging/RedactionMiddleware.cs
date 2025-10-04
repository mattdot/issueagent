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
    "github-token",
    "github_token",
        "access-token"
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
