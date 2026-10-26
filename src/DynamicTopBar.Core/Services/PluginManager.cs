using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe plugin manager providing discovery, lifecycle orchestration, version compatibility checks,
/// and crash isolation for modular extensions.
/// </summary>
public sealed class PluginManager : IPluginManager
{
    public static readonly Version CurrentAppVersion = new(1, 0, 0);

    private readonly object _lock = new();
    private readonly IToolRegistry _toolRegistry;
    private readonly ICommandPaletteService _commandPalette;
    private readonly ILoggingService _logger;
    private readonly string _stateFilePath;
    private readonly string _settingsDirectory;

    private readonly Dictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _disabledPluginIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> _pluginSettings = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

    public IReadOnlyList<IPlugin> Plugins
    {
        get
        {
            lock (_lock) return _plugins.Values.ToList();
        }
    }

    public IReadOnlyList<IPlugin> ActivePlugins
    {
        get
        {
            lock (_lock) return _plugins.Values.Where(p => p.State == PluginLifecycleState.Active).ToList();
        }
    }

    public event EventHandler<IPlugin>? PluginStateChanged;

    public PluginManager(
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

        _settingsDirectory = Path.Combine(baseDir, "plugins");
        _stateFilePath = Path.Combine(_settingsDirectory, "plugins_state.json");

        if (!Directory.Exists(_settingsDirectory))
        {
            Directory.CreateDirectory(_settingsDirectory);
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await LoadStateAsync();

        List<IPlugin> pluginsToActivate;
        lock (_lock)
        {
            pluginsToActivate = _plugins.Values
                .Where(p => !_disabledPluginIds.Contains(p.Manifest.Id))
                .ToList();
        }

        foreach (var plugin in pluginsToActivate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TryActivatePluginAsync(plugin, cancellationToken);
        }
    }

    public bool RegisterPlugin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var manifest = plugin.Manifest;

        // Version compatibility verification
        if (Version.TryParse(manifest.MinAppVersion, out var minVer) && minVer > CurrentAppVersion)
        {
            _logger.LogWarning($"Plugin '{manifest.Id}' requires app version {manifest.MinAppVersion}, but current is {CurrentAppVersion}. Registration rejected.", null, "PluginManager");
            return false;
        }

        lock (_lock)
        {
            if (_plugins.ContainsKey(manifest.Id))
            {
                _logger.LogWarning($"Plugin '{manifest.Id}' is already registered.", null, "PluginManager");
                return false;
            }

            _plugins[manifest.Id] = plugin;
        }

        _logger.LogInfo($"Plugin registered: {manifest.Name} ({manifest.Id}) v{manifest.Version}", "PluginManager");
        PluginStateChanged?.Invoke(this, plugin);
        return true;
    }

    public bool UnregisterPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;

