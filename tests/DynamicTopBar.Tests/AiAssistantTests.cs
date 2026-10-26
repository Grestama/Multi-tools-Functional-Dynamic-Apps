using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class AiAssistantTests : IDisposable
{
    private readonly ToolRegistry _toolRegistry;
    private readonly LoggingService _logger;
    private readonly DeveloperUtilitiesService _devUtils;
    private readonly FileToolsService _fileTools;

    public AiAssistantTests()
    {
        _logger = new LoggingService();
        _toolRegistry = new ToolRegistry();
        _devUtils = new DeveloperUtilitiesService();
        _fileTools = new FileToolsService();

        // Register all standard desktop tools into registry
        foreach (var tool in StandardDesktopTools.CreateAll(_devUtils, _fileTools))
        {
            _toolRegistry.Register(tool);
        }
    }

    public void Dispose()
    {
        _logger.Dispose();
    }

    [Fact]
    public async Task ProcessQueryAsync_WhenDisabled_RejectsExecutionWithPrivacyNotice()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        Assert.False(assistant.IsEnabled); // Privacy by default

        var result = await assistant.ProcessQueryAsync("generate uuid");

        Assert.False(result.Success);
        Assert.Contains("opt-in", result.AssistantResponse, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.ToolResult);
    }

    [Fact]
    public async Task PlanIntentAsync_MapsJsonQueryToFormatTool()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        var plan = await assistant.PlanIntentAsync("prettify json {\"a\":1}");

        Assert.NotNull(plan);
        Assert.Equal("tool-json-format", plan.TargetToolId);
        Assert.True(plan.Confidence > 0.7);
        Assert.Contains("{\"a\":1}", plan.ExtractedInputText);
    }

    [Fact]
    public async Task PlanIntentAsync_MapsGuidQueryToGuidTool()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        var plan = await assistant.PlanIntentAsync("create new guid");

        Assert.NotNull(plan);
        Assert.Equal("tool-uuid", plan.TargetToolId);
        Assert.True(plan.Confidence >= 0.8);
    }

    [Fact]
    public async Task PlanIntentAsync_MapsBase64QueryCorrectly()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        var plan = await assistant.PlanIntentAsync("base64 encode hello world");

        Assert.NotNull(plan);
        Assert.Equal("tool-base64", plan.TargetToolId);
        Assert.Equal("Encode", plan.ExtractedParameters["operation"]);
        Assert.Equal("hello world", plan.ExtractedInputText);
    }

    [Fact]
    public async Task ProcessQueryAsync_RunsToolAndReturnsResult()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        var result = await assistant.ProcessQueryAsync("generate a new guid");

        Assert.True(result.Success);
        Assert.NotNull(result.ToolResult);
        Assert.True(result.ToolResult.Success);
        Assert.True(Guid.TryParse(result.ToolResult.OutputText?.Trim(), out _));
    }

    [Fact]
    public async Task ProcessQueryAsync_ConfirmationDecline_CancelsSafely()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        // Query matching a destructive tool (e.g., batch rename)
        var result = await assistant.ProcessQueryAsync("batch rename files in C:\\Test", userConfirmationCallback: p => Task.FromResult(false));

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.AssistantResponse, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessQueryAsync_UnknownQuery_ReturnsHelpfulGuidance()
    {
        using var assistant = new AiAssistantService(_toolRegistry, _logger);
        assistant.IsEnabled = true;

        var result = await assistant.ProcessQueryAsync("quantum teleport my monitor");

        Assert.False(result.Success);
        Assert.Contains("could not resolve", result.AssistantResponse, StringComparison.OrdinalIgnoreCase);
    }
}
