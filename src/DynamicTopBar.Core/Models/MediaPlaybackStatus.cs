namespace DynamicTopBar.Core.Models;

/// <summary>
/// Neutral representation of playback status.
/// </summary>
public enum MediaPlaybackStatus
{
    Unknown,
    Closed,
    Opened,
    Changing,
    Stopped,
    Playing,
    Paused
}
