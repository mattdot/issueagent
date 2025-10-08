using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // For now, use the simple response generator
            // The full AI integration requires proper understanding of the Persistent Agents API
            // which may have changed or requires different patterns
            _logger?.LogInformation("AI response generation requested but using fallback (API integration pending)");
            return Task.FromResult(GenerateSimpleResponse(history, decision));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating AI response - using fallback");
            return Task.FromResult(GenerateSimpleResponse(history, decision));
        }
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
