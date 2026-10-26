using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Execution engine for multi-step automated workflows with parameter chaining,
/// failure handling policies, confirmation guards for destructive actions, and Command Palette integration.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly object _lock = new();
    private readonly IToolRegistry _toolRegistry;
    private readonly ICommandPaletteService _commandPalette;
    private readonly ILoggingService _logger;
    private readonly string _storageFilePath;

    private readonly List<WorkflowDefinition> _workflows = new();
    private readonly HashSet<string> _registeredPaletteCommandIds = new();
    private bool _isDisposed;

    public IReadOnlyList<WorkflowDefinition> Workflows
    {
        get
        {
            lock (_lock) return _workflows.ToList();
        }
    }

    public event EventHandler<IReadOnlyList<WorkflowDefinition>>? WorkflowsChanged;

    public WorkflowEngine(
        IToolRegistry toolRegistry,
        ICommandPaletteService commandPalette,
        ILoggingService logger,
        string? baseStorageDirectory = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _commandPalette = commandPalette ?? throw new ArgumentNullException(nameof(commandPalette));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string baseDir = baseStorageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar");

        _storageFilePath = Path.Combine(baseDir, "workflows.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadWorkflowsAsync();

        lock (_lock)
        {
            if (_workflows.Count == 0)
            {
                // Prepopulate standard default workflows
                _workflows.AddRange(CreateDefaultWorkflows());
                SaveWorkflows();
            }
        }

        SynchronizeCommandPalette();
    }

    public WorkflowValidationResult ValidateWorkflow(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(workflow.Id)) errors.Add("Workflow ID is required.");
        if (string.IsNullOrWhiteSpace(workflow.Name)) errors.Add("Workflow Name is required.");
        if (workflow.Steps == null || workflow.Steps.Count == 0)
        {
            errors.Add("Workflow must contain at least one step.");
        }
        else
        {
            for (int i = 0; i < workflow.Steps.Count; i++)
            {
                var step = workflow.Steps[i];
                if (string.IsNullOrWhiteSpace(step.ToolId))
                {
                    errors.Add($"Step #{i + 1} is missing a ToolId.");
                }
                else if (_toolRegistry.GetTool(step.ToolId) == null)
                {
                    errors.Add($"Step #{i + 1} references uninstalled tool '{step.ToolId}'.");
                }
            }
        }

        return new WorkflowValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        string workflowId,
        Func<WorkflowStep, Task<bool>>? confirmDestructiveStep = null,
        CancellationToken cancellationToken = default)
    {
        WorkflowDefinition? workflow;
        lock (_lock)
        {
            workflow = _workflows.FirstOrDefault(w => string.Equals(w.Id, workflowId, StringComparison.OrdinalIgnoreCase));
        }

        if (workflow == null)
        {
            return new WorkflowExecutionResult
            {
                WorkflowId = workflowId,
                WorkflowName = "Unknown",
                Success = false,
                ErrorMessage = $"Workflow '{workflowId}' not found."
            };
        }

        var sw = Stopwatch.StartNew();
        var stepResults = new List<StepExecutionResult>();
        string? previousOutput = null;

        _logger.LogInfo($"Executing workflow: {workflow.Name} ({workflow.Id}) - {workflow.Steps.Count} steps", "WorkflowEngine");

        for (int i = 0; i < workflow.Steps.Count; i++)
        {
            var step = workflow.Steps[i];
            var stepSw = Stopwatch.StartNew();

            if (cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                return new WorkflowExecutionResult
                {
                    WorkflowId = workflow.Id,
                    WorkflowName = workflow.Name,
                    Success = false,
                    StepResults = stepResults,
                    TotalDuration = sw.Elapsed,
                    WasCancelled = true,
                    ErrorMessage = "Workflow cancelled by caller."
                };
            }

            // Destructive Action Gate
            if (step.IsDestructive && confirmDestructiveStep != null)
            {
                bool confirmed = await confirmDestructiveStep(step);
                if (!confirmed)
                {
                    sw.Stop();
                    _logger.LogWarning($"Workflow step '{step.Name}' cancelled by user confirmation decline.", null, "WorkflowEngine");
                    return new WorkflowExecutionResult
                    {
                        WorkflowId = workflow.Id,
                        WorkflowName = workflow.Name,
                        Success = false,
                        StepResults = stepResults,
                        TotalDuration = sw.Elapsed,
                        ErrorMessage = $"Destructive step '{step.Name}' was declined by user."
                    };
                }
            }

            // Parameter & Input Text chaining
            string? inputText = step.InputTextTemplate;
            if (!string.IsNullOrEmpty(inputText) && previousOutput != null)
            {
                inputText = inputText.Replace("{prev.output}", previousOutput);
            }

            var toolContext = new ToolContext
            {
                InputText = inputText,
                Parameters = new Dictionary<string, object?>(step.Parameters)
            };

            // Execute via ToolRegistry
            ToolResult toolResult;
            try
            {
                toolResult = await _toolRegistry.ExecuteToolAsync(step.ToolId, toolContext, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                return new WorkflowExecutionResult
                {
                    WorkflowId = workflow.Id,
                    WorkflowName = workflow.Name,
                    Success = false,
                    StepResults = stepResults,
                    TotalDuration = sw.Elapsed,
                    WasCancelled = true,
                    ErrorMessage = "Workflow cancelled during tool execution."
                };
            }
            catch (Exception ex)
            {
                toolResult = ToolResult.Fail($"Unhandled step error: {ex.Message}");
            }

            stepSw.Stop();

            var stepRes = new StepExecutionResult
            {
                StepId = step.StepId,
                ToolId = step.ToolId,
                StepName = string.IsNullOrWhiteSpace(step.Name) ? step.ToolId : step.Name,
                Success = toolResult.Success,
                OutputText = toolResult.OutputText,
                ErrorMessage = toolResult.ErrorMessage,
                Duration = stepSw.Elapsed
            };
            stepResults.Add(stepRes);

            if (!toolResult.Success)
            {
                _logger.LogWarning($"Step #{i + 1} '{stepRes.StepName}' failed: {toolResult.ErrorMessage}", null, "WorkflowEngine");
                if (!step.ContinueOnError)
                {
                    sw.Stop();
                    return new WorkflowExecutionResult
                    {
                        WorkflowId = workflow.Id,
                        WorkflowName = workflow.Name,
                        Success = false,
                        StepResults = stepResults,
                        TotalDuration = sw.Elapsed,
                        ErrorMessage = $"Step '{stepRes.StepName}' failed: {toolResult.ErrorMessage}"
                    };
                }
            }
            else
            {
                previousOutput = toolResult.OutputText;
            }
        }

        sw.Stop();
        _logger.LogInfo($"Workflow '{workflow.Name}' finished successfully in {sw.ElapsedMilliseconds} ms", "WorkflowEngine");

        return new WorkflowExecutionResult
        {
            WorkflowId = workflow.Id,
            WorkflowName = workflow.Name,
            Success = true,
            StepResults = stepResults,
            FinalOutput = previousOutput,
            TotalDuration = sw.Elapsed
        };
    }

    public Task SaveWorkflowAsync(WorkflowDefinition workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        lock (_lock)
        {
            int idx = _workflows.FindIndex(w => string.Equals(w.Id, workflow.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _workflows[idx] = workflow;
            }
            else
            {
                _workflows.Add(workflow);
            }
        }

        SaveWorkflows();
        SynchronizeCommandPalette();
        WorkflowsChanged?.Invoke(this, Workflows);
        return Task.CompletedTask;
    }

    public bool DeleteWorkflow(string workflowId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _workflows.RemoveAll(w => string.Equals(w.Id, workflowId, StringComparison.OrdinalIgnoreCase)) > 0;
        }

        if (removed)
        {
            SaveWorkflows();
            SynchronizeCommandPalette();
            WorkflowsChanged?.Invoke(this, Workflows);
        }

        return removed;
    }

    public WorkflowDefinition? GetWorkflow(string workflowId)
    {
        lock (_lock)
        {
            return _workflows.FirstOrDefault(w => string.Equals(w.Id, workflowId, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void SynchronizeCommandPalette()
    {
        lock (_lock)
        {
            foreach (var cmdId in _registeredPaletteCommandIds)
            {
                _commandPalette.Unregister(cmdId);
            }
            _registeredPaletteCommandIds.Clear();

            foreach (var wf in _workflows)
            {
                string cmdId = $"cmd-workflow-{wf.Id}";
                _commandPalette.RegisterCommand(new PaletteCommand
                {
                    Id = cmdId,
                    Title = $"Run Workflow: {wf.Name}",
                    Description = string.IsNullOrWhiteSpace(wf.Description) ? $"Execute {wf.Steps.Count} automated step(s)" : wf.Description,
                    Category = "Workflows",
                    Keywords = new[] { "workflow", "automation", "batch", wf.Name.ToLowerInvariant() },
                    IconGlyph = wf.IconGlyph,
                    ShortcutHint = wf.ShortcutHint,
                    AsyncAction = async () =>
                    {
                        await ExecuteWorkflowAsync(wf.Id);
                    }
                });
                _registeredPaletteCommandIds.Add(cmdId);
            }
        }
    }

    private static List<WorkflowDefinition> CreateDefaultWorkflows()
    {
        return new List<WorkflowDefinition>
        {
            new()
            {
                Id = "workflow-focus-session",
                Name = "Quick Focus Setup",
                Description = "Format note scratchpad and log starting timestamp",
                IconGlyph = "\uE823",
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        ToolId = "tool-timestamp",
                        Name = "Log Start Timestamp",
                        Parameters = new Dictionary<string, object?>()
                    },
                    new()
                    {
                        ToolId = "tool-text-format",
                        Name = "Format Focus Goal",
                        InputTextTemplate = "FOCUS SESSION STARTED AT: {prev.output}",
                        Parameters = new Dictionary<string, object?> { ["mode"] = "Uppercase" }
                    }
                }
            },
            new()
            {
                Id = "workflow-format-json-backup",
                Name = "JSON Validation & Checksum",
                Description = "Format input JSON and compute SHA256 integrity hash",
                IconGlyph = "\uE943",
                Steps = new List<WorkflowStep>
                {
                    new()
                    {
                        ToolId = "tool-json-format",
                        Name = "Prettify JSON",
                        InputTextTemplate = "{\"status\":\"healthy\",\"timestamp\":\"2026-10-25\"}",
                        Parameters = new Dictionary<string, object?> { ["mode"] = "Prettify" }
                    },
                    new()
                    {
                        ToolId = "tool-hash",
                        Name = "Calculate Checksum",
                        InputTextTemplate = "{prev.output}",
                        Parameters = new Dictionary<string, object?> { ["algorithm"] = "SHA256" }
                    }
                }
            }
        };
    }

    private Task LoadWorkflowsAsync()
    {
        if (!File.Exists(_storageFilePath)) return Task.CompletedTask;

        try
        {
            string json = File.ReadAllText(_storageFilePath);
            var list = JsonSerializer.Deserialize<List<WorkflowDefinition>>(json);
            if (list != null)
            {
                lock (_lock)
                {
                    _workflows.Clear();
                    _workflows.AddRange(list);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load workflows from disk", ex, "WorkflowEngine");
        }

        return Task.CompletedTask;
    }

    private void SaveWorkflows()
    {
        try
        {
            List<WorkflowDefinition> copy;
            lock (_lock) copy = _workflows.ToList();

            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{_storageFilePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _storageFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save workflows to disk", ex, "WorkflowEngine");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_lock)
        {
            foreach (var cmdId in _registeredPaletteCommandIds)
            {
                _commandPalette.Unregister(cmdId);
            }
            _registeredPaletteCommandIds.Clear();
        }
    }
}
