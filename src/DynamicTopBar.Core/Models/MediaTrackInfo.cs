namespace DynamicTopBar.Core.Models;

/// <summary>
/// Neutral metadata describing the currently active media track.
/// </summary>
public record MediaTrackInfo
{
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? AlbumTitle { get; init; }
    public string? AlbumArtist { get; init; }
    public byte[]? ArtworkBytes { get; init; }
    public string? ArtworkMimeType { get; init; }

    public static MediaTrackInfo Empty { get; } = new();
}
