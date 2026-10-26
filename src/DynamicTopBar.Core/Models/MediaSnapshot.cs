namespace DynamicTopBar.Core.Models;

/// <summary>
/// Immutable snapshot of the current system media state.
/// </summary>
public record MediaSnapshot
{
    public bool HasActiveSession { get; init; }
    public string? AppSourceId { get; init; }
    public MediaTrackInfo Track { get; init; } = MediaTrackInfo.Empty;
    public MediaPlaybackStatus Status { get; init; } = MediaPlaybackStatus.Unknown;
    public MediaTimelineInfo Timeline { get; init; } = MediaTimelineInfo.Empty;
    public MediaPlaybackActions Actions { get; init; } = MediaPlaybackActions.None;

    public bool IsPlaying => Status == MediaPlaybackStatus.Playing;

    public static MediaSnapshot Empty { get; } = new()
    {
        HasActiveSession = false,
        Track = MediaTrackInfo.Empty,
        Status = MediaPlaybackStatus.Closed,
        Timeline = MediaTimelineInfo.Empty,
        Actions = MediaPlaybackActions.None
    };
}
