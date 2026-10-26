using System;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract providing lightweight, verified system glance metrics
/// (realtime clock, battery status, machine info, uptime) without aggressive polling.
/// </summary>
public interface ISystemGlanceService : IDisposable
{
    /// <summary>
    /// Current snapshot of system metrics.
    /// </summary>
    SystemGlanceSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// Raised whenever system metrics change (clock tick, power event, or manual refresh).
    /// </summary>
    event EventHandler<SystemGlanceSnapshot>? SnapshotChanged;

    /// <summary>
    /// Forces an immediate update of system metrics.
    /// </summary>
    void Refresh();
}
