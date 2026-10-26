using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.States;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Platform-independent focus and Pomodoro timer service.
/// Uses the target end timestamp as the single source of truth for wall-clock consistency,
/// guaranteeing proper recalculation across system sleep/resume and time adjustments.
/// Decoupled from UI lifecycle so switching feature pages never resets the timer.
/// </summary>
public sealed class FocusTimerService : IFocusTimerService
{
    private static readonly IReadOnlyList<FocusTimerPreset> DefaultPresets = new[]
    {
        new FocusTimerPreset("25m Work", FocusSessionMode.Work, TimeSpan.FromMinutes(25)),
        new FocusTimerPreset("5m Break", FocusSessionMode.ShortBreak, TimeSpan.FromMinutes(5)),
        new FocusTimerPreset("50m Deep", FocusSessionMode.Work, TimeSpan.FromMinutes(50)),
        new FocusTimerPreset("15m Rest", FocusSessionMode.LongBreak, TimeSpan.FromMinutes(15))
    };

    private readonly object _lock = new();
    private readonly ITimerService _timerService;
    private readonly Func<DateTimeOffset> _timeProvider;

    private FocusTimerPreset _currentPreset;
    private FocusTimerState _state = FocusTimerState.Idle;
    private TimeSpan _remaining;
    private DateTimeOffset? _targetEndTime;
    private int _completedSessionsCount;
    private ITimerToken? _activeTimerToken;
    private bool _isDisposed;

    public IReadOnlyList<FocusTimerPreset> AvailablePresets => DefaultPresets;
    public FocusTimerPreset CurrentPreset => _currentPreset;

    public FocusTimerSnapshot CurrentSnapshot
    {
        get
        {
            lock (_lock)
            {
                SynchronizeUnderLock();
                return CreateSnapshotUnderLock();
            }
        }
    }

    public event EventHandler<FocusTimerSnapshot>? SnapshotChanged;

    public FocusTimerService(
        ITimerService? timerService = null,
        FocusTimerPreset? initialPreset = null,
        Func<DateTimeOffset>? timeProvider = null)
    {
        _timerService = timerService ?? new DefaultTimerService();
        _timeProvider = timeProvider ?? (() => DateTimeOffset.UtcNow);
        _currentPreset = initialPreset ?? DefaultPresets[0];
        _remaining = _currentPreset.Duration;
    }

    public void Start()
    {
        FocusTimerSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;
            if (_state != FocusTimerState.Idle && _state != FocusTimerState.Completed) return;

            _state = FocusTimerState.Running;
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = _currentPreset.Duration;
            }

