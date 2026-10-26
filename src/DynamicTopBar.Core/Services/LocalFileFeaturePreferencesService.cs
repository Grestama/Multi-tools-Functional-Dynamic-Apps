using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe local file implementation of IFeaturePreferencesService.
/// Persists stable feature IDs to local JSON storage with safe fallback on corruption.
/// </summary>
public sealed class LocalFileFeaturePreferencesService : IFeaturePreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _lock = new();
    private readonly string _filePath;
    private FeaturePreferences _currentPreferences;

    public event EventHandler<FeaturePreferences>? PreferencesChanged;

    public LocalFileFeaturePreferencesService(string? customFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _filePath = customFilePath;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(appData, "DynamicTopBar", "preferences.json");
        }

        _currentPreferences = Load();
    }

    public FeaturePreferences Load()
    {
        lock (_lock)
        {
            return _currentPreferences ??= LoadFromDisk();
        }
    }

    public FeaturePreferences ReloadFromDisk()
    {
        lock (_lock)
        {
            return LoadFromDisk();
        }
    }

    private FeaturePreferences LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _currentPreferences = FeaturePreferences.Default;
                return _currentPreferences;
            }

            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _currentPreferences = FeaturePreferences.Default;
                return _currentPreferences;
            }

            var loaded = JsonSerializer.Deserialize<FeaturePreferences>(json, JsonOptions);
            _currentPreferences = loaded ?? FeaturePreferences.Default;
            return _currentPreferences;
        }
        catch (Exception)
        {
            // Gracefully fallback to defaults if file is corrupted, locked, or unreadable
            _currentPreferences = FeaturePreferences.Default;
            return _currentPreferences;
        }
    }

    public void Save(FeaturePreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        lock (_lock)
        {
            _currentPreferences = preferences;

            try
            {
                string? directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(preferences, JsonOptions);
                string tempPath = $"{_filePath}.tmp_{Guid.NewGuid():N}";
                try
                {
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, _filePath, overwrite: true);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore disk write errors to prevent crashing the overlay
            }
        }

        PreferencesChanged?.Invoke(this, preferences);
    }

    public void SetFeatureEnabled(string featureId, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return;

        FeaturePreferences updated;
        lock (_lock)
        {
            var enabledSet = new HashSet<string>(_currentPreferences.EnabledFeatureIds, StringComparer.OrdinalIgnoreCase);
            if (isEnabled)
            {
                enabledSet.Add(featureId);
            }
            else
            {
                enabledSet.Remove(featureId);
            }

            updated = _currentPreferences with
            {
                EnabledFeatureIds = enabledSet.ToList()
            };
        }

        Save(updated);
    }

    public void SetFeatureOrder(IEnumerable<string> orderedIds)
    {
        ArgumentNullException.ThrowIfNull(orderedIds);

        FeaturePreferences updated;
        lock (_lock)
        {
            updated = _currentPreferences with
            {
                FeatureOrder = orderedIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        Save(updated);
    }

    public void SetLastActiveFeature(string featureId)
    {
        if (string.IsNullOrWhiteSpace(featureId)) return;

        FeaturePreferences updated;
        lock (_lock)
        {
            updated = _currentPreferences with
            {
                LastActiveFeatureId = featureId
            };
        }

        Save(updated);
    }
}
