using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Event arguments containing an updated media snapshot.
/// </summary>
public class MediaSnapshotChangedEventArgs : EventArgs
{
    public MediaSnapshot Snapshot { get; }

    public MediaSnapshotChangedEventArgs(MediaSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}

/// <summary>
/// Platform-independent boundary for querying media playback and invoking media controls.
/// </summary>
public interface IMediaSessionService : IDisposable
{
    /// <summary>
    /// Current snapshot of media playback state.
    /// </summary>
    MediaSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Raised whenever playback status, track metadata, timeline, or session changes.
    /// </summary>
    event EventHandler<MediaSnapshotChangedEventArgs>? SnapshotChanged;

    /// <summary>
    /// Toggles play/pause on the active session if supported.
    /// </summary>
    Task TogglePlayPauseAsync(CancellationToken ct = default);

    /// <summary>
    /// Requests playback to start.
    /// </summary>
    Task PlayAsync(CancellationToken ct = default);

    /// <summary>
    /// Requests playback to pause.
    /// </summary>
    Task PauseAsync(CancellationToken ct = default);

    /// <summary>
    /// Skips to the next track if supported.
    /// </summary>
    Task SkipNextAsync(CancellationToken ct = default);

    /// <summary>
    /// Skips to the previous track if supported.
    /// </summary>
    Task SkipPreviousAsync(CancellationToken ct = default);
}
