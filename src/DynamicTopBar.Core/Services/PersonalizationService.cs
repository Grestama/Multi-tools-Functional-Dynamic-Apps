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
/// Thread-safe personalization engine managing named user profiles, theme customization,
/// configuration version migration, and atomic export/import.
/// </summary>
public sealed class PersonalizationService : IPersonalizationService
{
    private readonly object _lock = new();
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _logger;
    private readonly string _storageFilePath;

    private VersionedConfiguration _configuration;
    private bool _isDisposed;

    public VersionedConfiguration CurrentConfiguration
    {
        get
        {
            lock (_lock) return _configuration;
        }
    }

    public PersonalizationProfile ActiveProfile
    {
        get
        {
            lock (_lock)
            {
                var profile = _configuration.Profiles.FirstOrDefault(p => string.Equals(p.Id, _configuration.ActiveProfileId, StringComparison.OrdinalIgnoreCase));
                return profile ?? _configuration.Profiles.FirstOrDefault() ?? CreateDefaultProfiles().First();
            }
        }
    }

    public event EventHandler<PersonalizationProfile>? ActiveProfileChanged;

    public PersonalizationService(
        ISettingsService settingsService,
        ILoggingService logger,
        string? baseStorageDirectory = null)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string baseDir = baseStorageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar");

