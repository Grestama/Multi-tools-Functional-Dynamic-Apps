using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class CommandPaletteTests
{
    [Fact]
    public void RegisterAndUnregister_ModifiesCommandCatalog()
    {
        using var palette = new CommandPaletteService();
        var cmd = new PaletteCommand
        {
            Id = "test-cmd",
            Title = "Test Command",
            Action = () => { }
        };

        palette.Register(cmd);
        Assert.Contains(palette.Commands, c => c.Id == "test-cmd");

        bool removed = palette.Unregister("test-cmd");
        Assert.True(removed);
        Assert.DoesNotContain(palette.Commands, c => c.Id == "test-cmd");
    }

    [Fact]
    public void Search_ReturnsMatchingCommands_OrderedByRelevance()
    {
        using var palette = new CommandPaletteService();

        palette.Register(new PaletteCommand
        {
            Id = "cmd-notes",
            Title = "Open Notes Scratchpad",
            Keywords = new[] { "memo", "notes" }
        });

        palette.Register(new PaletteCommand
        {
            Id = "cmd-spotify",
            Title = "Play Spotify Music",
            Keywords = new[] { "audio", "spotify" }
        });

        var results = palette.Search("notes");

        Assert.NotEmpty(results);
        Assert.Equal("cmd-notes", results[0].Id);
    }

    [Fact]
    public void Search_BoostsCommand_WhenActiveModuleContextMatches()
    {
        using var palette = new CommandPaletteService();

        palette.Register(new PaletteCommand
        {
            Id = "cmd-generic-play",
            Title = "Play Action",
            Keywords = new[] { "play" }
        });

        palette.Register(new PaletteCommand
        {
            Id = "cmd-spotify-play",
            Title = "Play Action",
            ModuleContextId = "spotify",
            Keywords = new[] { "play" }
        });

        // Search with active module "spotify"
        var results = palette.Search("play", activeModuleId: "spotify");

        Assert.True(results.Count >= 2);
        Assert.Equal("cmd-spotify-play", results[0].Id);
    }

    [Fact]
    public async Task ExecuteCommandAsync_InvokesSyncAction_AndRaisesEvent()
    {
        using var palette = new CommandPaletteService();
        bool executed = false;
        string? eventCmdId = null;
        palette.CommandExecuted += (s, id) => eventCmdId = id;

        var cmd = new PaletteCommand
        {
            Id = "sync-test",
            Title = "Sync Action",
            Action = () => executed = true
        };
        palette.Register(cmd);

        bool result = await palette.ExecuteCommandAsync("sync-test");

        Assert.True(result);
        Assert.True(executed);
        Assert.Equal("sync-test", eventCmdId);
    }

    [Fact]
    public async Task ExecuteCommandAsync_InvokesAsyncAction()
    {
        using var palette = new CommandPaletteService();
        bool executed = false;

        var cmd = new PaletteCommand
        {
            Id = "async-test",
            Title = "Async Action",
            AsyncAction = async () =>
            {
                await Task.Yield();
                executed = true;
            }
        };
        palette.Register(cmd);

        bool result = await palette.ExecuteCommandAsync("async-test");

        Assert.True(result);
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteCommandAsync_HonorsAvailabilityPredicate()
    {
        using var palette = new CommandPaletteService();
        bool executed = false;

        var cmd = new PaletteCommand
        {
            Id = "disabled-test",
            Title = "Disabled Action",
            AvailabilityPredicate = () => false,
            Action = () => executed = true
        };
        palette.Register(cmd);

        bool result = await palette.ExecuteCommandAsync("disabled-test");

        Assert.False(result);
        Assert.False(executed);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WhenCommandThrows_RaisesCommandFailedEvent()
    {
        using var palette = new CommandPaletteService();
        PaletteCommand? failedCmd = null;
        palette.CommandFailed += (s, cmd) => failedCmd = cmd;

        var cmd = new PaletteCommand
        {
            Id = "failing-cmd",
            Title = "Failing Command",
            Action = () => throw new InvalidOperationException("Simulated crash")
        };
        palette.Register(cmd);

        bool result = await palette.ExecuteCommandAsync("failing-cmd");

        Assert.False(result);
        Assert.NotNull(failedCmd);
        Assert.Equal("failing-cmd", failedCmd.Id);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WhenAsyncActionThrows_RaisesCommandFailedEvent()
    {
        using var palette = new CommandPaletteService();
        PaletteCommand? failedCmd = null;
        palette.CommandFailed += (s, cmd) => failedCmd = cmd;

        var cmd = new PaletteCommand
        {
            Id = "failing-async-cmd",
            Title = "Failing Async Command",
            AsyncAction = () => throw new HttpRequestException("Network failure")
        };
        palette.Register(cmd);

        bool result = await palette.ExecuteCommandAsync("failing-async-cmd");

        Assert.False(result);
        Assert.NotNull(failedCmd);
        Assert.Equal("failing-async-cmd", failedCmd.Id);
    }
}
