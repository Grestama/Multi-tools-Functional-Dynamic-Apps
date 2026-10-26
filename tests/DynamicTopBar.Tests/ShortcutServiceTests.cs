using System;
using System.IO;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class ShortcutServiceTests : IDisposable
{
    private readonly string _tempConfigPath;

    public ShortcutServiceTests()
    {
        _tempConfigPath = Path.Combine(Path.GetTempPath(), $"shortcuts_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath))
        {
            try { File.Delete(_tempConfigPath); } catch { }
        }
    }

    [Fact]
    public void Constructor_LoadsDefaultShortcuts_WhenNoFileExists()
    {
        using var service = new ShortcutService(_tempConfigPath);

        Assert.NotEmpty(service.Shortcuts);
        Assert.Contains(service.Shortcuts, s => s.Id == "command-palette");
        Assert.Contains(service.Shortcuts, s => s.Id == "toggle-overlay");
    }

    [Fact]
    public void ValidateShortcut_RejectsNoneKey()
    {
        using var service = new ShortcutService(_tempConfigPath);

        var result = service.ValidateShortcut(ShortcutModifiers.Control, ShortcutVirtualKey.None);

        Assert.False(result.IsValid);
        Assert.Contains("valid key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateShortcut_RejectsMissingModifiers()
    {
        using var service = new ShortcutService(_tempConfigPath);

        var result = service.ValidateShortcut(ShortcutModifiers.None, ShortcutVirtualKey.P);

        Assert.False(result.IsValid);
        Assert.Contains("modifier key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateShortcut_RejectsReservedOSCombination()
    {
        using var service = new ShortcutService(_tempConfigPath);

        var result = service.ValidateShortcut(ShortcutModifiers.Control | ShortcutModifiers.Alt, ShortcutVirtualKey.Escape);

        Assert.False(result.IsValid);
        Assert.Contains("reserved", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateShortcut_RejectsConflictingCombination()
    {
        using var service = new ShortcutService(_tempConfigPath);

        // Ctrl+Shift+P is default for command-palette
        var result = service.ValidateShortcut(ShortcutModifiers.Control | ShortcutModifiers.Shift, ShortcutVirtualKey.P);

        Assert.False(result.IsValid);
        Assert.Contains("already assigned", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateShortcut_AllowsSameCombinationWhenIgnoringSelf()
    {
        using var service = new ShortcutService(_tempConfigPath);

        var result = service.ValidateShortcut(ShortcutModifiers.Control | ShortcutModifiers.Shift, ShortcutVirtualKey.P, ignoreShortcutId: "command-palette");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateShortcut_PersistsChanges_AndRaisesEvent()
    {
        using var service = new ShortcutService(_tempConfigPath);
        bool eventFired = false;
        service.ShortcutsChanged += (s, e) => eventFired = true;

        bool updated = service.UpdateShortcut("command-palette", ShortcutModifiers.Alt | ShortcutModifiers.Shift, ShortcutVirtualKey.K, true);

        Assert.True(updated);
        Assert.True(eventFired);

        // Verify loaded from new instance
        using var reload = new ShortcutService(_tempConfigPath);
        var shortcut = Assert.Single(reload.Shortcuts, s => s.Id == "command-palette");
        Assert.Equal(ShortcutModifiers.Alt | ShortcutModifiers.Shift, shortcut.Modifiers);
        Assert.Equal(ShortcutVirtualKey.K, shortcut.Key);
    }

    [Fact]
    public void HandleHotKeyTriggered_FiresShortcutTriggered_WithActionId()
    {
        using var service = new ShortcutService(_tempConfigPath);
        string? firedAction = null;
        service.ShortcutTriggered += (s, actionId) => firedAction = actionId;

        service.HandleHotKeyTriggered("command-palette");

        Assert.Equal("open-command-palette", firedAction);
    }

    [Fact]
    public void HandleHotKeyTriggered_DoesNotFire_WhenShortcutDisabled()
    {
        using var service = new ShortcutService(_tempConfigPath);
        service.SetShortcutEnabled("command-palette", false);

        string? firedAction = null;
        service.ShortcutTriggered += (s, actionId) => firedAction = actionId;

        service.HandleHotKeyTriggered("command-palette");

        Assert.Null(firedAction);
    }

    [Fact]
    public void ImportExport_RoundTripsSuccessfully()
    {
        using var service = new ShortcutService(_tempConfigPath);
        service.UpdateShortcut("command-palette", ShortcutModifiers.Control | ShortcutModifiers.Alt, ShortcutVirtualKey.Y, true);

        string json = service.ExportConfiguration();
        Assert.Contains("command-palette", json);

        var newTempPath = Path.Combine(Path.GetTempPath(), $"shortcuts_import_{Guid.NewGuid():N}.json");
        try
        {
            using var importService = new ShortcutService(newTempPath);
            bool success = importService.ImportConfiguration(json);

            Assert.True(success);
            var shortcut = Assert.Single(importService.Shortcuts, s => s.Id == "command-palette");
            Assert.Equal(ShortcutVirtualKey.Y, shortcut.Key);
        }
        finally
        {
            if (File.Exists(newTempPath)) File.Delete(newTempPath);
        }
    }
}
