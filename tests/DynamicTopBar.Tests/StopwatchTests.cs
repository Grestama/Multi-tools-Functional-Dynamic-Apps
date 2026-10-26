using System;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class StopwatchTests
{
    [Fact]
    public void InitialState_IsStoppedWithZeroElapsed()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        var snapshot = sw.CurrentSnapshot;
        Assert.False(snapshot.IsRunning);
        Assert.Equal(TimeSpan.Zero, snapshot.Elapsed);
        Assert.Equal("00:00.0", snapshot.FormattedElapsed);
        Assert.Empty(snapshot.Laps);
    }

    [Fact]
    public void Start_SetsIsRunningTrue_AndSchedulesTimer()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        sw.Start();

        Assert.True(sw.CurrentSnapshot.IsRunning);
        Assert.NotNull(timerService.LastTimer);
        Assert.Equal(100, timerService.LastTimer.DelayMs);
    }

    [Fact]
    public void Advance_IncrementsElapsedCorrectly()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        sw.Advance(TimeSpan.FromSeconds(12.5));

        var snapshot = sw.CurrentSnapshot;
        Assert.Equal(12.5, snapshot.Elapsed.TotalSeconds);
        Assert.Equal("00:12.5", snapshot.FormattedElapsed);
    }

    [Fact]
    public void Stop_RetainsAccumulatedTime()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        sw.Advance(TimeSpan.FromSeconds(5.2));
        sw.Stop();

        Assert.False(sw.CurrentSnapshot.IsRunning);
        Assert.Equal(5.2, sw.CurrentSnapshot.Elapsed.TotalSeconds);
    }

    [Fact]
    public void Reset_ClearsElapsedAndLaps()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        sw.Advance(TimeSpan.FromSeconds(10));
        sw.Lap();
        sw.Reset();

        var snapshot = sw.CurrentSnapshot;
        Assert.False(snapshot.IsRunning);
        Assert.Equal(TimeSpan.Zero, snapshot.Elapsed);
        Assert.Empty(snapshot.Laps);
    }

    [Fact]
    public void Lap_RecordsCurrentElapsedLapTime()
    {
        var timerService = new TestTimerService();
        using var sw = new StopwatchService(timerService);

        sw.Advance(TimeSpan.FromSeconds(3.5));
        sw.Lap();

        sw.Advance(TimeSpan.FromSeconds(2.0));
        sw.Lap();

        var snapshot = sw.CurrentSnapshot;
        Assert.Equal(2, snapshot.Laps.Count);
        Assert.Equal(3.5, snapshot.Laps[0].TotalSeconds);
        Assert.Equal(5.5, snapshot.Laps[1].TotalSeconds);
    }
}
