using System;
using System.Collections.Generic;
using System.Threading;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Declared capability permissions for modular plugins.
/// </summary>
[Flags]
public enum PluginPermission
{
    None = 0,
    FileSystem = 1 << 0,
    Clipboard = 1 << 1,
    Notifications = 1 << 2,
    MediaControl = 1 << 3,
    Network = 1 << 4
}

/// <summary>
/// Execution state of a plugin within the application lifecycle.
/// </summary>
public enum PluginLifecycleState
{
    Unloaded,
    Loaded,
    Active,
    Disabled,
    Failed
}

/// <summary>
/// Static metadata descriptor for a DynamicTopBar plugin.
/// </summary>
public record PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; } = "1.0.0";
    public string Author { get; init; } = "DynamicTopBar Ecosystem";
    public string Description { get; init; } = string.Empty;
    public string MinAppVersion { get; init; } = "1.0.0";
    public PluginPermission Permissions { get; init; } = PluginPermission.None;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string? SettingsSchemaJson { get; init; }
}

/// <summary>
/// Context passed to plugins during initialization providing scoped access to core services.
/// </summary>
public record PluginContext
{
    public required IToolRegistry ToolRegistry { get; init; }
    public required ICommandPaletteService CommandPalette { get; init; }
    public required ILoggingService Logger { get; init; }
    public Dictionary<string, string> Settings { get; init; } = new();
    public CancellationToken CancellationToken { get; init; } = default;
}