        IPlugin? plugin;
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out plugin))
            {
                return false;
            }

            _plugins.Remove(pluginId);
        }

        if (plugin.State == PluginLifecycleState.Active)
        {
            try
            {
                plugin.ShutdownAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error shutting down plugin '{pluginId}' during unregistration", ex, "PluginManager");
            }
        }

        // Unregister associated tools from ToolRegistry
        foreach (var tool in plugin.GetTools())
        {
            _toolRegistry.Unregister(tool.Id);
        }

        // Unregister associated commands from CommandPalette
        foreach (var cmd in plugin.GetCommands())
        {
            _commandPalette.Unregister(cmd.Id);
        }

        plugin.Dispose();
        PluginStateChanged?.Invoke(this, plugin);
        return true;
    }

    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        IPlugin? plugin;
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out plugin))
            {
                return false;
            }

            _disabledPluginIds.Remove(pluginId);
        }

        SaveState();
        return await TryActivatePluginAsync(plugin);
    }

    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        IPlugin? plugin;
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out plugin))
            {
                return false;
            }

            _disabledPluginIds.Add(pluginId);
        }

        SaveState();

        try
        {
            await plugin.ShutdownAsync();

            // Unregister tools from ToolRegistry
            foreach (var tool in plugin.GetTools())
            {
                _toolRegistry.Unregister(tool.Id);
            }

            // Unregister commands from CommandPalette
            foreach (var cmd in plugin.GetCommands())
            {
                _commandPalette.Unregister(cmd.Id);
            }

            PluginStateChanged?.Invoke(this, plugin);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error disabling plugin '{pluginId}'", ex, "PluginManager");
            return false;
        }
    }

    public IPlugin? GetPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return null;
        lock (_lock)
        {
            _plugins.TryGetValue(pluginId, out var plugin);
            return plugin;
        }
    }

    public bool IsPluginEnabled(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;
        lock (_lock)
        {
            return _plugins.ContainsKey(pluginId) && !_disabledPluginIds.Contains(pluginId);
        }
    }

    public Task SavePluginSettingsAsync(string pluginId, Dictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_lock)
        {
            _pluginSettings[pluginId] = new Dictionary<string, string>(settings, StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            string filePath = Path.Combine(_settingsDirectory, $"{pluginId}.settings.json");
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{filePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to persist settings for plugin '{pluginId}'", ex, "PluginManager");
        }

        return Task.CompletedTask;
    }

    public Dictionary<string, string> GetPluginSettings(string pluginId)
    {
        lock (_lock)
        {
            if (_pluginSettings.TryGetValue(pluginId, out var settings))
            {
                return new Dictionary<string, string>(settings);
            }
        }

        // Try load from disk
        string filePath = Path.Combine(_settingsDirectory, $"{pluginId}.settings.json");
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    lock (_lock)
                    {
                        _pluginSettings[pluginId] = loaded;
                    }
                    return new Dictionary<string, string>(loaded);
                }
            }
            catch { }
        }

        return new Dictionary<string, string>();
    }

    private async Task<bool> TryActivatePluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        // Fault isolation boundary: unhandled exceptions inside a plugin will never crash the host
        try
        {
            var settings = GetPluginSettings(plugin.Manifest.Id);
            var context = new PluginContext
            {
                ToolRegistry = _toolRegistry,
                CommandPalette = _commandPalette,
                Logger = _logger,
                Settings = settings,
                CancellationToken = cancellationToken
            };

            bool ok = await plugin.InitializeAsync(context);
            if (!ok)
            {
                _logger.LogWarning($"Plugin '{plugin.Manifest.Id}' declined activation or failed initialization.", null, "PluginManager");
                PluginStateChanged?.Invoke(this, plugin);
                return false;
            }

            // Register plugin's declared tools
            foreach (var tool in plugin.GetTools())
            {
                _toolRegistry.Register(tool);
            }

            // Register plugin's declared palette commands
            foreach (var cmd in plugin.GetCommands())
            {
                _commandPalette.RegisterCommand(cmd);
            }

            _logger.LogInfo($"Plugin successfully activated: {plugin.Manifest.Id}", "PluginManager");
            PluginStateChanged?.Invoke(this, plugin);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fault Isolation Triggered: Plugin '{plugin.Manifest.Id}' crashed during activation.", ex, "PluginManager");
            plugin.MarkFailed(ex.Message);
            PluginStateChanged?.Invoke(this, plugin);
            return false;
        }
    }

    private Task LoadStateAsync()
    {
        if (!File.Exists(_stateFilePath)) return Task.CompletedTask;

        try
        {
            string json = File.ReadAllText(_stateFilePath);
            var disabled = JsonSerializer.Deserialize<List<string>>(json);
            if (disabled != null)
            {
                lock (_lock)
                {
                    _disabledPluginIds.Clear();
                    foreach (var id in disabled) _disabledPluginIds.Add(id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to load plugin state file. Falling back to defaults.", ex, "PluginManager");
        }

        return Task.CompletedTask;
    }

    private void SaveState()
    {
        try
        {
            List<string> disabledCopy;
            lock (_lock)
            {
                disabledCopy = _disabledPluginIds.ToList();
            }

            string json = JsonSerializer.Serialize(disabledCopy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{_stateFilePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _stateFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save plugin state.", ex, "PluginManager");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        List<IPlugin> pluginsToShutdown;
        lock (_lock)
        {
            pluginsToShutdown = _plugins.Values.ToList();
            _plugins.Clear();
        }

        foreach (var p in pluginsToShutdown)
        {
            try
            {
                p.ShutdownAsync().GetAwaiter().GetResult();
                p.Dispose();
            }
            catch { }
        }
    }
}
