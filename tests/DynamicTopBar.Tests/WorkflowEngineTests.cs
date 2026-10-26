using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class WorkflowEngineTests : IDisposable
{
    private readonly string _tempFile;
    private readonly ToolRegistry _toolRegistry;
    private readonly CommandPaletteService _commandPalette;
    private readonly LoggingService _logger;

    public WorkflowEngineTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dtb_wf_test_{Guid.NewGuid():N}.json");
        _logger = new LoggingService();
        _commandPalette = new CommandPaletteService();
        _toolRegistry = new ToolRegistry();
    }

    public void Dispose()
    {
        _commandPalette.Dispose();
        _logger.Dispose();
        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch { }
    }

    private sealed class EchoTool : IDesktopTool
    {
        public string Id { get; }
        public string DisplayName => Id;
        public string Description => "Echo tool";
        public string Category => "Test";
        public string IconGlyph => "\uE71D";
        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => Array.Empty<ToolParameterDefinition>();

        private readonly Func<ToolContext, ToolResult> _action;

        public EchoTool(string id, Func<ToolContext, ToolResult> action)
        {
            Id = id;
            _action = action;
        }

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_action(context));
        }
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_SequentialStepsWithTokenSubstitution_Succeeds()
    {
        // Step 1 outputs "Hello", Step 2 prepends "Echo: " to {prev.output}
        _toolRegistry.Register(new EchoTool("tool-step1", ctx => ToolResult.Ok("Hello")));
        _toolRegistry.Register(new EchoTool("tool-step2", ctx => ToolResult.Ok($"Echo: {ctx.InputText}")));

        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var workflow = new WorkflowDefinition
        {
            Id = "test-chain",
            Name = "Chained Workflow",
            Steps = new List<WorkflowStep>
            {
                new() { ToolId = "tool-step1", Name = "Step 1" },
                new() { ToolId = "tool-step2", Name = "Step 2", InputTextTemplate = "{prev.output}" }
            }
        };

        await engine.SaveWorkflowAsync(workflow);
        var result = await engine.ExecuteWorkflowAsync("test-chain");

        Assert.True(result.Success);
        Assert.Equal(2, result.StepResults.Count);
        Assert.Equal("Hello", result.StepResults[0].OutputText);
        Assert.Equal("Echo: Hello", result.StepResults[1].OutputText);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_DestructiveStepDeclined_AbortsWorkflow()
    {
        _toolRegistry.Register(new EchoTool("tool-safe", ctx => ToolResult.Ok("Safe step done")));
        _toolRegistry.Register(new EchoTool("tool-danger", ctx => ToolResult.Ok("Dangerous deleted")));

        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var workflow = new WorkflowDefinition
        {
            Id = "test-destructive",
            Name = "Dangerous Workflow",
            Steps = new List<WorkflowStep>
            {
                new() { ToolId = "tool-safe", Name = "Safe Step" },
                new() { ToolId = "tool-danger", Name = "Dangerous Step", IsDestructive = true }
            }
        };

        await engine.SaveWorkflowAsync(workflow);

        // User declines destructive confirmation
        var result = await engine.ExecuteWorkflowAsync("test-destructive", confirmDestructiveStep: step => Task.FromResult(false));

        Assert.False(result.Success);
        Assert.Single(result.StepResults); // Only step 1 ran
        Assert.Contains("declined", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_StepFailsWithoutContinueOnError_StopsImmediately()
    {
        _toolRegistry.Register(new EchoTool("tool-fail", ctx => ToolResult.Fail("Disk full")));
        _toolRegistry.Register(new EchoTool("tool-subsequent", ctx => ToolResult.Ok("Subsequent done")));

        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var workflow = new WorkflowDefinition
        {
            Id = "test-failure-stop",
            Name = "Failure Stop Workflow",
            Steps = new List<WorkflowStep>
            {
                new() { ToolId = "tool-fail", Name = "Failing Step", ContinueOnError = false },
                new() { ToolId = "tool-subsequent", Name = "Subsequent Step" }
            }
        };

        await engine.SaveWorkflowAsync(workflow);
        var result = await engine.ExecuteWorkflowAsync("test-failure-stop");

        Assert.False(result.Success);
        Assert.Single(result.StepResults);
        Assert.Equal("Disk full", result.StepResults[0].ErrorMessage);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_StepFailsWithContinueOnError_RunsNextStep()
    {
        _toolRegistry.Register(new EchoTool("tool-fail-ok", ctx => ToolResult.Fail("Soft failure")));
        _toolRegistry.Register(new EchoTool("tool-after-fail", ctx => ToolResult.Ok("Recovered output")));

        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var workflow = new WorkflowDefinition
        {
            Id = "test-continue-error",
            Name = "Continue Workflow",
            Steps = new List<WorkflowStep>
            {
                new() { ToolId = "tool-fail-ok", Name = "Soft Fail Step", ContinueOnError = true },
                new() { ToolId = "tool-after-fail", Name = "Step After Fail" }
            }
        };

        await engine.SaveWorkflowAsync(workflow);
        var result = await engine.ExecuteWorkflowAsync("test-continue-error");

        Assert.True(result.Success);
        Assert.Equal(2, result.StepResults.Count);
        Assert.False(result.StepResults[0].Success);
        Assert.True(result.StepResults[1].Success);
        Assert.Equal("Recovered output", result.StepResults[1].OutputText);
    }

    [Fact]
    public async Task WorkflowEngine_RegistersWorkflowsInCommandPalette()
    {
        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var workflow = new WorkflowDefinition
        {
            Id = "cmd-wf",
            Name = "Palette Workflow",
            Steps = new List<WorkflowStep>()
        };

        await engine.SaveWorkflowAsync(workflow);

        Assert.Contains(_commandPalette.Commands, c => c.Id == "cmd-workflow-cmd-wf");

        engine.DeleteWorkflow("cmd-wf");
        Assert.DoesNotContain(_commandPalette.Commands, c => c.Id == "cmd-workflow-cmd-wf");
    }

    [Fact]
    public void ValidateWorkflow_DetectsMissingToolsAndInvalidSteps()
    {
        using var engine = new WorkflowEngine(_toolRegistry, _commandPalette, _logger, _tempFile);

        var invalidWf = new WorkflowDefinition
        {
            Id = "",
            Name = "",
            Steps = new List<WorkflowStep>
            {
                new() { ToolId = "non-existent-tool" }
            }
        };

        var validation = engine.ValidateWorkflow(invalidWf);

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
    }
}
