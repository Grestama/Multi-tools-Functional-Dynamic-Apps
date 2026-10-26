using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Execution state of the focus countdown timer.
/// </summary>
public enum FocusTimerState
{
    Idle,
    Running,
    Paused,
    Completed
}

/// <summary>
/// Operational mode for the focus timer session.
/// </summary>
public enum FocusSessionMode
{
    Work,
    ShortBreak,
    LongBreak
}

/// <summary>
/// Predefined timer configuration preset.
/// </summary>
public record FocusTimerPreset(string Name, FocusSessionMode Mode, TimeSpan Duration);

/// <summary>
/// Immutable snapshot of the current focus timer state.
/// </summary>
public record FocusTimerSnapshot
{
    public FocusSessionMode Mode { get; init; } = FocusSessionMode.Work;
    public FocusTimerState State { get; init; } = FocusTimerState.Idle;
    public TimeSpan Remaining { get; init; } = TimeSpan.FromMinutes(25);
    public TimeSpan TotalDuration { get; init; } = TimeSpan.FromMinutes(25);
    public int CompletedSessionsCount { get; init; }
    public DateTimeOffset? TargetEndTime { get; init; }

    public double ProgressPercentage
    {
        get
        {
            if (TotalDuration <= TimeSpan.Zero) return 0.0;
            double elapsed = (TotalDuration - Remaining).TotalSeconds;
            return Math.Clamp((elapsed / TotalDuration.TotalSeconds) * 100.0, 0.0, 100.0);
        }
    }

    public string FormattedRemaining
    {
        get
        {
            int minutes = (int)Remaining.TotalMinutes;
            int seconds = Remaining.Seconds;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    public bool CanStart => State is FocusTimerState.Idle or FocusTimerState.Completed;
    public bool CanPause => State == FocusTimerState.Running;
    public bool CanResume => State == FocusTimerState.Paused;
    public bool CanReset => State != FocusTimerState.Idle;
}
