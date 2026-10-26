using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract for persisting enabled modules, module ordering, and user preferences.
/// </summary>
public interface ISettingsService : IFeaturePreferencesService
{
    /// <summary>
    /// Loads user preferences from persistent storage.
    /// </summary>
    FeaturePreferences LoadPreferences();

    /// <summary>
    /// Saves user preferences to persistent storage.
    /// </summary>
    void SavePreferences(FeaturePreferences preferences);

    /// <summary>
    /// Gets the identifier of the last active feature.
    /// </summary>
    string? GetLastActiveFeatureId();
}
