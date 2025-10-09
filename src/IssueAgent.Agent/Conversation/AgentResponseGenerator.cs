using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Agents.Persistent;
using Azure.AI.OpenAI;
using IssueAgent.Shared.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace IssueAgent.Agent.Conversation;

public class AgentResponseGenerator
{
    private readonly PersistentAgentsClient? _agentClient;
    private readonly string? _modelDeploymentName;
    private readonly ILogger<AgentResponseGenerator>? _logger;

    public AgentResponseGenerator(
        PersistentAgentsClient? agentClient = null,
        string? modelDeploymentName = null,
        ILogger<AgentResponseGenerator>? logger = null)
    {
        _agentClient = agentClient;
        _modelDeploymentName = modelDeploymentName;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(
        IReadOnlyList<ConversationMessage> history,
        ResponseDecisionResult decision,
        CancellationToken cancellationToken = default)
    {
        // Azure AI Foundry configuration is required
        if (_agentClient == null)
        {
            throw new InvalidOperationException("Azure AI Foundry client is not configured. Please ensure AZURE_FOUNDRY_PROJECT_ENDPOINT is set and credentials are available.");
        }

        if (string.IsNullOrWhiteSpace(_modelDeploymentName))
        {
            throw new InvalidOperationException("Model deployment name is not configured. Please ensure AZURE_FOUNDRY_PROJECT_DEPLOYMENT_NAME is set.");
        }

        _logger?.LogInformation("Generating AI response using Azure AI Foundry with model {ModelDeployment}", _modelDeploymentName);
        _logger?.LogInformation("Conversation context: {MessageCount} messages", history.Count);

        return await GenerateAIResponseAsync(history, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateAIResponseAsync(
        IReadOnlyList<ConversationMessage> history,
        CancellationToken cancellationToken)
    {
        // Create a persistent agent on the server
        var agentResult = await _agentClient!.Administration.CreateAgentAsync(
            model: _modelDeploymentName!,
            name: "issueagent",
            instructions: IssueAgentSystemPrompt.Prompt,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!agentResult.HasValue)
        {
            throw new InvalidOperationException("Failed to create agent - no value returned");
        }

        var agentId = agentResult.Value.Id;

        try
        {
            _logger?.LogInformation("Created persistent agent with ID: {AgentId}", agentId);

            // Convert PersistentAgentsClient to IChatClient using the agent
            var chatClient = _agentClient.AsIChatClient(agentId);

            // Create an AI agent from the chat client
            var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions
            {
                Name = "issueagent",
                Instructions = IssueAgentSystemPrompt.Prompt
            });

            // Build conversation prompt from history
            var prompt = BuildConversationPrompt(history);

            // Get a new thread for this conversation
            var thread = agent.GetNewThread();

            // Run the agent
            _logger?.LogDebug("Running agent with conversation context...");
            var runResponse = await agent.RunAsync<string>(prompt, thread, serializerOptions: null, options: null, useJsonSchemaResponseFormat: null, cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation("AI response generated successfully");

            // Extract the response text
            return runResponse.Text ?? string.Empty;
        }
        finally
        {
            // Clean up the agent
            try
            {
                await _agentClient.Administration.DeleteAgentAsync(agentId, cancellationToken).ConfigureAwait(false);
                _logger?.LogDebug("Cleaned up agent {AgentId}", agentId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clean up agent {AgentId}", agentId);
            }
        }
    }

    private string BuildConversationPrompt(IReadOnlyList<ConversationMessage> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Previous conversation:");
        sb.AppendLine();

        foreach (var message in history)
        {
            var roleLabel = message.Role == Shared.Models.MessageRole.Assistant ? "Assistant (issueagent)" : $"User ({message.AuthorName})";
            sb.AppendLine($"**{roleLabel}:**");
            sb.AppendLine(message.Text);
            sb.AppendLine();
        }

        sb.AppendLine("## Your task:");
        sb.AppendLine("Based on the conversation above, provide a helpful response following the responding policy in your instructions.");

        return sb.ToString();
    }

}
