using System;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing high-precision stopwatch, lap times, and countdown controls.
/// </summary>
public interface IStopwatchService : IDisposable
{
    /// <summary>
    /// Current snapshot of stopwatch elapsed time and lap history.
    /// </summary>
    StopwatchSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Raised whenever stopwatch elapsed time or laps change.
    /// </summary>
    event EventHandler<StopwatchSnapshot>? SnapshotChanged;

    /// <summary>
    /// Starts the stopwatch countdown/counter.
    /// </summary>
    void Start();

    /// <summary>
    /// Pauses the stopwatch.
    /// </summary>
    void Stop();

    /// <summary>
    /// Resets the elapsed time and clears laps.
    /// </summary>
    void Reset();

    /// <summary>
    /// Records a lap time at the current elapsed time.
    /// </summary>
    void Lap();

    /// <summary>
    /// Advances elapsed time deterministically for unit tests.
    /// </summary>
    void Advance(TimeSpan delta);
}
