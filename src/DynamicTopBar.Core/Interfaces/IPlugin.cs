using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Contract for extensible DynamicTopBar plugins providing modular tools and commands.
/// </summary>
public interface IPlugin : IDisposable
{
    PluginManifest Manifest { get; }
    PluginLifecycleState State { get; }
    string? FailureReason { get; }

    Task<bool> InitializeAsync(PluginContext context);
    Task ShutdownAsync();
    void MarkFailed(string reason);
    IEnumerable<IDesktopTool> GetTools();
    IEnumerable<PaletteCommand> GetCommands();
}
