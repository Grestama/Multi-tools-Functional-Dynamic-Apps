namespace DynamicTopBar.Core.Models;

/// <summary>
/// Baseline configuration values and design tokens for the overlay interaction.
/// </summary>
public record OverlayConfig
{
    /// <summary>
    /// Target idle capsule height in effective UI units (DIPs).
    /// </summary>
    public double IdleHeight { get; init; } = 44.0;

    /// <summary>
    /// Idle capsule width in effective UI units (DIPs) to accommodate the app chip and system status.
    /// </summary>
    public double IdleWidth { get; init; } = 340.0;

    /// <summary>
    /// Expanded panel target height in effective UI units (DIPs).
    /// </summary>
    public double ExpandedHeight { get; init; } = 224.0;

    /// <summary>
    /// Expanded panel target width in effective UI units (DIPs).
    /// </summary>
    public double ExpandedWidth { get; init; } = 376.0;

    /// <summary>
    /// Pill corner radius for the capsule in effective UI units.
    /// </summary>
    public double CapsuleCornerRadius { get; init; } = 22.0;

    /// <summary>
    /// Width of the minimal trigger handle bar (garis button) when concealed at top edge.
    /// </summary>
    public double TriggerBarWidth { get; init; } = 54.0;

    /// <summary>
    /// Height of the minimal trigger handle bar (garis button) when concealed at top edge.
    /// </summary>
    public double TriggerBarHeight { get; init; } = 4.0;

    /// <summary>
    /// Hover-to-expand delay in milliseconds.
    /// </summary>
    public int HoverDelayMs { get; init; } = 500;

    /// <summary>
    /// Collapse delay after pointer leaves in milliseconds.
    /// </summary>
    public int CollapseDelayMs { get; init; } = 4000;
}
