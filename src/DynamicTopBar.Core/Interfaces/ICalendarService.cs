using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing calendar events, ICS file import/export, and schedule glances.
/// </summary>
public interface ICalendarService : IDisposable
{
    IReadOnlyList<CalendarEvent> Events { get; }
    CalendarGlance Glance { get; }

    event EventHandler<CalendarGlance>? GlanceChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<CalendarEvent> AddEventAsync(string title, DateTimeOffset startTime, DateTimeOffset endTime, string? location = null, string? description = null, bool isAllDay = false, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> ImportIcsAsync(string icsContent, CancellationToken cancellationToken = default);
    string ExportToIcs();
}
