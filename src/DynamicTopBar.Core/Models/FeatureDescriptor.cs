namespace DynamicTopBar.Core.Models;

/// <summary>
/// Descriptor representing a feature module available in the expanded top bar container.
/// </summary>
public record FeatureDescriptor
{
    /// <summary>
    /// Unique stable identifier (e.g. "media", "focus", "quick-launch", "system-glance").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable title for the feature.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Symbolic or icon resource key representing the module.
    /// </summary>
    public string IconKey { get; init; } = string.Empty;

    /// <summary>
    /// Logical sorting order in the navigation sequence.
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Whether this feature is enabled by the user or configuration.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Whether this feature is currently available on the host platform.
    /// </summary>
    public bool IsAvailable { get; init; } = true;

    /// <summary>
    /// Reason explaining why the feature is unavailable if IsAvailable is false.
    /// </summary>
    public string? UnavailableReason { get; init; }

    /// <summary>
    /// Summary description of the feature or its development roadmap status.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Implementation phase tier (e.g. 1, 2, or 3).
    /// </summary>
    public int RoadmapPhase { get; init; } = 1;

    /// <summary>
    /// Whether this module is currently in a deliberate placeholder state pending API integration.
    /// </summary>
    public bool IsPlaceholder { get; init; }
}
