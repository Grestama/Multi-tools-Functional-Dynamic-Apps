using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.States;
using Xunit;

namespace DynamicTopBar.Tests;

public class TestTimerService : ITimerService
{
    public class TestTimerToken : ITimerToken
    {
        public int DelayMs { get; }
        public Action Callback { get; }
        public bool IsActive { get; private set; } = true;
        public bool WasCancelled { get; private set; }

        public TestTimerToken(int delayMs, Action callback)
        {
            DelayMs = delayMs;
            Callback = callback;
        }

        public void Trigger()
        {
            if (IsActive)
            {
                IsActive = false;
                Callback();
            }
        }

        public void Cancel()
        {
            IsActive = false;
            WasCancelled = true;
        }

        public void Dispose() => Cancel();
    }

    public List<TestTimerToken> Timers { get; } = new();

    public ITimerToken StartTimer(int delayMs, Action callback)
    {
        var token = new TestTimerToken(delayMs, callback);
        Timers.Add(token);
        return token;
    }

    public TestTimerToken? LastTimer => Timers.Count > 0 ? Timers[^1] : null;
}

public class OverlayStateMachineTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        Assert.Equal(OverlayState.Idle, sm.CurrentState);
    }

    [Fact]
    public void PointerEntered_FromIdle_TransitionsToHoverPending_AndStarts500msTimer()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();

        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);
        Assert.NotNull(timer.LastTimer);
        Assert.Equal(500, timer.LastTimer.DelayMs);
        Assert.True(timer.LastTimer.IsActive);
    }

    [Fact]
    public void PointerExited_FromHoverPending_CancelsTimer_AndReturnsToIdle()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);

        sm.PointerExited();
        Assert.Equal(OverlayState.Idle, sm.CurrentState);
        Assert.True(timer.LastTimer?.WasCancelled);
    }

    [Fact]
    public void HoverTimerElapsed_FromHoverPending_TransitionsToExpanded()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        timer.LastTimer?.Trigger();

        Assert.Equal(OverlayState.Expanded, sm.CurrentState);
    }

    [Fact]
    public void PointerExited_FromExpanded_TransitionsToCollapsePending_AndStarts4000msTimer()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        // Expand first
        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);

        // Leave overlay
        sm.PointerExited();
        Assert.Equal(OverlayState.CollapsePending, sm.CurrentState);
        Assert.NotNull(timer.LastTimer);
        Assert.Equal(4000, timer.LastTimer.DelayMs);
        Assert.True(timer.LastTimer.IsActive);
    }

    [Fact]
    public void PointerEntered_FromCollapsePending_CancelsTimer_AndReturnsToExpanded()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        // Expand and leave
        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        sm.PointerExited();
        Assert.Equal(OverlayState.CollapsePending, sm.CurrentState);

        // Re-enter pointer before 4s completes
        sm.PointerEntered();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);
        Assert.True(timer.LastTimer?.WasCancelled);
    }

    [Fact]
    public void CollapseTimerElapsed_FromCollapsePending_TransitionsToIdle()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        // Expand, leave, and let collapse timer complete
        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        sm.PointerExited();
        timer.LastTimer?.Trigger();

        Assert.Equal(OverlayState.Idle, sm.CurrentState);
    }

    [Fact]
    public void UserInteracted_FromCollapsePending_CancelsTimer_AndReturnsToExpanded()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        sm.PointerExited();
        Assert.Equal(OverlayState.CollapsePending, sm.CurrentState);

        sm.UserInteracted();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);
        Assert.True(timer.LastTimer?.WasCancelled);
    }

    [Fact]
    public void DismissExplicit_FromExpanded_ReturnsToIdle()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);

        sm.DismissExplicit();
        Assert.Equal(OverlayState.Idle, sm.CurrentState);
    }

    [Fact]
    public void DismissExplicit_FromHoverPending_CancelsTimer_AndReturnsToIdle()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);

        sm.DismissExplicit();
        Assert.Equal(OverlayState.Idle, sm.CurrentState);
        Assert.True(timer.LastTimer?.WasCancelled);
    }

    [Fact]
    public void DoublePointerEntered_InHoverPending_IsIdempotent()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        int timerCountAfterFirst = timer.Timers.Count;

        sm.PointerEntered();
        Assert.Equal(timerCountAfterFirst, timer.Timers.Count);
        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);
    }

    [Fact]
    public void PointerEntered_WhenAlreadyExpanded_IsIdempotent()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        timer.LastTimer?.Trigger();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);

        sm.PointerEntered();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);
    }

    [Fact]
    public void PointerExited_WhenIdle_DoesNothing()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        sm.PointerExited();
        Assert.Equal(OverlayState.Idle, sm.CurrentState);
        Assert.Empty(timer.Timers);
    }

    [Fact]
    public void DisposedStateMachine_CancelsTimer_AndIgnoresEvents()
    {
        var timer = new TestTimerService();
        var sm = new OverlayStateMachine(null, timer);

        sm.PointerEntered();
        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);
        var activeTimer = timer.LastTimer;

        sm.Dispose();

        Assert.True(activeTimer?.WasCancelled);

        // Calling methods on disposed state machine should not throw or change state
        sm.PointerEntered();
        sm.PointerExited();
        sm.UserInteracted();
        sm.DismissExplicit();

        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);
    }

    [Fact]
    public void RapidHoverLeaveHover_CyclesCorrectly()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        // Hover then quickly leave
        sm.PointerEntered();
        sm.PointerExited();
        Assert.Equal(OverlayState.Idle, sm.CurrentState);

        // Hover again and let it expand
        sm.PointerEntered();
        Assert.Equal(OverlayState.HoverPending, sm.CurrentState);
        timer.LastTimer?.Trigger();
        Assert.Equal(OverlayState.Expanded, sm.CurrentState);
    }

    [Fact]
    public void StateChanged_FiresOnTransitions_WithCorrectArgs()
    {
        var timer = new TestTimerService();
        using var sm = new OverlayStateMachine(null, timer);

        var events = new List<OverlayStateChangedEventArgs>();
        sm.StateChanged += (s, e) => events.Add(e);

        sm.PointerEntered();
        Assert.Single(events);
        Assert.Equal(OverlayState.Idle, events[0].PreviousState);
        Assert.Equal(OverlayState.HoverPending, events[0].NewState);

        timer.LastTimer?.Trigger();
        Assert.Equal(2, events.Count);
        Assert.Equal(OverlayState.HoverPending, events[1].PreviousState);
        Assert.Equal(OverlayState.Expanded, events[1].NewState);
    }
}
