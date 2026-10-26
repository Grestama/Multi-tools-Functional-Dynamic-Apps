using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Persistence contract for local calendar events.
/// </summary>
public interface ICalendarRepository
{
    Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAllAsync(IReadOnlyList<CalendarEvent> events, CancellationToken cancellationToken = default);
}
