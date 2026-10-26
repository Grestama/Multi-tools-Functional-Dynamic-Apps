using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Status of the direct Spotify Web API authentication.
/// </summary>
public enum SpotifyAuthState
{
    Disconnected,
    Connecting,
    Authenticated,
    TokenExpired,
    Error
}

/// <summary>
/// Spotify OAuth PKCE token information.
/// </summary>
public record SpotifyToken
{
    public required string AccessToken { get; init; }
    public string? TokenType { get; init; } = "Bearer";
    public string? RefreshToken { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? Scope { get; init; }
    public string? ClientId { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt.AddSeconds(-30);
}

/// <summary>
/// User profile returned by Spotify Web API /me.
/// </summary>
public record SpotifyUserProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Product { get; init; } // "premium", "free", "open"

    public bool IsPremium => string.Equals(Product, "premium", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Spotify track details retrieved from Web API.
/// </summary>
public record SpotifyTrack
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Artist { get; init; }
    public required string Album { get; init; }
    public string? ArtworkUrl { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsPlaying { get; init; }
}

/// <summary>
/// A Spotify Connect player device returned by /v1/me/player/devices.
/// </summary>
public record SpotifyDevice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool IsActive { get; init; }
    public bool IsRestricted { get; init; }
    public int? VolumePercent { get; init; }
}

/// <summary>
/// Snapshot of Spotify playback state via Web API.
/// </summary>
public record SpotifyPlaybackSnapshot
{
    public bool IsActive { get; init; }
    public bool IsPlaying { get; init; }
    public string? DeviceName { get; init; }
    public SpotifyTrack? Track { get; init; }
    public TimeSpan Position { get; init; }
    public bool CanPlayPause { get; init; }
    public bool CanSkipNext { get; init; }
    public bool CanSkipPrevious { get; init; }
    public bool RequiresPremiumNotice { get; init; }
    public string? StatusMessage { get; init; }
    public IReadOnlyList<SpotifyDevice> AvailableDevices { get; init; } = Array.Empty<SpotifyDevice>();

    public static SpotifyPlaybackSnapshot Empty(string? message = null) => new()
    {
        IsActive = false,
        IsPlaying = false,
        StatusMessage = message ?? "No active Spotify playback",
        AvailableDevices = Array.Empty<SpotifyDevice>()
    };
}
