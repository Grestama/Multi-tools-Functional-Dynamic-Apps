using System;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// State manager preserving independent feature state while navigating between modules.
/// </summary>
public interface IFeatureStateManager
{
    /// <summary>
    /// Event fired when a feature's saved state changes.
    /// </summary>
    event EventHandler<string>? StateChanged;

    /// <summary>
    /// Stores or updates the state object associated with a feature ID.
    /// </summary>
    void SetState<T>(string featureId, T state);

    /// <summary>
    /// Retrieves the state object associated with a feature ID.
    /// Returns default if no state exists or if the state is not of type T.
    /// </summary>
    T? GetState<T>(string featureId);

    /// <summary>
    /// Attempts to retrieve the state object associated with a feature ID.
    /// </summary>
    bool TryGetState<T>(string featureId, out T? state);

    /// <summary>
    /// Checks whether an active state exists for the feature ID.
    /// </summary>
    bool HasState(string featureId);

    /// <summary>
    /// Removes the state associated with the feature ID.
    /// </summary>
    bool RemoveState(string featureId);

    /// <summary>
    /// Clears all stored feature states.
    /// </summary>
    void ClearAll();
}
