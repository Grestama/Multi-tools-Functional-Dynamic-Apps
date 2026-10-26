using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract for direct Spotify Web API OAuth and playback controls.
/// </summary>
public interface ISpotifyService : IDisposable
{
    SpotifyAuthState AuthState { get; }
    SpotifyUserProfile? CurrentUser { get; }
    SpotifyPlaybackSnapshot CurrentPlayback { get; }
    string? StoredClientId { get; }

    event EventHandler<SpotifyAuthState>? AuthStateChanged;
    event EventHandler<SpotifyPlaybackSnapshot>? PlaybackChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> StartPkceAuthAsync(string clientId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<SpotifyPlaybackSnapshot> RefreshPlaybackAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpotifyDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task<bool> TransferPlaybackAsync(string deviceId, bool play = true, CancellationToken cancellationToken = default);
    Task<bool> TogglePlayPauseAsync(CancellationToken cancellationToken = default);
    Task<bool> SkipNextAsync(CancellationToken cancellationToken = default);
    Task<bool> SkipPreviousAsync(CancellationToken cancellationToken = default);
}
