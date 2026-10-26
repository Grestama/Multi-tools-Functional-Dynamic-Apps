using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing discovery, lifecycle, version verification, and fault isolation for plugins.
/// </summary>
public interface IPluginManager : IDisposable
{
    IReadOnlyList<IPlugin> Plugins { get; }
    IReadOnlyList<IPlugin> ActivePlugins { get; }

    event EventHandler<IPlugin>? PluginStateChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    bool RegisterPlugin(IPlugin plugin);
    bool UnregisterPlugin(string pluginId);
    Task<bool> EnablePluginAsync(string pluginId);
    Task<bool> DisablePluginAsync(string pluginId);
    IPlugin? GetPlugin(string pluginId);
    bool IsPluginEnabled(string pluginId);
    Task SavePluginSettingsAsync(string pluginId, Dictionary<string, string> settings);
    Dictionary<string, string> GetPluginSettings(string pluginId);
}
