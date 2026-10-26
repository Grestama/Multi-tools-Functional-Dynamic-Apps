using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Neutral timeline values for the current media item.
/// </summary>
public record MediaTimelineInfo
{
    public TimeSpan? Position { get; init; }
    public TimeSpan? Duration { get; init; }
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }

    public static MediaTimelineInfo Empty { get; } = new();
}