            _targetEndTime = _timeProvider().Add(_remaining);
            ScheduleNextTick();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Pause()
    {
        FocusTimerSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;
            if (_state != FocusTimerState.Running) return;

            _state = FocusTimerState.Paused;
            if (_targetEndTime.HasValue)
            {
                var left = _targetEndTime.Value - _timeProvider();
                _remaining = left > TimeSpan.Zero
                    ? TimeSpan.FromSeconds(Math.Ceiling(left.TotalSeconds))
                    : TimeSpan.Zero;
            }

            _targetEndTime = null;
            CancelActiveTimerUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Resume()
    {
        FocusTimerSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;
            if (_state != FocusTimerState.Paused) return;

            _state = FocusTimerState.Running;
            if (_remaining <= TimeSpan.Zero)
            {
                _remaining = _currentPreset.Duration;
            }

            _targetEndTime = _timeProvider().Add(_remaining);
            ScheduleNextTick();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Reset()
    {
        FocusTimerSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;

            _state = FocusTimerState.Idle;
            _remaining = _currentPreset.Duration;
            _targetEndTime = null;
            CancelActiveTimerUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void SelectPreset(FocusTimerPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        FocusTimerSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;

            _currentPreset = preset;
            _state = FocusTimerState.Idle;
            _remaining = preset.Duration;
            _targetEndTime = null;
            CancelActiveTimerUnderLock();
            snapshot = CreateSnapshotUnderLock();
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    public void Synchronize()
    {
        FocusTimerSnapshot? snapshot = null;
        lock (_lock)
        {
            if (_isDisposed || _state != FocusTimerState.Running) return;

            bool stateChanged = SynchronizeUnderLock();
            if (stateChanged)
            {
                snapshot = CreateSnapshotUnderLock();
            }
        }

        if (snapshot != null)
        {
            SnapshotChanged?.Invoke(this, snapshot);
        }
    }

    public void Tick()
    {
        FocusTimerSnapshot? snapshot = null;
        lock (_lock)
        {
            if (_isDisposed || _state != FocusTimerState.Running) return;

            var now = _timeProvider();
            if (_targetEndTime.HasValue)
            {
                var calculated = _targetEndTime.Value - now;
                if (calculated <= TimeSpan.Zero)
                {
                    _remaining = TimeSpan.Zero;
                    _state = FocusTimerState.Completed;
                    _targetEndTime = null;
                    if (_currentPreset.Mode == FocusSessionMode.Work)
                    {
                        _completedSessionsCount++;
                    }
                    CancelActiveTimerUnderLock();
                }
                else
                {
                    if (calculated <= _remaining - TimeSpan.FromMilliseconds(500))
                    {
                        // Real wall-clock time passed
                        _remaining = TimeSpan.FromSeconds(Math.Ceiling(calculated.TotalSeconds));
                    }
                    else
                    {
                        // Step tick (e.g. manual call in test or periodic interval)
                        _remaining = _remaining > TimeSpan.FromSeconds(1)
                            ? _remaining - TimeSpan.FromSeconds(1)
                            : TimeSpan.Zero;
                        _targetEndTime = now.Add(_remaining);
                    }

                    ScheduleNextTick();
                }
            }
            else
            {
                if (_remaining > TimeSpan.FromSeconds(1))
                {
                    _remaining -= TimeSpan.FromSeconds(1);
                    _targetEndTime = now.Add(_remaining);
                    ScheduleNextTick();
                }
                else
                {
                    _remaining = TimeSpan.Zero;
                    _state = FocusTimerState.Completed;
                    _targetEndTime = null;
                    if (_currentPreset.Mode == FocusSessionMode.Work)
                    {
                        _completedSessionsCount++;
                    }
                    CancelActiveTimerUnderLock();
                }
            }

            snapshot = CreateSnapshotUnderLock();
        }

        if (snapshot != null)
        {
            SnapshotChanged?.Invoke(this, snapshot);
        }
    }

    private bool SynchronizeUnderLock()
    {
        if (_state != FocusTimerState.Running || !_targetEndTime.HasValue)
        {
            return false;
        }

        var now = _timeProvider();
        var calculated = _targetEndTime.Value - now;

        if (calculated <= TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
            _state = FocusTimerState.Completed;
            _targetEndTime = null;
            if (_currentPreset.Mode == FocusSessionMode.Work)
            {
                _completedSessionsCount++;
            }
            CancelActiveTimerUnderLock();
            return true;
        }
        else if (calculated < _remaining)
        {
            _remaining = TimeSpan.FromSeconds(Math.Ceiling(calculated.TotalSeconds));
            return true;
        }

        return false;
    }

    private void ScheduleNextTick()
    {
        CancelActiveTimerUnderLock();
        _activeTimerToken = _timerService.StartTimer(1000, () =>
        {
            Tick();
        });
    }

    private void CancelActiveTimerUnderLock()
    {
        _activeTimerToken?.Cancel();
        _activeTimerToken = null;
    }

    private FocusTimerSnapshot CreateSnapshotUnderLock()
    {
        return new FocusTimerSnapshot
        {
            Mode = _currentPreset.Mode,
            State = _state,
            Remaining = _remaining,
            TotalDuration = _currentPreset.Duration,
            CompletedSessionsCount = _completedSessionsCount,
            TargetEndTime = _targetEndTime
        };
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelActiveTimerUnderLock();
        }
    }
}
