using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class PersonalizationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsFile;
    private readonly SettingsService _settingsService;
    private readonly LoggingService _logger;

    public PersonalizationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dtb_personalize_svc_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _settingsFile = Path.Combine(_tempDir, "settings.json");
        _logger = new LoggingService();
        _settingsService = new SettingsService(_settingsFile);
    }

    public void Dispose()
    {
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

    [Fact]
    public async Task InitializeAsync_CreatesDefaultProfilePresets()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);
        await service.InitializeAsync();

        var config = service.CurrentConfiguration;
        Assert.Equal(2, config.SchemaVersion);
        Assert.NotEmpty(config.Profiles);

        var active = service.ActiveProfile;
        Assert.NotNull(active);
        Assert.Contains(config.Profiles, p => p.Id == "profile.default");
        Assert.Contains(config.Profiles, p => p.Id == "profile.deep-work");
        Assert.Contains(config.Profiles, p => p.Id == "profile.media-studio");
    }

    [Fact]
    public async Task SetActiveProfileAsync_SwitchesProfileAndFiresEvent()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);
        await service.InitializeAsync();

        PersonalizationProfile? eventProfile = null;
        service.ActiveProfileChanged += (s, p) => eventProfile = p;

        await service.SetActiveProfileAsync("profile.deep-work");

        Assert.Equal("profile.deep-work", service.ActiveProfile.Id);
        Assert.NotNull(eventProfile);
        Assert.Equal("profile.deep-work", eventProfile.Id);
    }

    [Fact]
    public async Task SaveProfileAsync_AddsNewCustomProfile()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);
        await service.InitializeAsync();

        var customProfile = new PersonalizationProfile
        {
            Id = "profile.custom-work",
            Name = "Work Mode",
            Theme = AppThemePreference.Dark,
            AccentColorHex = "#FF5722",
            EnabledFeatureIds = new List<string> { "todo", "calendar", "notes" }
        };

        await service.SaveProfileAsync(customProfile);
        Assert.Contains(service.CurrentConfiguration.Profiles, p => p.Id == "profile.custom-work");

        await service.SetActiveProfileAsync("profile.custom-work");
        Assert.Equal("#FF5722", service.ActiveProfile.AccentColorHex);
    }

    [Fact]
    public async Task DeleteProfile_RemovesProfile_GuardsLastRemaining()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);
        await service.InitializeAsync();

        // Create a custom profile
        var custom = new PersonalizationProfile
        {
            Id = "to-delete",
            Name = "Temporary Profile"
        };
        await service.SaveProfileAsync(custom);
        Assert.Contains(service.CurrentConfiguration.Profiles, p => p.Id == "to-delete");

        bool deleted = service.DeleteProfile("to-delete");
        Assert.True(deleted);
        Assert.DoesNotContain(service.CurrentConfiguration.Profiles, p => p.Id == "to-delete");

        // Try deleting everything until 1 remains
        var remaining = service.CurrentConfiguration.Profiles.Select(p => p.Id).ToList();
        for (int i = 0; i < remaining.Count - 1; i++)
        {
            service.DeleteProfile(remaining[i]);
        }

        Assert.Single(service.CurrentConfiguration.Profiles);
        string lastId = service.CurrentConfiguration.Profiles[0].Id;

        // Attempting to delete the last profile should return false
        bool deleteLast = service.DeleteProfile(lastId);
        Assert.False(deleteLast);
        Assert.Single(service.CurrentConfiguration.Profiles);
    }

    [Fact]
    public async Task ExportAndImportConfiguration_PreservesState()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);
        await service.InitializeAsync();

        string exportedJson = service.ExportConfiguration();
        Assert.False(string.IsNullOrWhiteSpace(exportedJson));
        Assert.Contains("\"SchemaVersion\": 2", exportedJson);

        using var service2 = new PersonalizationService(_settingsService, _logger, Path.Combine(_tempDir, "sub2"));
        bool imported = service2.ImportConfiguration(exportedJson);

        Assert.True(imported);
        Assert.Equal(service.CurrentConfiguration.Profiles.Count, service2.CurrentConfiguration.Profiles.Count);
    }

    [Fact]
    public void MigrateConfiguration_UpgradesLegacyV1SchemaToV2()
    {
        using var service = new PersonalizationService(_settingsService, _logger, _tempDir);

        string legacyV1Json = @"
        {
            ""EnabledFeatureIds"": [""todo"", ""notes"", ""quicklaunch""],
            ""ClockUse24HourFormat"": true,
            ""AccentColorHex"": ""#00FF88"",
            ""ClipboardOptIn"": true
        }";

        var migrated = service.MigrateConfiguration(legacyV1Json);

        Assert.Equal(2, migrated.SchemaVersion);
        Assert.Equal("profile.default", migrated.ActiveProfileId);
        Assert.True(migrated.BasePreferences.ClockUse24HourFormat);
        Assert.Equal("#00FF88", migrated.BasePreferences.AccentColorHex);
        Assert.True(migrated.BasePreferences.ClipboardOptIn);
        Assert.NotEmpty(migrated.Profiles);
    }
}
