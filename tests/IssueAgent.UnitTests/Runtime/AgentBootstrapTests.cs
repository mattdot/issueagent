using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using IssueAgent.Agent.Runtime;
using Xunit;

namespace IssueAgent.UnitTests.Runtime;

public class AgentBootstrapTests
{
    private static readonly MethodInfo ReadTokenMethod = typeof(AgentBootstrap)
        .GetMethod("ReadToken", BindingFlags.Static | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("ReadToken method not found.");

    private static readonly MethodInfo ShouldSkipExecutionMethod = typeof(AgentBootstrap)
        .GetMethod("ShouldSkipExecution", BindingFlags.Static | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("ShouldSkipExecution method not found.");

    [Fact]
    public void ReadToken_PrefersExplicitInput()
    {
        using var scope = new EnvironmentVariableScope(
            ("INPUT_GITHUB_TOKEN", "explicit-token"),
            ("GITHUB_TOKEN", "gh-token"));

        var result = InvokeReadToken();

        result.Should().Be("explicit-token");
    }

    [Fact]
    public void ReadToken_FallsBackToGitHubToken()
    {
        using var scope = new EnvironmentVariableScope(
            ("INPUT_GITHUB_TOKEN", null),
            ("GITHUB_TOKEN", "gh-token"));

        var result = InvokeReadToken();

        result.Should().Be("gh-token");
    }

    private static string? InvokeReadToken()
    {
        return (string?)ReadTokenMethod.Invoke(null, Array.Empty<object?>());
    }

    private static bool InvokeShouldSkipExecution(string eventName, JsonElement payload)
    {
        return (bool)ShouldSkipExecutionMethod.Invoke(null, new object?[] { eventName, payload })!;
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_ForNonCommentEvents()
    {
        var payload = JsonDocument.Parse("{}").RootElement;

        var result = InvokeShouldSkipExecution("issues", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsTrue_WhenSenderIsBot()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""Bot""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_WhenSenderIsUser()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""OWNER""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsTrue_WhenAuthorHasInsufficientPermissions()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""CONTRIBUTOR""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_WhenAuthorIsOwner()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""OWNER""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_WhenAuthorIsMember()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""MEMBER""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_WhenAuthorIsCollaborator()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""COLLABORATOR""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsFalse_WhenAuthorIsMaintainer()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""MAINTAINER""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldSkipExecution_ReturnsTrue_WhenAuthorIsFirstTimeContributor()
    {
        var payload = JsonDocument.Parse(@"{
            ""sender"": {
                ""type"": ""User""
            },
            ""comment"": {
                ""author_association"": ""FIRST_TIME_CONTRIBUTOR""
            }
        }").RootElement;

        var result = InvokeShouldSkipExecution("issue_comment", payload);

        result.Should().BeTrue();
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
