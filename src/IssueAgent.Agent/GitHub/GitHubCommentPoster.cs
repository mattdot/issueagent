using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IssueAgent.Agent.GitHub;

public record PostCommentResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CommentId { get; init; }
    public string? CommentUrl { get; init; }
}

public class GitHubCommentPoster
{
    private const string AgentHeader = "🤖 **issueagent**\n\n";
    private const string SignatureFooter = "\n\n<!-- issueagent-signature -->";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubCommentPoster> _logger;

    public GitHubCommentPoster(string token, ILogger<GitHubCommentPoster> logger, string? apiBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("GitHub token must be provided.", nameof(token));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var baseUrl = !string.IsNullOrWhiteSpace(apiBaseUrl) 
            ? new Uri(apiBaseUrl) 
            : new Uri("https://api.github.com");

        _httpClient = new HttpClient
        {
            BaseAddress = baseUrl
        };

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("issueagent", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<PostCommentResult> PostCommentAsync(
        string owner,
        string repo,
        int issueNumber,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("Owner must be provided.", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(repo))
        {
            throw new ArgumentException("Repo must be provided.", nameof(repo));
        }

        if (issueNumber <= 0)
        {
            throw new ArgumentException("Issue number must be positive.", nameof(issueNumber));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Comment body must be provided.", nameof(body));
        }

        try
        {
            var formattedBody = AgentHeader + body.Trim() + SignatureFooter;
            var url = $"/repos/{owner}/{repo}/issues/{issueNumber}/comments";

            // Use source-generated JSON for AOT compatibility
            var requestBody = new GitHubCommentRequest { Body = formattedBody };
            var json = JsonSerializer.Serialize(requestBody, GitHubCommentJsonContext.Default.GitHubCommentRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Posting comment to issue #{IssueNumber} in {Owner}/{Repo}", issueNumber, owner, repo);

            var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogError(
                    "Failed to post comment: {StatusCode} - {ErrorContent}",
                    response.StatusCode,
                    errorContent);

                return new PostCommentResult
                {
                    Success = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            
            // Parse response using source-generated JSON
            var responseData = JsonSerializer.Deserialize(responseContent, GitHubCommentJsonContext.Default.GitHubCommentResponse);

            _logger.LogInformation("Successfully posted comment: {CommentUrl}", responseData?.HtmlUrl);

            return new PostCommentResult
            {
                Success = true,
                CommentId = responseData?.NodeId,
                CommentUrl = responseData?.HtmlUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while posting comment");
            return new PostCommentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
