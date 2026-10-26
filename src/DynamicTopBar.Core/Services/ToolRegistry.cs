using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe registry and execution dispatcher for Universal Tools Engine utilities.
/// </summary>
public sealed class ToolRegistry : IToolRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, IDesktopTool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

    public IReadOnlyList<IDesktopTool> Tools
    {
        get
        {
            lock (_lock)
            {
                return _tools.Values.OrderBy(t => t.Category).ThenBy(t => t.DisplayName).ToList();
            }
        }
    }

    public event EventHandler<IDesktopTool>? ToolRegistered;
    public event EventHandler<string>? ToolUnregistered;

    public ToolRegistry(IEnumerable<IDesktopTool>? initialTools = null)
    {
        if (initialTools != null)
        {
            foreach (var tool in initialTools)
            {
                Register(tool);
            }
        }
    }

    public void Register(IDesktopTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        if (string.IsNullOrWhiteSpace(tool.Id))
        {
            throw new ArgumentException("Tool ID cannot be null or whitespace.", nameof(tool));
        }

        lock (_lock)
        {
            _tools[tool.Id] = tool;
        }

        ToolRegistered?.Invoke(this, tool);
    }

    public bool Unregister(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId)) return false;

        bool removed;
        lock (_lock)
        {
            removed = _tools.Remove(toolId);
        }

        if (removed)
        {
            ToolUnregistered?.Invoke(this, toolId);
        }

        return removed;
    }

    public IDesktopTool? GetTool(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId)) return null;

        lock (_lock)
        {
            return _tools.TryGetValue(toolId, out var tool) ? tool : null;
        }
    }

    public IReadOnlyList<IDesktopTool> GetToolsByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return Array.Empty<IDesktopTool>();

        lock (_lock)
        {
            return _tools.Values
                .Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.DisplayName)
                .ToList();
        }
    }

    public IReadOnlyList<string> GetCategories()
    {
        lock (_lock)
        {
            return _tools.Values
                .Select(t => t.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }
    }

    public async Task<ToolResult> ExecuteToolAsync(string toolId, ToolContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var tool = GetTool(toolId);
        if (tool == null)
        {
            return ToolResult.Fail($"Tool with ID '{toolId}' is not registered.");
        }

        try
        {
            return await tool.ExecuteAsync(context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Fail("Operation was canceled.");
        }
        catch (Exception ex)
        {
            return ToolResult.Fail($"Execution error in '{tool.DisplayName}': {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_lock)
        {
            _tools.Clear();
        }
    }
}
