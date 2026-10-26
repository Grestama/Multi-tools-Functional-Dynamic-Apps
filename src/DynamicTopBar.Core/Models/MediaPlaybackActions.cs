namespace DynamicTopBar.Core.Models;

/// <summary>
/// Supported media control actions reported by the active media session.
/// </summary>
public record MediaPlaybackActions
{
    public bool CanPlay { get; init; }
    public bool CanPause { get; init; }
    public bool CanTogglePlayPause { get; init; }
    public bool CanSkipPrevious { get; init; }
    public bool CanSkipNext { get; init; }

    public static MediaPlaybackActions None { get; } = new();
}
