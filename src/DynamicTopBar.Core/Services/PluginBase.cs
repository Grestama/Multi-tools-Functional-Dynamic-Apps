using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Convenient abstract base class for DynamicTopBar plugins providing standardized state transitions and disposal.
/// </summary>
public abstract class PluginBase : IPlugin
{
    private bool _isDisposed;

    public abstract PluginManifest Manifest { get; }
    public PluginLifecycleState State { get; protected set; } = PluginLifecycleState.Loaded;
    public string? FailureReason { get; protected set; }

    public virtual Task<bool> InitializeAsync(PluginContext context)
    {
        State = PluginLifecycleState.Active;
        return Task.FromResult(true);
    }

    public virtual Task ShutdownAsync()
    {
        State = PluginLifecycleState.Disabled;
        return Task.CompletedTask;
    }

    public virtual IEnumerable<IDesktopTool> GetTools() => Array.Empty<IDesktopTool>();

    public virtual IEnumerable<PaletteCommand> GetCommands() => Array.Empty<PaletteCommand>();

    public void MarkFailed(string reason)
    {
        State = PluginLifecycleState.Failed;
        FailureReason = reason;
    }

    public virtual void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
