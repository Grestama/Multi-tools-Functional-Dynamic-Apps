using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Event arguments raised when the active feature selection changes.
/// </summary>
public class FeatureNavigationChangedEventArgs : EventArgs
{
    public FeatureDescriptor? PreviousFeature { get; }
    public FeatureDescriptor? CurrentFeature { get; }
    public int CurrentIndex { get; }
    public int TotalFeatures { get; }

    public FeatureNavigationChangedEventArgs(
        FeatureDescriptor? previousFeature,
        FeatureDescriptor? currentFeature,
        int currentIndex,
        int totalFeatures)
    {
        PreviousFeature = previousFeature;
        CurrentFeature = currentFeature;
        CurrentIndex = currentIndex;
        TotalFeatures = totalFeatures;
    }
}

/// <summary>
/// Manages registration, ordering, and navigation between feature modules in the top bar container.
/// </summary>
public interface IFeatureNavigator
{
    /// <summary>
    /// Currently selected feature module. Null if no enabled features are registered.
    /// </summary>
    FeatureDescriptor? CurrentFeature { get; }

    /// <summary>
    /// Zero-based index of the currently active feature among enabled features (-1 if none).
    /// </summary>
    int CurrentIndex { get; }

    /// <summary>
    /// Total count of enabled features available for navigation.
    /// </summary>
    int TotalFeatures { get; }

    /// <summary>
    /// True if there is a preceding enabled feature (no wrap-around).
    /// </summary>
    bool CanNavigatePrevious { get; }

    /// <summary>
    /// True if there is a succeeding enabled feature (no wrap-around).
    /// </summary>
    bool CanNavigateNext { get; }

    /// <summary>
    /// List of all registered enabled features in display order.
    /// </summary>
    IReadOnlyList<FeatureDescriptor> EnabledFeatures { get; }

    /// <summary>
    /// Raised whenever the selected feature changes.
    /// </summary>
    event EventHandler<FeatureNavigationChangedEventArgs>? CurrentFeatureChanged;

    /// <summary>
    /// Navigates to the previous feature in sequence. Returns true if navigation succeeded.
    /// </summary>
    bool NavigatePrevious();

    /// <summary>
    /// Navigates to the next feature in sequence. Returns true if navigation succeeded.
    /// </summary>
    bool NavigateNext();

    /// <summary>
    /// Navigates directly to a feature by its unique identifier. Returns true if found and selected.
    /// </summary>
    bool NavigateTo(string featureId);

    /// <summary>
    /// Registers or updates a feature descriptor in the catalog.
    /// </summary>
    void RegisterFeature(FeatureDescriptor feature);

    /// <summary>
    /// Removes a feature from the catalog by its identifier.
    /// </summary>
    bool UnregisterFeature(string featureId);

    /// <summary>
    /// Applies user preferences (enabled status, sequence order, and preferred active ID).
    /// </summary>
    void ApplyPreferences(FeaturePreferences preferences);
}
