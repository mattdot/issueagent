using System.Text.Json.Serialization;

namespace IssueAgent.Agent.GitHub;

public class GitHubCommentRequest
{
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}

public class GitHubCommentResponse
{
    [JsonPropertyName("node_id")]
    public string? NodeId { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(GitHubCommentRequest))]
[JsonSerializable(typeof(GitHubCommentResponse))]
internal partial class GitHubCommentJsonContext : JsonSerializerContext
{
}
