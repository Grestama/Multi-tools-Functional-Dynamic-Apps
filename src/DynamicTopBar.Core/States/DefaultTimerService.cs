using System;
using System.Threading;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.Core.States;

/// <summary>
/// Production timer service backed by System.Threading.Timer.
/// </summary>
public sealed class DefaultTimerService : ITimerService
{
    private sealed class TimerToken : ITimerToken
    {
        private Timer? _timer;
        private int _state; // 0 = active, 1 = fired/cancelled

        public bool IsActive => _state == 0;

        public TimerToken(int delayMs, Action callback)
        {
            _timer = new Timer(_ =>
            {
                // Atomically transition from active(0) to fired(1)
                if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
                {
                    DisposeTimer();
                    callback();
                }
            }, null, delayMs, Timeout.Infinite);
        }

        public void Cancel()
        {
            // Atomically transition from active(0) to cancelled(1)
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                DisposeTimer();
            }
        }

        public void Dispose()
        {
            Cancel();
        }

        private void DisposeTimer()
        {
            var t = Interlocked.Exchange(ref _timer, null);
            t?.Dispose();
        }
    }

    public ITimerToken StartTimer(int delayMs, Action callback)
    {
        return new TimerToken(delayMs, callback);
    }
}
