using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// A single sequential action step within a composite workflow.
/// </summary>
public record WorkflowStep
{
    public string StepId { get; init; } = Guid.NewGuid().ToString("N");
    public required string ToolId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; init; } = new();
    public string? InputTextTemplate { get; init; } // Supports token substitution e.g. "{prev.output}"
    public bool IsDestructive { get; init; } = false;
    public bool ContinueOnError { get; init; } = false;
}

/// <summary>
/// Definition of a multi-step user automated workflow.
/// </summary>
public record WorkflowDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "\uE8B7";
    public string? ShortcutHint { get; init; }
    public List<WorkflowStep> Steps { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Execution output for an individual workflow step.
/// </summary>
public record StepExecutionResult
{
    public required string StepId { get; init; }
    public required string ToolId { get; init; }
    public required string StepName { get; init; }
    public bool Success { get; init; }
    public string? OutputText { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Summary report of the full workflow execution.
/// </summary>
public record WorkflowExecutionResult
{
    public required string WorkflowId { get; init; }
    public required string WorkflowName { get; init; }
    public bool Success { get; init; }
    public IReadOnlyList<StepExecutionResult> StepResults { get; init; } = Array.Empty<StepExecutionResult>();
    public string? FinalOutput { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public bool WasCancelled { get; init; }
}

/// <summary>
/// Validation result for workflow configuration prior to execution.
/// </summary>
public record WorkflowValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}
