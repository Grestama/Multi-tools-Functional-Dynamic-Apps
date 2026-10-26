using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing workflow definitions, persistence, step validation, and sequential execution.
/// </summary>
public interface IWorkflowEngine : IDisposable
{
    IReadOnlyList<WorkflowDefinition> Workflows { get; }
    event EventHandler<IReadOnlyList<WorkflowDefinition>>? WorkflowsChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        string workflowId,
        Func<WorkflowStep, Task<bool>>? confirmDestructiveStep = null,
        CancellationToken cancellationToken = default);

    Task SaveWorkflowAsync(WorkflowDefinition workflow);
    bool DeleteWorkflow(string workflowId);
    WorkflowDefinition? GetWorkflow(string workflowId);
    WorkflowValidationResult ValidateWorkflow(WorkflowDefinition workflow);
}
