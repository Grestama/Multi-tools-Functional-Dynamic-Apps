using System;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Handle to an active timer that can be cancelled or queried.
/// </summary>
public interface ITimerToken : IDisposable
{
    bool IsActive { get; }
    void Cancel();
}

/// <summary>
/// Time abstraction allowing deterministic, mockable delays in unit tests.
/// </summary>
public interface ITimerService
{
    ITimerToken StartTimer(int delayMs, Action callback);
}
