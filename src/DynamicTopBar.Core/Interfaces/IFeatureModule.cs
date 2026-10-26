using System;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Consistent contract for independently managed feature modules.
/// </summary>
public interface IFeatureModule : IDisposable
{
    /// <summary>
    /// Metadata descriptor describing the feature module.
    /// </summary>
    FeatureDescriptor Descriptor { get; }

    /// <summary>
    /// Whether this module is currently selected and active in the top bar host.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Invoked when the module is navigated to and becomes the active surface.
    /// </summary>
    void Activate();

    /// <summary>
    /// Invoked when the user navigates away from this module.
    /// Navigation away must not destroy background tasks or reset ongoing timers.
    /// </summary>
    void Deactivate();
}
