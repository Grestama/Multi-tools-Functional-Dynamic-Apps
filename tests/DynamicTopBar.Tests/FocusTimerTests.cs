using System;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class FocusTimerTests
{
    [Fact]
    public void InitialState_IsIdle_WithDefault25MinDuration()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Idle, snapshot.State);
        Assert.Equal(FocusSessionMode.Work, snapshot.Mode);
        Assert.Equal(TimeSpan.FromMinutes(25), snapshot.Remaining);
        Assert.Equal(TimeSpan.FromMinutes(25), snapshot.TotalDuration);
        Assert.Equal(0.0, snapshot.ProgressPercentage);
        Assert.Equal("25:00", snapshot.FormattedRemaining);
        Assert.True(snapshot.CanStart);
        Assert.False(snapshot.CanPause);
        Assert.False(snapshot.CanResume);
        Assert.False(snapshot.CanReset);
    }

    [Fact]
    public void Start_TransitionsToRunning_AndSchedulesTimer()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        focusTimer.Start();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Running, snapshot.State);
        Assert.False(snapshot.CanStart);
        Assert.True(snapshot.CanPause);
        Assert.NotNull(timerService.LastTimer);
        Assert.Equal(1000, timerService.LastTimer.DelayMs);
    }

    [Fact]
    public void Tick_DecrementsRemaining_AndUpdatesProgress()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        focusTimer.Start();
        focusTimer.Tick();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(TimeSpan.FromSeconds(25 * 60 - 1), snapshot.Remaining);
        Assert.True(snapshot.ProgressPercentage > 0.0);
        Assert.Equal("24:59", snapshot.FormattedRemaining);
    }

    [Fact]
    public void Pause_RetainsRemaining_AndTransitionsToPaused()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        focusTimer.Start();
        focusTimer.Tick();
        focusTimer.Pause();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Paused, snapshot.State);
        Assert.Equal(TimeSpan.FromSeconds(25 * 60 - 1), snapshot.Remaining);
        Assert.True(snapshot.CanResume);
        Assert.True(snapshot.CanReset);
        Assert.False(snapshot.CanPause);
    }

    [Fact]
    public void Resume_RestartsRunningState()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        focusTimer.Start();
        focusTimer.Tick();
        focusTimer.Pause();
        focusTimer.Resume();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Running, snapshot.State);
        Assert.True(snapshot.CanPause);
    }

    [Fact]
    public void Reset_RestoresOriginalDuration_AndIdleState()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        focusTimer.Start();
        focusTimer.Tick();
        focusTimer.Reset();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Idle, snapshot.State);
        Assert.Equal(TimeSpan.FromMinutes(25), snapshot.Remaining);
        Assert.Equal("25:00", snapshot.FormattedRemaining);
        Assert.Equal(0.0, snapshot.ProgressPercentage);
    }

    [Fact]
    public void SelectPreset_SwitchesDurationAndMode()
    {
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService);

        var shortBreakPreset = new FocusTimerPreset("5m Break", FocusSessionMode.ShortBreak, TimeSpan.FromMinutes(5));
        focusTimer.SelectPreset(shortBreakPreset);

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusSessionMode.ShortBreak, snapshot.Mode);
        Assert.Equal(TimeSpan.FromMinutes(5), snapshot.Remaining);
        Assert.Equal("05:00", snapshot.FormattedRemaining);
        Assert.Equal(FocusTimerState.Idle, snapshot.State);
    }

    [Fact]
    public void TimerCompletion_IncrementsCompletedCount_ForWorkSessions()
    {
        var timerService = new TestTimerService();
        var shortWork = new FocusTimerPreset("2s Test", FocusSessionMode.Work, TimeSpan.FromSeconds(2));
        using var focusTimer = new FocusTimerService(timerService, shortWork);

        focusTimer.Start();
        focusTimer.Tick(); // remaining: 1s
        focusTimer.Tick(); // remaining: 0s -> completed!

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Completed, snapshot.State);
        Assert.Equal(TimeSpan.Zero, snapshot.Remaining);
        Assert.Equal(100.0, snapshot.ProgressPercentage);
        Assert.Equal(1, snapshot.CompletedSessionsCount);
        Assert.True(snapshot.CanStart);
    }

    [Fact]
    public void Start_PopulatesTargetEndTime()
    {
        var startTime = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService, null, () => startTime);

        focusTimer.Start();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(startTime.AddMinutes(25), snapshot.TargetEndTime);
    }

    [Fact]
    public void SleepResume_SynchronizesRemainingTime_BasedOnTargetEndTime()
    {
        var currentTime = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService, null, () => currentTime);

        focusTimer.Start(); // Starts 25m timer ending at 12:25:00

        // Simulate 10 minutes of machine sleep (time jumps to 12:10:00)
        currentTime = currentTime.AddMinutes(10);
        focusTimer.Synchronize();

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Running, snapshot.State);
        Assert.Equal(TimeSpan.FromMinutes(15), snapshot.Remaining);
        Assert.Equal("15:00", snapshot.FormattedRemaining);
    }

    [Fact]
    public void SleepResume_TransitionsToCompleted_WhenSleepExceedsDuration()
    {
        var currentTime = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var timerService = new TestTimerService();
        using var focusTimer = new FocusTimerService(timerService, null, () => currentTime);

        focusTimer.Start(); // Starts 25m work session

        // Simulate 30 minutes of sleep (past the 25m target end time)
        currentTime = currentTime.AddMinutes(30);

        var snapshot = focusTimer.CurrentSnapshot;
        Assert.Equal(FocusTimerState.Completed, snapshot.State);
        Assert.Equal(TimeSpan.Zero, snapshot.Remaining);
        Assert.Equal(1, snapshot.CompletedSessionsCount);
    }
}
