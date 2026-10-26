using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Planned tool execution synthesized by the AI Assistant from a natural language user query.
/// </summary>
public record AiIntentPlan
{
    public required string UserQuery { get; init; }
    public required string TargetToolId { get; init; }
    public string IntentDescription { get; init; } = string.Empty;
    public double Confidence { get; init; } = 1.0;
    public Dictionary<string, object?> ExtractedParameters { get; init; } = new();
    public string? ExtractedInputText { get; init; }
    public bool RequiresConfirmation { get; init; }
    public string? ConfirmationPrompt { get; init; }
}

/// <summary>
/// Outcome of the AI Assistant intent resolution and tool execution pipeline.
/// </summary>
public record AiExecutionResult
{
    public bool Success { get; init; }
    public string Query { get; init; } = string.Empty;
    public AiIntentPlan? Plan { get; init; }
    public ToolResult? ToolResult { get; init; }
    public string AssistantResponse { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}
