using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using IssueAgent.Agent.Runtime;

namespace IssueAgent.UnitTests.Runtime;

public class AgentBootstrapTests
{
    private static readonly MethodInfo ReadTokenMethod = typeof(AgentBootstrap)
        .GetMethod("ReadToken", BindingFlags.Static | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("ReadToken method not found.");

    [Fact]
    public void ReadToken_PrefersHyphenatedInput()
    {
        using var scope = new EnvironmentVariableScope(
            ("INPUT_GITHUB-TOKEN", "hyphen-token"),
            ("INPUT_GITHUB_TOKEN", null),
            ("GITHUB_TOKEN", null));

        var result = InvokeReadToken();

        result.Should().Be("hyphen-token");
    }

    [Fact]
    public void ReadToken_FallsBackToUnderscoreVariant()
    {
        using var scope = new EnvironmentVariableScope(
            ("INPUT_GITHUB-TOKEN", null),
            ("INPUT_GITHUB_TOKEN", "underscore-token"),
            ("GITHUB_TOKEN", null));

        var result = InvokeReadToken();

        result.Should().Be("underscore-token");
    }

    [Fact]
    public void ReadToken_FallsBackToGitHubToken()
    {
        using var scope = new EnvironmentVariableScope(
            ("INPUT_GITHUB-TOKEN", null),
            ("INPUT_GITHUB_TOKEN", null),
            ("GITHUB_TOKEN", "gh-token"));

        var result = InvokeReadToken();

        result.Should().Be("gh-token");
    }

    private static string? InvokeReadToken()
    {
        return (string?)ReadTokenMethod.Invoke(null, Array.Empty<object?>());
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly IReadOnlyList<(string Key, string? OriginalValue)> _snapshot;

        public EnvironmentVariableScope(params (string Key, string? Value)[] variables)
        {
            _snapshot = variables
                .Select(variable =>
                {
                    var original = Environment.GetEnvironmentVariable(variable.Key);
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                    return (variable.Key, original);
                })
                .ToArray();
        }

        public void Dispose()
        {
            foreach (var (key, value) in _snapshot)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
