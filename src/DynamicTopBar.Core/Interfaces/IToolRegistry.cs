using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing discovery, registration, and dispatch of desktop tools.
/// </summary>
public interface IToolRegistry : IDisposable
{
    IReadOnlyList<IDesktopTool> Tools { get; }

    event EventHandler<IDesktopTool>? ToolRegistered;
    event EventHandler<string>? ToolUnregistered;

    void Register(IDesktopTool tool);
    bool Unregister(string toolId);
    IDesktopTool? GetTool(string toolId);
    IReadOnlyList<IDesktopTool> GetToolsByCategory(string category);
    IReadOnlyList<string> GetCategories();
    Task<ToolResult> ExecuteToolAsync(string toolId, ToolContext context, CancellationToken cancellationToken = default);
}
