using System;
using System.IO;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class PersonalizationTests : IDisposable
{
    private readonly string _tempFile;

    public PersonalizationTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dtb_personalization_test_{Guid.NewGuid():N}.json");
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
    public void DefaultPreferences_HasExpectedPhase3Defaults()
    {
        var prefs = new FeaturePreferences();

        Assert.False(prefs.ClockUse24HourFormat);
        Assert.Equal(WeatherUnit.Celsius, prefs.WeatherUnit);
        Assert.Equal("#00B4FF", prefs.AccentColorHex);
        Assert.False(prefs.ClipboardOptIn);
        Assert.True(prefs.RemindersEnabled);
        Assert.Null(prefs.PinnedFeatureId);
    }

    [Fact]
    public void SaveAndLoad_PersistsPhase3PersonalizationSettings()
    {
        var service1 = new LocalFileFeaturePreferencesService(_tempFile);
        var custom = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "calendar", "notes", "clipboard" },
            FeatureOrder = new() { "notes", "calendar", "clipboard" },
            LastActiveFeatureId = "notes",
            ClockUse24HourFormat = true,
            WeatherUnit = WeatherUnit.Fahrenheit,
            AccentColorHex = "#107C41",
            ClipboardOptIn = true,
            RemindersEnabled = false,
            PinnedFeatureId = "notes"
        };

        service1.Save(custom);

        var service2 = new LocalFileFeaturePreferencesService(_tempFile);
        var loaded = service2.Load();

        Assert.True(loaded.ClockUse24HourFormat);
        Assert.Equal(WeatherUnit.Fahrenheit, loaded.WeatherUnit);
        Assert.Equal("#107C41", loaded.AccentColorHex);
        Assert.True(loaded.ClipboardOptIn);
        Assert.False(loaded.RemindersEnabled);
        Assert.Equal("notes", loaded.PinnedFeatureId);
        Assert.Equal(3, loaded.EnabledFeatureIds.Count);
        Assert.Equal("notes", loaded.LastActiveFeatureId);
    }
}
