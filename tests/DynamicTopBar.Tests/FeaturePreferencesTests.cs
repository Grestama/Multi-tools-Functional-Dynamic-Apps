using System;
using System.IO;
using System.Linq;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class FeaturePreferencesTests : IDisposable
{
    private readonly string _tempFile;

    public FeaturePreferencesTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dtb_prefs_test_{Guid.NewGuid():N}.json");
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
    public void Load_MissingFile_ReturnsDefaultPreferences()
    {
        var service = new LocalFileFeaturePreferencesService(_tempFile);
        var prefs = service.Load();

        Assert.NotNull(prefs);
        Assert.Contains("media", prefs.EnabledFeatureIds);
        Assert.Contains("focus", prefs.EnabledFeatureIds);
        Assert.Contains("quick-launch", prefs.EnabledFeatureIds);
        Assert.Contains("system-glance", prefs.EnabledFeatureIds);
        Assert.Equal("media", prefs.LastActiveFeatureId);
    }

    [Fact]
    public void Save_And_Load_PersistsSuccessfully()
    {
        var service = new LocalFileFeaturePreferencesService(_tempFile);
        var customPrefs = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "media", "focus" },
            FeatureOrder = new() { "focus", "media" },
            LastActiveFeatureId = "focus"
        };

        service.Save(customPrefs);

        var service2 = new LocalFileFeaturePreferencesService(_tempFile);
        var loaded = service2.Load();

        Assert.Equal(2, loaded.EnabledFeatureIds.Count);
        Assert.Equal("focus", loaded.FeatureOrder[0]);
        Assert.Equal("media", loaded.FeatureOrder[1]);
        Assert.Equal("focus", loaded.LastActiveFeatureId);
    }

    [Fact]
    public void SetFeatureEnabled_UpdatesAndSavesProperly()
    {
        var service = new LocalFileFeaturePreferencesService(_tempFile);

        service.SetFeatureEnabled("media", false);
        var loaded = service.Load();
        Assert.DoesNotContain("media", loaded.EnabledFeatureIds);

        service.SetFeatureEnabled("media", true);
        var loaded2 = service.Load();
        Assert.Contains("media", loaded2.EnabledFeatureIds);
    }

    [Fact]
    public void SetFeatureOrder_ReordersProperly()
    {
        var service = new LocalFileFeaturePreferencesService(_tempFile);
        service.SetFeatureOrder(new[] { "system-glance", "quick-launch", "focus", "media" });

        var loaded = service.Load();
        Assert.Equal("system-glance", loaded.FeatureOrder[0]);
        Assert.Equal("quick-launch", loaded.FeatureOrder[1]);
    }

    [Fact]
    public void Load_CorruptedJson_RecoversSafelyWithoutThrowing()
    {
        File.WriteAllText(_tempFile, "{ this is not valid json content !!! }");

        var service = new LocalFileFeaturePreferencesService(_tempFile);
        var prefs = service.Load();

        Assert.NotNull(prefs);
        Assert.Contains("media", prefs.EnabledFeatureIds);
    }

    [Fact]
    public void FeatureNavigator_ApplyPreferences_FiltersDisabledModulesAndReorders()
    {
        var media = new FeatureDescriptor { Id = "media", DisplayName = "Media", DisplayOrder = 1 };
        var focus = new FeatureDescriptor { Id = "focus", DisplayName = "Focus", DisplayOrder = 2 };
        var quick = new FeatureDescriptor { Id = "quick-launch", DisplayName = "Quick", DisplayOrder = 3 };

        var navigator = new FeatureNavigator(new[] { media, focus, quick });
        Assert.Equal(3, navigator.TotalFeatures);

        var prefs = new FeaturePreferences
        {
            EnabledFeatureIds = new() { "focus", "quick-launch" }, // media disabled
            FeatureOrder = new() { "quick-launch", "focus" },
            LastActiveFeatureId = "quick-launch"
        };

        navigator.ApplyPreferences(prefs);

        Assert.Equal(2, navigator.TotalFeatures);
        Assert.Equal("quick-launch", navigator.CurrentFeature?.Id);
        Assert.Equal(0, navigator.CurrentIndex);

        Assert.True(navigator.NavigateNext());
        Assert.Equal("focus", navigator.CurrentFeature?.Id);
        Assert.False(navigator.CanNavigateNext); // non-wrapping boundary
    }

    [Fact]
    public void FeatureNavigator_ApplyPreferences_AllFeaturesDisabled_HandlesGracefully()
    {
        var media = new FeatureDescriptor { Id = "media", DisplayName = "Media", DisplayOrder = 1 };
        var navigator = new FeatureNavigator(new[] { media });

        var prefs = new FeaturePreferences
        {
            EnabledFeatureIds = new(), // all disabled
            FeatureOrder = new() { "media" },
            LastActiveFeatureId = null
        };

        navigator.ApplyPreferences(prefs);

        Assert.Equal(0, navigator.TotalFeatures);
        Assert.Null(navigator.CurrentFeature);
        Assert.Equal(-1, navigator.CurrentIndex);
        Assert.False(navigator.CanNavigatePrevious);
        Assert.False(navigator.CanNavigateNext);
    }

    [Fact]
    public void Load_SubsequentCalls_ReturnsCachedInstanceWithoutReReadingDisk()
    {
        var service = new LocalFileFeaturePreferencesService(_tempFile);
        var initial = service.Load();

        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }

        var cached = service.Load();
        Assert.Same(initial, cached);
    }
}
