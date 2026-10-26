using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Concrete Windows platform adapter implementing IMediaSessionService using
/// GlobalSystemMediaTransportControlsSessionManager (GSMTC).
/// </summary>
public sealed class WindowsMediaSessionService : IMediaSessionService
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private volatile MediaSnapshot _currentSnapshot = MediaSnapshot.Empty;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile bool _isDisposed;

    public MediaSnapshot CurrentSnapshot => _currentSnapshot;

    public event EventHandler<MediaSnapshotChangedEventArgs>? SnapshotChanged;

    public WindowsMediaSessionService()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_isDisposed) return;

            _sessionManager = manager;
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                await BindCurrentSessionAsync(_sessionManager.GetCurrentSession());
            }
        }
        catch (Exception)
        {
            if (!_isDisposed)
            {
                UpdateSnapshot(MediaSnapshot.Empty);
            }
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        if (_isDisposed) return;

        try
        {
            var session = sender.GetCurrentSession();
            await BindCurrentSessionAsync(session);
        }
        catch (Exception)
        {
            if (!_isDisposed)
            {
                UpdateSnapshot(MediaSnapshot.Empty);
            }
        }
    }

    private async Task BindCurrentSessionAsync(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_isDisposed) return;

        try
        {
            await _lock.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            if (_isDisposed) return;

            if (_currentSession != null)
            {
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                _currentSession = null;
            }

            _currentSession = session;

            if (_currentSession != null)
            {
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

                await RefreshSnapshotAsync(_currentSession);
            }
            else
            {
                UpdateSnapshot(MediaSnapshot.Empty);
            }
        }
        finally
        {
            try
            {
                _lock.Release();
            }
            catch (ObjectDisposedException) { }
        }
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        if (_isDisposed) return;
        await RefreshSnapshotAsync(sender);
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        if (_isDisposed) return;
        await RefreshSnapshotAsync(sender);
    }

    private async void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        if (_isDisposed) return;
        await RefreshSnapshotAsync(sender);
    }

    private async Task RefreshSnapshotAsync(GlobalSystemMediaTransportControlsSession session)
    {
        if (_isDisposed) return;

        try
        {
            var playbackInfo = session.GetPlaybackInfo();
            var timelineProps = session.GetTimelineProperties();
            var mediaProperties = await session.TryGetMediaPropertiesAsync();

            if (_isDisposed) return;

            var status = MapPlaybackStatus(playbackInfo?.PlaybackStatus);
            var actions = MapPlaybackActions(playbackInfo?.Controls);
            var timeline = MapTimeline(timelineProps);
            var track = await MapTrackAsync(mediaProperties);

            if (_isDisposed) return;

            var snapshot = new MediaSnapshot
            {
                HasActiveSession = true,
                AppSourceId = session.SourceAppUserModelId,
                Status = status,
                Actions = actions,
                Timeline = timeline,
                Track = track
            };

            UpdateSnapshot(snapshot);
        }
        catch (Exception)
        {
            if (!_isDisposed)
            {
                UpdateSnapshot(MediaSnapshot.Empty);
            }
        }
    }

    private static MediaPlaybackStatus MapPlaybackStatus(GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed => MediaPlaybackStatus.Closed,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Opened => MediaPlaybackStatus.Opened,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Changing => MediaPlaybackStatus.Changing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped => MediaPlaybackStatus.Stopped,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => MediaPlaybackStatus.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => MediaPlaybackStatus.Paused,
            _ => MediaPlaybackStatus.Unknown
        };
    }

    private static MediaPlaybackActions MapPlaybackActions(GlobalSystemMediaTransportControlsSessionPlaybackControls? controls)
    {
        if (controls == null)
        {
            return MediaPlaybackActions.None;
        }

        return new MediaPlaybackActions
        {
            CanPlay = controls.IsPlayEnabled,
            CanPause = controls.IsPauseEnabled,
            CanTogglePlayPause = controls.IsPlayPauseToggleEnabled || controls.IsPlayEnabled || controls.IsPauseEnabled,
            CanSkipPrevious = controls.IsPreviousEnabled,
            CanSkipNext = controls.IsNextEnabled
        };
    }

    private static MediaTimelineInfo MapTimeline(GlobalSystemMediaTransportControlsSessionTimelineProperties? props)
    {
        if (props == null)
        {
            return MediaTimelineInfo.Empty;
        }

        return new MediaTimelineInfo
        {
            Position = props.Position,
            Duration = props.EndTime > props.StartTime ? props.EndTime - props.StartTime : null,
            StartTime = props.StartTime,
            EndTime = props.EndTime
        };
    }

    private static async Task<MediaTrackInfo> MapTrackAsync(GlobalSystemMediaTransportControlsSessionMediaProperties? props)
    {
        if (props == null)
        {
            return MediaTrackInfo.Empty;
        }

        byte[]? artworkBytes = null;
        string? artworkMimeType = null;

        if (props.Thumbnail != null)
        {
            try
            {
                using var stream = await props.Thumbnail.OpenReadAsync();
                artworkMimeType = stream.ContentType;
                using var netStream = stream.AsStreamForRead();
                using var ms = new MemoryStream();
                await netStream.CopyToAsync(ms);
                artworkBytes = ms.ToArray();
            }
            catch (Exception)
            {
                // Thumbnail stream read failure: fallback to null artwork
                artworkBytes = null;
            }
        }

        return new MediaTrackInfo
        {
            Title = props.Title,
            Artist = props.Artist,
            AlbumTitle = props.AlbumTitle,
            AlbumArtist = props.AlbumArtist,
            ArtworkBytes = artworkBytes,
            ArtworkMimeType = artworkMimeType
        };
    }

    private void UpdateSnapshot(MediaSnapshot snapshot)
    {
        if (_isDisposed) return;
        _currentSnapshot = snapshot;
        SnapshotChanged?.Invoke(this, new MediaSnapshotChangedEventArgs(snapshot));
    }

    public async Task TogglePlayPauseAsync(CancellationToken ct = default)
    {
        if (_isDisposed) return;

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
            catch (Exception) { }
        }
    }

    public async Task PlayAsync(CancellationToken ct = default)
    {
        if (_isDisposed) return;

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryPlayAsync();
            }
            catch (Exception) { }
        }
    }

    public async Task PauseAsync(CancellationToken ct = default)
    {
        if (_isDisposed) return;

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryPauseAsync();
            }
            catch (Exception) { }
        }
    }

    public async Task SkipNextAsync(CancellationToken ct = default)
    {
        if (_isDisposed) return;

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
            }
            catch (Exception) { }
        }
    }

    public async Task SkipPreviousAsync(CancellationToken ct = default)
    {
        if (_isDisposed) return;

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
            }
            catch (Exception) { }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_currentSession != null)
        {
            try
            {
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
            catch { }
            _currentSession = null;
        }

        if (_sessionManager != null)
        {
            try
            {
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
            }
            catch { }
            _sessionManager = null;
        }

        _lock.Dispose();
    }
}
