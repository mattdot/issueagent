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
        // Implementation based on provided examples:
        // The examples show using extension methods CreateAIAgentAsync and GetAIAgentAsync
        // on PersistentAgentsClient, but these are not available in current package versions.
        //
        // Expected implementation (from examples):
        // AIAgent agent = await _agentClient!.CreateAIAgentAsync(
        //     model: _modelDeploymentName!,
        //     name: "issueagent",
        //     instructions: IssueAgentSystemPrompt.Prompt,
        //     cancellationToken: cancellationToken);
        //
        // AgentThread thread = agent.GetNewThread();
        // string prompt = BuildConversationPrompt(history);
        // var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
        // return response.ToString();
        //
        // Current status: Awaiting package version clarification
        // - Added Azure.AI.OpenAI and OpenAI packages as suggested
        // - Extension methods still not found in compilation
        // - May need newer package versions from different NuGet feed
        
        _logger?.LogWarning("AI Agent integration pending - extension methods not available in current package versions");
        _logger?.LogInformation("Package versions: Azure.AI.Agents.Persistent 1.1.0, Microsoft.Agents.AI 1.0.0-preview.251002.1");
        _logger?.LogInformation("See GitHub issue comment thread for status");
        
        // Fall back to simple response
        await Task.CompletedTask; // Satisfy async signature
        throw new NotImplementedException(
            "AIAgent extension methods (CreateAIAgentAsync, GetAIAgentAsync) not available. " +
            "Waiting for package version clarification - see PR comment thread.");
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
