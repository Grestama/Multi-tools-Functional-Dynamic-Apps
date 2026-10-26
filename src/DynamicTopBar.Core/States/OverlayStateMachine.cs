using System;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.States;

/// <summary>
/// Event arguments for overlay state transitions.
/// </summary>
public class OverlayStateChangedEventArgs : EventArgs
{
    public OverlayState PreviousState { get; }
    public OverlayState NewState { get; }

    public OverlayStateChangedEventArgs(OverlayState previousState, OverlayState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}

/// <summary>
/// Manages the explicit UX state transitions (Idle, HoverPending, Expanded, CollapsePending)
/// with predictable hover and collapse delays, cancellation on re-entry, and testable timers.
/// </summary>
public sealed class OverlayStateMachine : IDisposable
{
    private readonly OverlayConfig _config;
    private readonly ITimerService _timerService;
    private readonly object _lock = new();

    private OverlayState _currentState = OverlayState.Idle;
    private ITimerToken? _activeTimer;
    private bool _isDisposed;

    public OverlayState CurrentState
    {
        get
        {
            lock (_lock)
            {
                return _currentState;
            }
        }
    }

    public OverlayConfig Config => _config;

    public event EventHandler<OverlayStateChangedEventArgs>? StateChanged;

    public OverlayStateMachine(OverlayConfig? config = null, ITimerService? timerService = null)
    {
        _config = config ?? new OverlayConfig();
        _timerService = timerService ?? new DefaultTimerService();
    }

    /// <summary>
    /// Invoked when pointer enters the top bar overlay.
    /// </summary>
    public void PointerEntered()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            switch (_currentState)
            {
                case OverlayState.Idle:
                    CancelCurrentTimer();
                    change = SetState(OverlayState.HoverPending);
                    _activeTimer = _timerService.StartTimer(_config.HoverDelayMs, OnHoverTimerElapsed);
                    break;

                case OverlayState.CollapsePending:
                    // Pointer re-entry cancels collapse immediately
                    CancelCurrentTimer();
                    change = SetState(OverlayState.Expanded);
                    break;

                case OverlayState.HoverPending:
                case OverlayState.Expanded:
                    // Already in active hover or expanded; keep current state
                    break;
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    /// <summary>
    /// Invoked when pointer leaves the overlay entirely.
    /// </summary>
    public void PointerExited()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            switch (_currentState)
            {
                case OverlayState.HoverPending:
                    // Leaving before hover delay completes cancels expansion
                    CancelCurrentTimer();
                    change = SetState(OverlayState.Idle);
                    break;

                case OverlayState.Expanded:
                    CancelCurrentTimer();
                    change = SetState(OverlayState.CollapsePending);
                    _activeTimer = _timerService.StartTimer(_config.CollapseDelayMs, OnCollapseTimerElapsed);
                    break;

                case OverlayState.Idle:
                case OverlayState.CollapsePending:
                    break;
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    /// <summary>
    /// Invoked when user actively interacts with controls inside the expanded panel.
    /// Cancels any pending collapse.
    /// </summary>
    public void UserInteracted()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            if (_currentState == OverlayState.CollapsePending)
            {
                CancelCurrentTimer();
                change = SetState(OverlayState.Expanded);
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    /// <summary>
    /// Explicitly forces transition to Expanded state (cancelling timers).
    /// </summary>
    public void ForceExpand()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;
            CancelCurrentTimer();
            if (_currentState != OverlayState.Expanded)
            {
                change = SetState(OverlayState.Expanded);
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    /// <summary>
    /// Explicitly forces transition to Idle state (cancelling timers).
    /// </summary>
    public void ForceIdle()
    {
        DismissExplicit();
    }

    /// <summary>
    /// Explicitly dismisses the overlay back to Idle.
    /// </summary>
    public void DismissExplicit()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            CancelCurrentTimer();
            if (_currentState != OverlayState.Idle)
            {
                change = SetState(OverlayState.Idle);
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    private void OnHoverTimerElapsed()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            if (_currentState == OverlayState.HoverPending)
            {
                CancelCurrentTimer();
                change = SetState(OverlayState.Expanded);
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    private void OnCollapseTimerElapsed()
    {
        OverlayStateChangedEventArgs? change = null;
        lock (_lock)
        {
            if (_isDisposed) return;

            if (_currentState == OverlayState.CollapsePending)
            {
                CancelCurrentTimer();
                change = SetState(OverlayState.Idle);
            }
        }

        if (change != null)
        {
            StateChanged?.Invoke(this, change);
        }
    }

    private OverlayStateChangedEventArgs? SetState(OverlayState newState)
    {
        if (_currentState == newState) return null;

        var previous = _currentState;
        _currentState = newState;

        return new OverlayStateChangedEventArgs(previous, newState);
    }

    private void CancelCurrentTimer()
    {
        _activeTimer?.Cancel();
        _activeTimer?.Dispose();
        _activeTimer = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelCurrentTimer();
        }
    }
}
