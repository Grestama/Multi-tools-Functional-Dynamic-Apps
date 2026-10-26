using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using DynamicTopBar.Core.Services.Plugins;
using Xunit;

namespace DynamicTopBar.Tests;

public class PluginManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ToolRegistry _toolRegistry;
    private readonly CommandPaletteService _commandPalette;
    private readonly LoggingService _logger;

    public PluginManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dtb_plugins_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

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
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    private sealed class TestPlugin : PluginBase
    {
        private readonly PluginManifest _manifest;
        public override PluginManifest Manifest => _manifest;

        public bool Initialized { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public bool ShouldFailInit { get; set; }
        public bool ShouldThrowInit { get; set; }

        private readonly List<IDesktopTool> _tools = new();
        private readonly List<PaletteCommand> _commands = new();

        public TestPlugin(
            string id = "test-plugin",
            string name = "Test Plugin",
            string minVersion = "1.0.0",
            IReadOnlyList<IDesktopTool>? tools = null,
            IReadOnlyList<PaletteCommand>? commands = null)
        {
            _manifest = new PluginManifest
            {
                Id = id,
                Name = name,
                Version = "1.0.0",
                MinAppVersion = minVersion,
                Author = "Unit Test"
            };

            if (tools != null)
            {
                _tools.AddRange(tools);
            }
            if (commands != null)
            {
                _commands.AddRange(commands);
            }
        }

        public override IEnumerable<IDesktopTool> GetTools() => _tools;
        public override IEnumerable<PaletteCommand> GetCommands() => _commands;

        public override async Task<bool> InitializeAsync(PluginContext context)
        {
            if (ShouldThrowInit) throw new InvalidOperationException("Simulated plugin crash!");
            if (ShouldFailInit) return false;

            Initialized = true;
            return await base.InitializeAsync(context);
        }

        public override async Task ShutdownAsync()
        {
            ShutdownCalled = true;
            await base.ShutdownAsync();
        }
    }

    private sealed class DummyTool : IDesktopTool
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Category => "Test";
        public string Description => "Test tool";
        public string IconGlyph => "\uE71D";
        public IReadOnlyList<ToolParameterDefinition> ParameterDefinitions => Array.Empty<ToolParameterDefinition>();

        public DummyTool(string id, string name)
        {
            Id = id;
            DisplayName = name;
        }

        public Task<ToolResult> ExecuteAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ToolResult.Ok($"Executed {DisplayName}"));
        }
    }

    [Fact]
    public void RegisterPlugin_ValidPlugin_Succeeds()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var plugin = new TestPlugin("sample-id", "Sample Name");

        bool registered = manager.RegisterPlugin(plugin);

        Assert.True(registered);
        Assert.Single(manager.Plugins);
        Assert.Equal("sample-id", manager.Plugins[0].Manifest.Id);
    }

    [Fact]
    public void RegisterPlugin_DuplicateId_ReturnsFalse()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var plugin1 = new TestPlugin("same-id", "Plugin 1");
        var plugin2 = new TestPlugin("same-id", "Plugin 2");

        Assert.True(manager.RegisterPlugin(plugin1));
        Assert.False(manager.RegisterPlugin(plugin2));
        Assert.Single(manager.Plugins);
    }

    [Fact]
    public void RegisterPlugin_IncompatibleVersion_ReturnsFalse()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var plugin = new TestPlugin("future-id", "Future Plugin", minVersion: "99.0.0");

        bool registered = manager.RegisterPlugin(plugin);

        Assert.False(registered);
        Assert.Empty(manager.Plugins);
    }

    [Fact]
    public async Task EnableAndDisable_ManagesLifecycleAndTools()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var dummyTool = new DummyTool("test-tool-1", "Test Tool 1");
        var dummyCmd = new PaletteCommand { Id = "test-cmd-1", Title = "Test Command 1" };
        var plugin = new TestPlugin("lifecycle-plugin", "Lifecycle Plugin", tools: new[] { dummyTool }, commands: new[] { dummyCmd });

        manager.RegisterPlugin(plugin);
        Assert.Equal(PluginLifecycleState.Loaded, plugin.State);

        // Enable plugin
        bool enabled = await manager.EnablePluginAsync("lifecycle-plugin");
        Assert.True(enabled);
        Assert.Equal(PluginLifecycleState.Active, plugin.State);
        Assert.True(plugin.Initialized);
        Assert.NotNull(_toolRegistry.GetTool("test-tool-1"));
        Assert.Contains(_commandPalette.Commands, c => c.Id == "test-cmd-1");

        // Disable plugin
        bool disabled = await manager.DisablePluginAsync("lifecycle-plugin");
        Assert.True(disabled);
        Assert.Equal(PluginLifecycleState.Disabled, plugin.State);
        Assert.True(plugin.ShutdownCalled);
        Assert.Null(_toolRegistry.GetTool("test-tool-1"));
        Assert.DoesNotContain(_commandPalette.Commands, c => c.Id == "test-cmd-1");
    }

    [Fact]
    public async Task FaultIsolation_PluginThrowingOnInit_DoesNotCrashHost()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var crashingPlugin = new TestPlugin("crashing-plugin", "Crashing Plugin")
        {
            ShouldThrowInit = true
        };

        manager.RegisterPlugin(crashingPlugin);
        bool enabled = await manager.EnablePluginAsync("crashing-plugin");

        Assert.False(enabled);
        Assert.Equal(PluginLifecycleState.Failed, crashingPlugin.State);
        Assert.False(crashingPlugin.Initialized);
    }

    [Fact]
    public async Task Settings_PersistAndLoad()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var plugin = new TestPlugin("settings-plugin", "Settings Plugin");
        manager.RegisterPlugin(plugin);

        var settings = new Dictionary<string, string>
        {
            ["ApiEndpoint"] = "https://local.test",
            ["MaxRetries"] = "3"
        };

        await manager.SavePluginSettingsAsync("settings-plugin", settings);
        var loaded = manager.GetPluginSettings("settings-plugin");

        Assert.Equal("https://local.test", loaded["ApiEndpoint"]);
        Assert.Equal("3", loaded["MaxRetries"]);
    }

    [Fact]
    public async Task DeveloperToolkitPlugin_RegistersExpectedCommands()
    {
        using var manager = new PluginManager(_toolRegistry, _commandPalette, _logger, _tempDir);
        var devUtils = new DeveloperUtilitiesService();
        var devPlugin = new DeveloperToolkitPlugin(devUtils);

        manager.RegisterPlugin(devPlugin);
        bool enabled = await manager.EnablePluginAsync(devPlugin.Manifest.Id);

        Assert.True(enabled);
        Assert.Contains(_commandPalette.Commands, c => c.Id == "cmd-dev-jwt-glance");

        await manager.DisablePluginAsync(devPlugin.Manifest.Id);
        Assert.DoesNotContain(_commandPalette.Commands, c => c.Id == "cmd-dev-jwt-glance");
    }
}
