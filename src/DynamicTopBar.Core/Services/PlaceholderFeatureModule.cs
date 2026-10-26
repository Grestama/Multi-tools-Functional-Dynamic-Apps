using System;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Honest, deliberate placeholder implementation for planned features.
/// Prevents displaying fake controls or fabricated data prior to real service integration.
/// </summary>
public sealed class PlaceholderFeatureModule : FeatureModuleBase
{
    public string Milestone { get; }
    public string PlannedScope { get; }
    public string DependencyNotes { get; }

    public PlaceholderFeatureModule(
        FeatureDescriptor descriptor,
        string milestone,
        string plannedScope,
        string dependencyNotes)
        : base(descriptor with { IsPlaceholder = true })
    {
        Milestone = milestone ?? "Future Milestone";
        PlannedScope = plannedScope ?? string.Empty;
        DependencyNotes = dependencyNotes ?? string.Empty;
    }
}
