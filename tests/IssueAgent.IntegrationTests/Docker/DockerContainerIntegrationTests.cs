using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace IssueAgent.IntegrationTests.Docker;

[Collection("Docker")]
public class DockerContainerIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _imageName = "issue-agent:test";

    public DockerContainerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DockerContainer_ShouldProcessIssueOpenedEvent_WithRealGitHubAPI()
    {
        // Skip if test environment variables are not available
        var testToken = Environment.GetEnvironmentVariable("TEST_PAT");
        var testRepo = Environment.GetEnvironmentVariable("TEST_REPO");

        if (string.IsNullOrWhiteSpace(testToken) || string.IsNullOrWhiteSpace(testRepo))
        {
            _output.WriteLine("Skipping integration test: TEST_PAT and TEST_REPO environment variables are required");
            return;
        }

        // Build the Docker image first
        await BuildDockerImage();

        // Create test event payload for an issue opened event (using issue #2 which exists in test repo)
        var eventPayload = CreateIssueOpenedEventPayload(testRepo, 2);
        var tempEventFile = await CreateTempEventFile(eventPayload);

        try
        {
            // Run the container
            var result = await RunDockerContainer(testToken, testRepo, tempEventFile);

            // Assertions
            _output.WriteLine($"Container exit code: {result.ExitCode}");
            _output.WriteLine($"Container output:\n{result.Output}");

            result.ExitCode.Should().Be(0, "Agent should successfully fetch issue context");

            // Verify key log entries indicate successful execution
            result.Output.Should().Contain("Issue context status: Success", "Agent should report success status");
            result.Output.Should().Contain("StartupDurationMs=", "Agent should report startup metrics");
            result.Output.Should().Contain("issueId=", "Agent should retrieve issue details");
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempEventFile))
            {
                File.Delete(tempEventFile);
            }
        }
    }

    [Fact]
    public async Task DockerContainer_ShouldHandleInvalidToken_Gracefully()
    {
        var testRepo = Environment.GetEnvironmentVariable("TEST_REPO");

        if (string.IsNullOrWhiteSpace(testRepo))
        {
            _output.WriteLine("Skipping integration test: TEST_REPO environment variable is required");
            return;
        }

        await BuildDockerImage();

        var eventPayload = CreateIssueOpenedEventPayload(testRepo, 1);
        var tempEventFile = await CreateTempEventFile(eventPayload);

        try
        {
            // Use invalid token
            var result = await RunDockerContainer("invalid-token", testRepo, tempEventFile);

            _output.WriteLine($"Container exit code: {result.ExitCode}");
            _output.WriteLine($"Container output:\n{result.Output}");

            // Should fail gracefully with non-zero exit code
            result.ExitCode.Should().NotBe(0, "Agent should fail with invalid token");
            result.Output.Should().Contain("Issue context status:", "Agent should report status even on failure");
        }
        finally
        {
            if (File.Exists(tempEventFile))
            {
                File.Delete(tempEventFile);
            }
        }
    }

    [Fact]
    public async Task DockerContainer_ShouldValidateStartupPerformance()
    {
        var testToken = Environment.GetEnvironmentVariable("TEST_PAT");
        var testRepo = Environment.GetEnvironmentVariable("TEST_REPO");

        if (string.IsNullOrWhiteSpace(testToken) || string.IsNullOrWhiteSpace(testRepo))
        {
            _output.WriteLine("Skipping performance test: TEST_PAT and TEST_REPO environment variables are required");
            return;
        }

        await BuildDockerImage();

        var eventPayload = CreateIssueOpenedEventPayload(testRepo, 1);
        var tempEventFile = await CreateTempEventFile(eventPayload);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            var result = await RunDockerContainer(testToken, testRepo, tempEventFile);
            stopwatch.Stop();

            _output.WriteLine($"Total execution time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Container output:\n{result.Output}");

            // Performance assertions for GitHub Actions context
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30_000,
                "Container should complete within 30 seconds for GitHub Actions");

            if (result.ExitCode == 0)
            {
                // Extract startup metrics from logs if successful
                var startupDuration = ExtractStartupDurationFromLogs(result.Output);
                if (startupDuration.HasValue)
                {
                    startupDuration.Value.Should().BeLessThan(5_000,
                        "AOT agent should have cold start under 5 seconds");
                }
            }
        }
        finally
        {
            if (File.Exists(tempEventFile))
            {
                File.Delete(tempEventFile);
            }
        }
    }

    [Fact]
    public async Task DockerContainer_ShouldRespectCustomGraphQLUrl()
    {
        var testToken = Environment.GetEnvironmentVariable("TEST_PAT");
        var testRepo = Environment.GetEnvironmentVariable("TEST_REPO");

        if (string.IsNullOrWhiteSpace(testToken) || string.IsNullOrWhiteSpace(testRepo))
        {
            _output.WriteLine("Skipping custom URL test: TEST_PAT and TEST_REPO environment variables are required");
            return;
        }

        await BuildDockerImage();

        var eventPayload = CreateIssueOpenedEventPayload(testRepo, 2);
        var tempEventFile = await CreateTempEventFile(eventPayload);

        try
        {
            // Test with explicit GITHUB_GRAPHQL_URL pointing to GitHub.com
            var result = await RunDockerContainer(
                testToken,
                testRepo,
                tempEventFile,
                graphqlUrl: "https://api.github.com/graphql");

            _output.WriteLine($"Container exit code: {result.ExitCode}");
            _output.WriteLine($"Container output:\n{result.Output}");

            result.ExitCode.Should().Be(0, "Agent should successfully fetch issue context with custom GraphQL URL");
            result.Output.Should().Contain("Issue context status: Success", "Agent should report success status");
            result.Output.Should().Contain("issueId=", "Agent should retrieve issue details");
        }
        finally
        {
            if (File.Exists(tempEventFile))
            {
                File.Delete(tempEventFile);
            }
        }
    }

    [Fact]
    public async Task DockerContainer_ShouldLoadAzureAIFoundryCredentials_FromActionInputs()
    {
        var testToken = Environment.GetEnvironmentVariable("TEST_PAT");
        var testRepo = Environment.GetEnvironmentVariable("TEST_REPO");
        var azureEndpoint = Environment.GetEnvironmentVariable("TEST_AZURE_AI_FOUNDRY_ENDPOINT");
        var azureApiKey = Environment.GetEnvironmentVariable("TEST_AZURE_AI_FOUNDRY_API_KEY");

        if (string.IsNullOrWhiteSpace(testToken) || string.IsNullOrWhiteSpace(testRepo))
        {
            _output.WriteLine("Skipping Azure AI Foundry test: TEST_PAT and TEST_REPO environment variables are required");
            return;
        }

        if (string.IsNullOrWhiteSpace(azureEndpoint) || string.IsNullOrWhiteSpace(azureApiKey))
        {
            _output.WriteLine("Skipping Azure AI Foundry test: TEST_AZURE_AI_FOUNDRY_ENDPOINT and TEST_AZURE_AI_FOUNDRY_API_KEY environment variables are required");
            return;
        }

        await BuildDockerImage();

        var eventPayload = CreateIssueOpenedEventPayload(testRepo, 2);
        var tempEventFile = await CreateTempEventFile(eventPayload);

        try
        {
            // Test with Azure AI Foundry credentials passed as INPUT_ variables (mimicking GitHub Actions)
            var result = await RunDockerContainerWithAzureFoundry(
                testToken,
                testRepo,
                tempEventFile,
                azureEndpoint,
                azureApiKey);

            _output.WriteLine($"Container exit code: {result.ExitCode}");
            _output.WriteLine($"Container output:\n{result.Output}");

            result.ExitCode.Should().Be(0, "Agent should successfully load Azure AI Foundry credentials");
            result.Output.Should().Contain("Azure AI Foundry connection established",
                "Agent should report successful Azure AI Foundry connection");
        }
        finally
        {
            if (File.Exists(tempEventFile))
            {
                File.Delete(tempEventFile);
            }
        }
    }

    private async Task BuildDockerImage()
    {
        _output.WriteLine("Building Docker image for integration tests...");

        // Check if image already exists
        var checkResult = await RunDockerCommand("images", "-q", _imageName);
        if (!string.IsNullOrWhiteSpace(checkResult.Output))
        {
            _output.WriteLine("Docker image already exists, skipping build");
            return;
        }

        // Build the image from the workspace root
        var workspaceRoot = FindWorkspaceRoot();
        var buildResult = await RunDockerCommand(new[] { "build", "-t", _imageName, "." }, workspaceRoot);

        if (buildResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to build Docker image: {buildResult.Output}");
        }

        _output.WriteLine("Docker image built successfully");
    }

    private async Task<(int ExitCode, string Output)> RunDockerContainer(
        string token,
        string repo,
        string eventFilePath,
        string? graphqlUrl = null,
        string? apiUrl = null)
    {
        var containerId = Guid.NewGuid().ToString("N")[..12];

        // Build environment variables list
        var envVars = new List<string>
        {
            "-e", $"INPUT_GITHUB_TOKEN={token}",
            "-e", $"GITHUB_TOKEN={token}",
            "-e", $"GITHUB_REPOSITORY={repo}",
            "-e", "GITHUB_EVENT_NAME=issues",
            "-e", "GITHUB_EVENT_PATH=/tmp/event.json",
            "-e", $"GITHUB_RUN_ID=test-run-{Guid.NewGuid():N}",
            "-e", "INPUT_COMMENTS_PAGE_SIZE=5"
        };

        // Add optional GraphQL URL
        if (!string.IsNullOrWhiteSpace(graphqlUrl))
        {
            envVars.Add("-e");
            envVars.Add($"GITHUB_GRAPHQL_URL={graphqlUrl}");
        }

        // Add optional API URL
        if (!string.IsNullOrWhiteSpace(apiUrl))
        {
            envVars.Add("-e");
            envVars.Add($"GITHUB_API_URL={apiUrl}");
        }

        // Build full create command
        var createArgs = new List<string> { "create", "--name", containerId };
        createArgs.AddRange(envVars);
        createArgs.Add(_imageName);

        // First, create the container
        var createResult = await RunDockerCommand(createArgs.ToArray());

        if (createResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to create container: {createResult.Output}");
        }

        // Copy the event file into the container
        var copyResult = await RunDockerCommand("cp", eventFilePath, $"{containerId}:/tmp/event.json");
        if (copyResult.ExitCode != 0)
        {
            await RunDockerCommand("rm", "-f", containerId); // Cleanup
            throw new InvalidOperationException($"Failed to copy event file: {copyResult.Output}");
        }

        // Start the container
        var startResult = await RunDockerCommand("start", "-a", containerId);

        // Remove the container
        await RunDockerCommand("rm", "-f", containerId);

        return (startResult.ExitCode, startResult.Output);
    }

    private async Task<(int ExitCode, string Output)> RunDockerContainerWithAzureFoundry(
        string token,
        string repo,
        string eventFilePath,
        string azureEndpoint,
        string azureApiKey,
        string? modelDeployment = null,
        string? apiVersion = null)
    {
        var containerId = Guid.NewGuid().ToString("N")[..12];

        // Build environment variables list
        var envVars = new List<string>
        {
            "-e", $"INPUT_GITHUB_TOKEN={token}",
            "-e", $"GITHUB_TOKEN={token}",
            "-e", $"GITHUB_REPOSITORY={repo}",
            "-e", "GITHUB_EVENT_NAME=issues",
            "-e", "GITHUB_EVENT_PATH=/tmp/event.json",
            "-e", $"GITHUB_RUN_ID=test-run-{Guid.NewGuid():N}",
            "-e", "INPUT_COMMENTS_PAGE_SIZE=5",
            // Azure AI Foundry credentials as INPUT_ variables (mimicking GitHub Actions)
            "-e", $"INPUT_AZURE_AI_FOUNDRY_ENDPOINT={azureEndpoint}",
            "-e", $"INPUT_AZURE_AI_FOUNDRY_API_KEY={azureApiKey}"
        };

        if (!string.IsNullOrWhiteSpace(modelDeployment))
        {
            envVars.Add("-e");
            envVars.Add($"INPUT_AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT={modelDeployment}");
        }

        if (!string.IsNullOrWhiteSpace(apiVersion))
        {
            envVars.Add("-e");
            envVars.Add($"INPUT_AZURE_AI_FOUNDRY_API_VERSION={apiVersion}");
        }

        // Build full create command
        var createArgs = new List<string> { "create", "--name", containerId };
        createArgs.AddRange(envVars);
        createArgs.Add(_imageName);

        // First, create the container
        var createResult = await RunDockerCommand(createArgs.ToArray());

        if (createResult.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to create container: {createResult.Output}");
        }

        // Copy the event file into the container
        var copyResult = await RunDockerCommand("cp", eventFilePath, $"{containerId}:/tmp/event.json");
        if (copyResult.ExitCode != 0)
        {
            await RunDockerCommand("rm", "-f", containerId); // Cleanup
            throw new InvalidOperationException($"Failed to copy event file: {copyResult.Output}");
        }

        // Start the container
        var startResult = await RunDockerCommand("start", "-a", containerId);

        // Remove the container
        await RunDockerCommand("rm", "-f", containerId);

        return (startResult.ExitCode, startResult.Output);
    }

    private async Task<(int ExitCode, string Output)> RunDockerCommand(params string[] args)
    {
        return await RunDockerCommand(args, workingDirectory: null);
    }

    private async Task<(int ExitCode, string Output)> RunDockerCommand(string[] args, string? workingDirectory)
    {
        using var process = new Process();
        process.StartInfo.FileName = "sudo";
        process.StartInfo.Arguments = "docker " + string.Join(" ", args);
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        if (workingDirectory != null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
                _output.WriteLine($"STDOUT: {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
                _output.WriteLine($"STDERR: {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var combinedOutput = outputBuilder.ToString() + errorBuilder.ToString();
        return (process.ExitCode, combinedOutput);
    }

    private static async Task<string> CreateTempEventFile(string jsonContent)
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, jsonContent);

        // Make the file world-readable so the container's non-root user can read it
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Use chmod to ensure the file is readable by everyone
            var chmodProcess = Process.Start("chmod", $"644 {tempFile}");
            if (chmodProcess != null)
            {
                await chmodProcess.WaitForExitAsync();
            }
        }

        return tempFile;
    }

    private static string CreateIssueOpenedEventPayload(string repository, int issueNumber)
    {
        var payload = new
        {
            action = "opened",
            issue = new
            {
                number = issueNumber,
                id = 12345,
                title = "Test issue for integration testing",
                body = "This is a test issue created for Docker integration testing",
                state = "open",
                user = new
                {
                    login = "test-user",
                    id = 67890
                },
                created_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            repository = new
            {
                name = repository.Split('/')[1],
                full_name = repository,
                owner = new
                {
                    login = repository.Split('/')[0]
                }
            }
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static long? ExtractStartupDurationFromLogs(string logs)
    {
        // Look for pattern "StartupDurationMs=123"
        var lines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains("StartupDurationMs="))
            {
                var startIndex = line.IndexOf("StartupDurationMs=") + "StartupDurationMs=".Length;
                var endIndex = line.IndexOf(' ', startIndex);
                if (endIndex == -1) endIndex = line.Length;

                var durationStr = line.Substring(startIndex, endIndex - startIndex);
                if (long.TryParse(durationStr, out var duration))
                {
                    return duration;
                }
            }
        }
        return null;
    }

    private static string FindWorkspaceRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "IssueAgent.sln")))
            {
                return current;
            }
            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new InvalidOperationException("Could not find workspace root containing IssueAgent.sln");
    }
}