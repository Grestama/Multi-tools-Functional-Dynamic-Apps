namespace DynamicTopBar.Core.States;

/// <summary>
/// Defines the explicit UX states for the DynamicTopBar overlay.
/// </summary>
public enum OverlayState
{
    /// <summary>
    /// Compact idle capsule centered at the top of the display.
    /// </summary>
    Idle,

    /// <summary>
    /// Pointer has entered the capsule; waiting for hover delay before expanding.
    /// </summary>
    HoverPending,

    /// <summary>
    /// Contextual panel expanded downward from the capsule.
    /// </summary>
    Expanded,

    /// <summary>
    /// Pointer has left the overlay; waiting for collapse delay before returning to idle.
    /// </summary>
    CollapsePending
}
