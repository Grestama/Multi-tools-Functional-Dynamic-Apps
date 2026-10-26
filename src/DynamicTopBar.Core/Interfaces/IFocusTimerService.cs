using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing focus sessions, Pomodoro countdowns, and presets.
/// </summary>
public interface IFocusTimerService : IDisposable
{
    /// <summary>
    /// Current snapshot of the focus timer.
    /// </summary>
    FocusTimerSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Available presets for quick selection.
    /// </summary>
    IReadOnlyList<FocusTimerPreset> AvailablePresets { get; }

    /// <summary>
    /// Currently selected preset.
    /// </summary>
    FocusTimerPreset CurrentPreset { get; }

    /// <summary>
    /// Raised whenever the timer snapshot changes (tick, state transition, or preset change).
    /// </summary>
    event EventHandler<FocusTimerSnapshot>? SnapshotChanged;

    /// <summary>
    /// Starts the timer from Idle or Completed state.
    /// </summary>
    void Start();

    /// <summary>
    /// Pauses an actively running timer.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes a paused timer.
    /// </summary>
    void Resume();

    /// <summary>
    /// Resets the timer back to Idle with the duration of the current preset.
    /// </summary>
    void Reset();

    /// <summary>
    /// Switches to a specific preset, resetting the timer if needed.
    /// </summary>
    void SelectPreset(FocusTimerPreset preset);

    /// <summary>
    /// Deterministic single-tick step (decrements by 1 second) for testability.
    /// </summary>
    void Tick();

    /// <summary>
    /// Synchronizes the remaining time against the target end time (e.g. after sleep/resume or system time change).
    /// </summary>
    void Synchronize();
}
