using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.States;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Platform-independent stopwatch service providing high-precision elapsed timing and lap recording.
/// </summary>
public sealed class StopwatchService : IStopwatchService
{
    private readonly object _lock = new();
    private readonly ITimerService _timerService;
    private readonly List<TimeSpan> _laps = new();

    private bool _isRunning;
    private TimeSpan _accumulated = TimeSpan.Zero;
    private DateTime? _startedAtUtc;
    private ITimerToken? _activeTimerToken;
    private bool _isDisposed;

    public event EventHandler<StopwatchSnapshot>? SnapshotChanged;

    public StopwatchSnapshot CurrentSnapshot
    {
        get
        {
            lock (_lock)
            {
                return CreateSnapshotUnderLock();
            }
        }
    }

    public StopwatchService(ITimerService? timerService = null)
    {
        _timerService = timerService ?? new DefaultTimerService();
    }

    public void Start()
    {
        StopwatchSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed || _isRunning) return;

            _isRunning = true;
            _startedAtUtc = DateTime.UtcNow;
            ScheduleNextTickUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Stop()
    {
        StopwatchSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed || !_isRunning) return;

            if (_startedAtUtc.HasValue)
            {
                _accumulated += (DateTime.UtcNow - _startedAtUtc.Value);
                _startedAtUtc = null;
            }

            _isRunning = false;
            CancelTimerUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Reset()
    {
        StopwatchSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;

            _isRunning = false;
            _accumulated = TimeSpan.Zero;
            _startedAtUtc = null;
            _laps.Clear();
            CancelTimerUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Lap()
    {
        StopwatchSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;

            var currentElapsed = CalculateElapsedUnderLock();
            if (currentElapsed > TimeSpan.Zero)
            {
                _laps.Add(currentElapsed);
            }

            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Advance(TimeSpan delta)
    {
        StopwatchSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;

            _accumulated += delta;
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    private void ScheduleNextTickUnderLock()
    {
        CancelTimerUnderLock();
        _activeTimerToken = _timerService.StartTimer(100, () =>
        {
            StopwatchSnapshot? tickSnapshot = null;
            lock (_lock)
            {
                if (_isDisposed || !_isRunning) return;
                ScheduleNextTickUnderLock();
                tickSnapshot = CreateSnapshotUnderLock();
            }

            if (tickSnapshot != null)
            {
                SnapshotChanged?.Invoke(this, tickSnapshot);
            }
        });
    }

    private void CancelTimerUnderLock()
    {
        _activeTimerToken?.Cancel();
        _activeTimerToken = null;
    }

    private TimeSpan CalculateElapsedUnderLock()
    {
        var total = _accumulated;
        if (_isRunning && _startedAtUtc.HasValue)
        {
            total += (DateTime.UtcNow - _startedAtUtc.Value);
        }
        return total;
    }

    private StopwatchSnapshot CreateSnapshotUnderLock()
    {
        return new StopwatchSnapshot
        {
            IsRunning = _isRunning,
            Elapsed = CalculateElapsedUnderLock(),
            Laps = _laps.ToList()
        };
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelTimerUnderLock();
        }
    }
}
