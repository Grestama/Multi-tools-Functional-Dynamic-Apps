using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Composite snapshot aggregating productivity data across Calendar, Todo, Notes, and Focus Timer
/// without duplicating underlying storage.
/// </summary>
public record ProductivityGlance
{
    public IReadOnlyList<CalendarEvent> UpcomingEvents { get; init; } = Array.Empty<CalendarEvent>();
    public IReadOnlyList<TodoItem> ActiveTodos { get; init; } = Array.Empty<TodoItem>();
    public NoteItem? ActiveNote { get; init; }
    public FocusTimerSnapshot FocusTimer { get; init; } = new FocusTimerSnapshot();
    public int PendingReminderCount { get; init; }
    public string SummaryText { get; init; } = "Productivity Hub Ready";
    public UserProgressionSnapshot? GamificationProgression { get; init; }

    public static ProductivityGlance Empty() => new();
}
