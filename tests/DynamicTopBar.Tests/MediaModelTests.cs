using System;
using DynamicTopBar.Core.Models;
using Xunit;

namespace DynamicTopBar.Tests;

public class MediaModelTests
{
    [Fact]
    public void EmptyMediaSnapshot_HasExpectedDefaults()
    {
        var snapshot = MediaSnapshot.Empty;

        Assert.False(snapshot.HasActiveSession);
        Assert.False(snapshot.IsPlaying);
        Assert.Equal(MediaPlaybackStatus.Closed, snapshot.Status);
        Assert.Null(snapshot.Track.Title);
        Assert.Null(snapshot.Track.Artist);
        Assert.Null(snapshot.Track.ArtworkBytes);
        Assert.False(snapshot.Actions.CanPlay);
        Assert.False(snapshot.Actions.CanPause);
        Assert.False(snapshot.Actions.CanSkipNext);
        Assert.False(snapshot.Actions.CanSkipPrevious);
        Assert.Null(snapshot.Timeline.Duration);
        Assert.Null(snapshot.Timeline.Position);
    }

    [Fact]
    public void MediaSnapshot_ReflectsPlayingStateCorrectly()
    {
        var snapshot = new MediaSnapshot
        {
            HasActiveSession = true,
            Status = MediaPlaybackStatus.Playing,
            Track = new MediaTrackInfo { Title = "Song", Artist = "Artist" },
            Actions = new MediaPlaybackActions { CanPause = true, CanSkipNext = true }
        };

        Assert.True(snapshot.HasActiveSession);
        Assert.True(snapshot.IsPlaying);
        Assert.True(snapshot.Actions.CanPause);
        Assert.True(snapshot.Actions.CanSkipNext);
        Assert.False(snapshot.Actions.CanSkipPrevious);
    }

    [Fact]
    public void MediaSnapshot_PausedState_IsNotPlaying()
    {
        var snapshot = new MediaSnapshot
        {
            HasActiveSession = true,
            Status = MediaPlaybackStatus.Paused,
            Track = new MediaTrackInfo { Title = "Song", Artist = "Artist" }
        };

        Assert.False(snapshot.IsPlaying);
    }

    [Fact]
    public void MediaTimelineInfo_ValidDurationAndPosition_CalculatesCorrectly()
    {
        var timeline = new MediaTimelineInfo
        {
            Position = TimeSpan.FromSeconds(81), // 1:21
            Duration = TimeSpan.FromSeconds(207), // 3:27
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromSeconds(207)
        };

        Assert.NotNull(timeline.Duration);
        Assert.NotNull(timeline.Position);
        Assert.Equal(81, timeline.Position.Value.TotalSeconds);
        Assert.Equal(207, timeline.Duration.Value.TotalSeconds);

        var remaining = timeline.Duration.Value - timeline.Position.Value;
        Assert.Equal(126, remaining.TotalSeconds); // 2:06
    }

    [Fact]
    public void MediaTrackInfo_ArtworkBytes_RetainsIntegrity()
    {
        byte[] fakeArtwork = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var track = new MediaTrackInfo
        {
            Title = "Test Track",
            Artist = "Test Artist",
            ArtworkBytes = fakeArtwork,
            ArtworkMimeType = "image/png"
        };

        Assert.NotNull(track.ArtworkBytes);
        Assert.Equal(8, track.ArtworkBytes.Length);
        Assert.Equal(fakeArtwork, track.ArtworkBytes);
        Assert.Equal("image/png", track.ArtworkMimeType);
    }

    [Fact]
    public void MediaTrackInfo_ExtremelyLongTitleAndArtist_DoesNotThrow()
    {
        string extremelyLongTitle = new string('A', 5000);
        string extremelyLongArtist = new string('B', 5000);

        var track = new MediaTrackInfo
        {
            Title = extremelyLongTitle,
            Artist = extremelyLongArtist
        };

        Assert.Equal(5000, track.Title.Length);
        Assert.Equal(5000, track.Artist.Length);
    }
}
