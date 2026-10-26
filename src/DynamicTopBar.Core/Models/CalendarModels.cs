using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Model representing a single calendar event or schedule entry.
/// </summary>
public record CalendarEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Location { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public bool IsAllDay { get; init; }
    public string Source { get; init; } = "Local";

    public bool IsPast => EndTime < DateTimeOffset.Now;
    public bool IsUpcoming => StartTime > DateTimeOffset.Now;
    public bool IsOngoing => StartTime <= DateTimeOffset.Now && EndTime >= DateTimeOffset.Now;

    public string FormattedTimeRange
    {
        get
        {
            if (IsAllDay) return "All Day";
            string start = StartTime.LocalDateTime.ToString("h:mm tt");
            string end = EndTime.LocalDateTime.ToString("h:mm tt");
            return $"{start} - {end}";
        }
    }

    public string FormattedDate => StartTime.LocalDateTime.ToString("ddd, MMM d");
}

/// <summary>
/// Glance view of upcoming events, today's schedule, and countdown to next meeting.
/// </summary>
public record CalendarGlance
{
    public IReadOnlyList<CalendarEvent> TodayEvents { get; init; } = Array.Empty<CalendarEvent>();
    public IReadOnlyList<CalendarEvent> UpcomingEvents { get; init; } = Array.Empty<CalendarEvent>();
    public CalendarEvent? NextEvent { get; init; }
    public string NextEventCountdown { get; init; } = "No upcoming events";

    public static CalendarGlance Empty => new();
}
