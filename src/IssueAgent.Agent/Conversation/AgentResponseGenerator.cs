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
        // If no AI client configured, fall back to simple responses
        if (_agentClient == null || string.IsNullOrWhiteSpace(_modelDeploymentName))
        {
            _logger?.LogWarning("Azure AI Foundry not configured - using simple fallback responses");
            return GenerateSimpleResponse(history, decision);
        }

        try
        {
            _logger?.LogInformation("Generating AI response using Azure AI Foundry with model {ModelDeployment}", _modelDeploymentName);
            _logger?.LogInformation("Conversation context: {MessageCount} messages", history.Count);
            
            return await GenerateAIResponseAsync(history, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating AI response - using fallback");
            return GenerateSimpleResponse(history, decision);
        }
    }

    private async Task<string> GenerateAIResponseAsync(
        IReadOnlyList<ConversationMessage> history,
        CancellationToken cancellationToken)
    {
        // Attempted implementation with Microsoft.Agents.AI 1.0.0-preview.251007.1
        // and Azure.AI.Agents.Persistent 1.2.0-beta.1:
        //
        // AIAgent agent = await _agentClient!.CreateAIAgentAsync(
        //     model: _modelDeploymentName!,
        //     name: "issueagent",
        //     instructions: IssueAgentSystemPrompt.Prompt,
        //     cancellationToken: cancellationToken);
        //
        // However, CreateAIAgentAsync extension method not found in compilation.
        // DLL contains "CreateAIAgent" (without Async) but signature/usage unclear.
        //
        // Alternative approach from second example uses:
        // new AzureOpenAIClient(...).GetChatClient(...).CreateAIAgent(...)
        //
        // Awaiting clarification on correct API usage.
        
        _logger?.LogWarning("AI Agent integration pending API clarification");
        _logger?.LogInformation("Using Microsoft.Agents.AI 1.0.0-preview.251007.1, Azure.AI.Agents.Persistent 1.2.0-beta.1");
        
        await Task.CompletedTask;
        throw new NotImplementedException(
            "Extension methods from examples not found. " +
            "Awaiting clarification on API usage - see PR comments.");
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

    private string GenerateSimpleResponse(IReadOnlyList<ConversationMessage> history, ResponseDecisionResult decision)
    {
        // Generate simple acknowledgment based on context
        var latestMessage = history[^1];
        
        // Build a contextual response by analyzing the conversation
        var hasQuestions = history.Any(m => m.Role == Shared.Models.MessageRole.Assistant && m.Text.Contains('?'));
        var previousAgentMessages = history.Count(m => m.Role == Shared.Models.MessageRole.Assistant);
        
        if (decision.Decision == ResponseDecision.MustRespond)
        {
            if (previousAgentMessages == 0)
            {
                // First interaction
                return "Thanks for mentioning me! I'm here to help improve this issue and guide you toward writing world-class user stories and requirements.\n\n" +
                       "To get started, I'd like to understand:\n" +
                       "- What is the user story or goal you're trying to achieve?\n" +
                       "- Who are the actors (users/systems) involved?\n" +
                       "- What are the measurable acceptance criteria?\n" +
                       "- Are there any constraints or dependencies I should know about?";
            }
            else
            {
                // Subsequent interaction
                return "I'm reviewing your message. To help you effectively:\n\n" +
                       "- Please provide any additional context or clarifications\n" +
                       "- Ensure acceptance criteria are specific and measurable\n" +
                       "- List any assumptions or constraints\n\n" +
                       "Let me know what specific aspect you'd like me to help refine.";
            }
        }
        else
        {
            // This shouldn't happen with current logic, but handle gracefully
            return "Thanks for the update! Let me know if you need help refining the requirements or acceptance criteria.";
        }
    }
}
