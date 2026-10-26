using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing loading, saving, and updating user module preferences.
/// </summary>
public interface IFeaturePreferencesService
{
    /// <summary>
    /// Loads current preferences from disk or returns default configuration.
    /// </summary>
    FeaturePreferences Load();

    /// <summary>
    /// Persists preferences safely to disk.
    /// </summary>
    void Save(FeaturePreferences preferences);

    /// <summary>
    /// Enables or disables a feature module by its stable identifier.
    /// </summary>
    void SetFeatureEnabled(string featureId, bool isEnabled);

    /// <summary>
    /// Updates the display sequence of feature modules.
    /// </summary>
    void SetFeatureOrder(IEnumerable<string> orderedIds);

    /// <summary>
    /// Remembers the last active module selected by the user.
    /// </summary>
    void SetLastActiveFeature(string featureId);

    /// <summary>
    /// Raised whenever preferences are updated.
    /// </summary>
    event EventHandler<FeaturePreferences>? PreferencesChanged;
}
