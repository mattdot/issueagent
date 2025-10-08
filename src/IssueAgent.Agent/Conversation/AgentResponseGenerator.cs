using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.Agents.Persistent;
using IssueAgent.Shared.Models;
using Microsoft.Extensions.Logging;

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

    public Task<string> GenerateResponseAsync(
        IReadOnlyList<ConversationMessage> history,
        ResponseDecisionResult decision,
        CancellationToken cancellationToken = default)
    {
        // If no AI client configured, fall back to simple responses
        if (_agentClient == null || string.IsNullOrWhiteSpace(_modelDeploymentName))
        {
            _logger?.LogWarning("Azure AI Foundry not configured - using simple fallback responses");
            return Task.FromResult(GenerateSimpleResponse(history, decision));
        }

        try
        {
            _logger?.LogInformation("Generating AI response using Azure AI Foundry with model {ModelDeployment}", _modelDeploymentName);
            
            // TODO: Implement full Persistent Agents API integration
            // The Azure.AI.Agents.Persistent API surface needs to be properly explored
            // Current understanding:
            // - PersistentAgentsClient.Administration provides admin operations
            // - Need to determine correct methods for:
            //   1. Creating an agent with model + instructions
            //   2. Creating a thread
            //   3. Adding messages to thread
            //   4. Running the agent on the thread
            //   5. Retrieving agent responses
            //   6. Cleanup
            //
            // For now, using fallback with detailed logging for debugging
            
            _logger?.LogInformation("Conversation context: {MessageCount} messages", history.Count);
            foreach (var msg in history)
            {
                _logger?.LogDebug("[{Role}] {Author}: {TextPreview}...", 
                    msg.Role, 
                    msg.AuthorName, 
                    msg.Text.Substring(0, Math.Min(100, msg.Text.Length)));
            }
            
            // Fall back to simple response until API is fully implemented
            _logger?.LogWarning("Full AI integration pending - using fallback response");
            return Task.FromResult(GenerateSimpleResponse(history, decision));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating AI response - using fallback");
            return Task.FromResult(GenerateSimpleResponse(history, decision));
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
