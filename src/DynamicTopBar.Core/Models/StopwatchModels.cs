using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Immutable snapshot representing the state of the stopwatch module.
/// </summary>
public record StopwatchSnapshot
{
    public bool IsRunning { get; init; }
    public TimeSpan Elapsed { get; init; } = TimeSpan.Zero;
    public IReadOnlyList<TimeSpan> Laps { get; init; } = Array.Empty<TimeSpan>();

    public string FormattedElapsed
    {
        get
        {
            int minutes = (int)Elapsed.TotalMinutes;
            int seconds = Elapsed.Seconds;
            int tenths = Elapsed.Milliseconds / 100;
            return $"{minutes:D2}:{seconds:D2}.{tenths}";
        }
    }
}