        _storageFilePath = Path.Combine(baseDir, "personalization.json");
        _configuration = new VersionedConfiguration
        {
            Profiles = CreateDefaultProfiles()
        };
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_storageFilePath))
        {
            try
            {
                string rawJson = await File.ReadAllTextAsync(_storageFilePath, cancellationToken);
                var migrated = MigrateConfiguration(rawJson);
                lock (_lock)
                {
                    _configuration = migrated;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load personalization settings. Using defaults.", ex, "PersonalizationService");
            }
        }
        else
        {
            // First run: sync with existing base settings
            var basePrefs = _settingsService.LoadPreferences();
            lock (_lock)
            {
                _configuration = new VersionedConfiguration
                {
                    BasePreferences = basePrefs,
                    Profiles = CreateDefaultProfiles(basePrefs)
                };
            }
            SaveConfiguration();
        }
    }

    public Task SetActiveProfileAsync(string profileId)
    {
        PersonalizationProfile? selected;
        lock (_lock)
        {
            selected = _configuration.Profiles.FirstOrDefault(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase));
            if (selected == null)
            {
                _logger.LogWarning($"Profile '{profileId}' not found.", null, "PersonalizationService");
                return Task.CompletedTask;
            }

            _configuration = _configuration with { ActiveProfileId = selected.Id, LastModified = DateTimeOffset.UtcNow };
        }

        SaveConfiguration();
        _logger.LogInfo($"Active profile switched to: {selected.Name} ({selected.Id})", "PersonalizationService");
        ActiveProfileChanged?.Invoke(this, selected);
        return Task.CompletedTask;
    }

    public Task SaveProfileAsync(PersonalizationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (_lock)
        {
            var profiles = _configuration.Profiles.ToList();
            int idx = profiles.FindIndex(p => string.Equals(p.Id, profile.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                profiles[idx] = profile;
            }
            else
            {
                profiles.Add(profile);
            }

            _configuration = _configuration with { Profiles = profiles, LastModified = DateTimeOffset.UtcNow };
        }

        SaveConfiguration();
        if (string.Equals(ActiveProfile.Id, profile.Id, StringComparison.OrdinalIgnoreCase))
        {
            ActiveProfileChanged?.Invoke(this, profile);
        }

        return Task.CompletedTask;
    }

    public bool DeleteProfile(string profileId)
    {
        bool removed = false;
        PersonalizationProfile? fallback = null;

        lock (_lock)
        {
            // Do not delete last remaining profile
            if (_configuration.Profiles.Count <= 1) return false;

            var profiles = _configuration.Profiles.ToList();
            removed = profiles.RemoveAll(p => string.Equals(p.Id, profileId, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                string activeId = _configuration.ActiveProfileId;
                if (string.Equals(activeId, profileId, StringComparison.OrdinalIgnoreCase))
                {
                    fallback = profiles.First();
                    activeId = fallback.Id;
                }

                _configuration = _configuration with { Profiles = profiles, ActiveProfileId = activeId, LastModified = DateTimeOffset.UtcNow };
            }
        }

        if (removed)
        {
            SaveConfiguration();
            if (fallback != null)
            {
                ActiveProfileChanged?.Invoke(this, fallback);
            }
        }

        return removed;
    }

    public string ExportConfiguration()
    {
        lock (_lock)
        {
            return JsonSerializer.Serialize(_configuration, new JsonSerializerOptions { WriteIndented = true });
        }
    }

    public bool ImportConfiguration(string jsonConfig)
    {
        if (string.IsNullOrWhiteSpace(jsonConfig)) return false;

        try
        {
            var migrated = MigrateConfiguration(jsonConfig);
            lock (_lock)
            {
                _configuration = migrated;
            }

            SaveConfiguration();
            ActiveProfileChanged?.Invoke(this, ActiveProfile);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to import configuration JSON.", ex, "PersonalizationService");
            return false;
        }
    }

    public VersionedConfiguration MigrateConfiguration(string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        int schemaVersion = 1;
        if (root.TryGetProperty("SchemaVersion", out var svProp) && svProp.TryGetInt32(out int sv))
        {
            schemaVersion = sv;
        }

        if (schemaVersion >= VersionedConfiguration.CurrentSchemaVersion)
        {
            var current = JsonSerializer.Deserialize<VersionedConfiguration>(rawJson);
            if (current != null && current.Profiles.Count > 0)
            {
                return current;
            }
        }

        // Schema Migration from v1 to v2:
        _logger.LogInfo($"Migrating legacy configuration schema (v{schemaVersion} -> v{VersionedConfiguration.CurrentSchemaVersion})", "PersonalizationService");

        var basePrefs = FeaturePreferences.Default;
        if (root.TryGetProperty("BasePreferences", out var bpElem))
        {
            var parsed = JsonSerializer.Deserialize<FeaturePreferences>(bpElem.GetRawText());
            if (parsed != null) basePrefs = parsed;
        }
        else if (root.TryGetProperty("EnabledFeatureIds", out _))
        {
            // Root was a flat FeaturePreferences JSON
            var parsed = JsonSerializer.Deserialize<FeaturePreferences>(rawJson);
            if (parsed != null) basePrefs = parsed;
        }

        return new VersionedConfiguration
        {
            SchemaVersion = VersionedConfiguration.CurrentSchemaVersion,
            ActiveProfileId = "profile.default",
            BasePreferences = basePrefs,
            Profiles = CreateDefaultProfiles(basePrefs),
            LastModified = DateTimeOffset.UtcNow
        };
    }

    private static List<PersonalizationProfile> CreateDefaultProfiles(FeaturePreferences? basePrefs = null)
    {
        var prefs = basePrefs ?? FeaturePreferences.Default;

        return new List<PersonalizationProfile>
        {
            new()
            {
                Id = "profile.default",
                Name = "All-in-One Default",
                Description = "Balanced productivity and multimedia top bar glance",
                Theme = AppThemePreference.Dark,
                AccentColorHex = prefs.AccentColorHex,
                SizePreset = PanelSizePreset.Compact,
                ReduceMotion = false,
                EnabledFeatureIds = new List<string>(prefs.EnabledFeatureIds),
                FeatureOrder = new List<string>(prefs.FeatureOrder),
                FavoriteToolIds = new List<string> { "tool-json-format", "tool-base64", "tool-file-search" }
            },
            new()
            {
                Id = "profile.deep-work",
                Name = "Deep Focus",
                Description = "Minimized distraction overlay: Focus Timer, To-Do list, and Scratchpad notes only",
                Theme = AppThemePreference.Dark,
                AccentColorHex = "#FFB900", // Warm amber accent
                SizePreset = PanelSizePreset.Compact,
                ReduceMotion = true,
                EnabledFeatureIds = new List<string> { "focus", "todo", "notes", "clock" },
                FeatureOrder = new List<string> { "focus", "todo", "notes", "clock" },
                FavoriteToolIds = new List<string> { "tool-uuid", "tool-timestamp" }
            },
            new()
            {
                Id = "profile.media-studio",
                Name = "Media Control Studio",
                Description = "Universal media playback controls and Spotify Connect dashboard",
                Theme = AppThemePreference.Dark,
                AccentColorHex = "#1DB954", // Spotify green accent
                SizePreset = PanelSizePreset.Compact,
                ReduceMotion = false,
                EnabledFeatureIds = new List<string> { "media", "spotify", "clock", "system-glance" },
                FeatureOrder = new List<string> { "media", "spotify", "clock", "system-glance" },
                FavoriteToolIds = new List<string>()
            }
        };
    }

    private void SaveConfiguration()
    {
        try
        {
            string json = ExportConfiguration();
            string tmp = $"{_storageFilePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _storageFilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save personalization configuration.", ex, "PersonalizationService");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
