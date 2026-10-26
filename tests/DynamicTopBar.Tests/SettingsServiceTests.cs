using System;
using System.IO;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempFile;

    public SettingsServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dtb_settings_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch { }
    }

    [Fact]
    public void LoadPreferences_MissingFile_ReturnsDefaultAll11Features()
    {
        var service = new SettingsService(_tempFile);
        var prefs = service.LoadPreferences();

        Assert.NotNull(prefs);
        Assert.Equal(11, prefs.EnabledFeatureIds.Count);
        Assert.Contains("media", prefs.EnabledFeatureIds);
        Assert.Contains("spotify", prefs.EnabledFeatureIds);
        Assert.Contains("clock", prefs.EnabledFeatureIds);
        Assert.Contains("weather", prefs.EnabledFeatureIds);
        Assert.Contains("todo", prefs.EnabledFeatureIds);
        Assert.Contains("focus", prefs.EnabledFeatureIds);
        Assert.Contains("quick-launch", prefs.EnabledFeatureIds);
        Assert.Contains("system-glance", prefs.EnabledFeatureIds);
        Assert.Contains("calendar", prefs.EnabledFeatureIds);
        Assert.Contains("notes", prefs.EnabledFeatureIds);
        Assert.Contains("clipboard", prefs.EnabledFeatureIds);
    }

    [Fact]
    public void SavePreferences_And_LoadPreferences_PersistsAcrossRestarts()
    {
        var service = new SettingsService(_tempFile);
        var custom = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "clock", "media", "focus" },
            FeatureOrder = new() { "clock", "media", "focus" },
            LastActiveFeatureId = "clock"
        };

        service.SavePreferences(custom);

        var serviceRestarted = new SettingsService(_tempFile);
        var loaded = serviceRestarted.LoadPreferences();

        Assert.Equal(3, loaded.EnabledFeatureIds.Count);
        Assert.Equal("clock", loaded.FeatureOrder[0]);
        Assert.Equal("clock", serviceRestarted.GetLastActiveFeatureId());
    }

    [Fact]
    public void SetFeatureEnabled_UpdatesAndSavesImmediately()
    {
        var service = new SettingsService(_tempFile);

        service.SetFeatureEnabled("spotify", false);
        var loaded = service.LoadPreferences();
        Assert.DoesNotContain("spotify", loaded.EnabledFeatureIds);

        service.SetFeatureEnabled("spotify", true);
        var loaded2 = service.LoadPreferences();
        Assert.Contains("spotify", loaded2.EnabledFeatureIds);
    }

    [Fact]
    public void SetFeatureOrder_ReordersSequence()
    {
        var service = new SettingsService(_tempFile);
        service.SetFeatureOrder(new[] { "clock", "weather", "todo" });

        var loaded = service.LoadPreferences();
        Assert.Equal("clock", loaded.FeatureOrder[0]);
        Assert.Equal("weather", loaded.FeatureOrder[1]);
        Assert.Equal("todo", loaded.FeatureOrder[2]);
    }

    [Fact]
    public void LoadPreferences_CorruptedFile_RecoversSafelyWithoutThrowing()
    {
        File.WriteAllText(_tempFile, "NOT VALID JSON CONTENT >>> ERROR");

        var service = new SettingsService(_tempFile);
        var prefs = service.LoadPreferences();

        Assert.NotNull(prefs);
        Assert.Equal(11, prefs.EnabledFeatureIds.Count);
    }
}
