using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Settings service implementing ISettingsService and IFeaturePreferencesService,
/// persisting user preferences, enabled modules, and order to local file storage.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly IFeaturePreferencesService _innerService;

    public event EventHandler<FeaturePreferences>? PreferencesChanged
    {
        add => _innerService.PreferencesChanged += value;
        remove => _innerService.PreferencesChanged -= value;
    }

    public SettingsService(string? customFilePath = null)
    {
        _innerService = new LocalFileFeaturePreferencesService(customFilePath);
    }

    public SettingsService(IFeaturePreferencesService innerService)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
    }

    public FeaturePreferences Load() => _innerService.Load();

    public void Save(FeaturePreferences preferences) => _innerService.Save(preferences);

    public FeaturePreferences LoadPreferences() => _innerService.Load();

    public void SavePreferences(FeaturePreferences preferences) => _innerService.Save(preferences);

    public void SetFeatureEnabled(string featureId, bool isEnabled) => _innerService.SetFeatureEnabled(featureId, isEnabled);

    public void SetFeatureOrder(IEnumerable<string> orderedIds) => _innerService.SetFeatureOrder(orderedIds);

    public void SetLastActiveFeature(string featureId) => _innerService.SetLastActiveFeature(featureId);

    public string? GetLastActiveFeatureId() => _innerService.Load().LastActiveFeatureId;
}
