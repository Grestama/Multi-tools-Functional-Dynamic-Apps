using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Model representing an app-generated local reminder from To-do tasks or Calendar events.
/// </summary>
public record AppReminder
{
    public required string Id { get; init; }
    public required string SourceFeatureId { get; init; } // "todo" or "calendar"
    public required string Title { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset DueTime { get; init; }
    public bool IsDismissed { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string FormattedDueTime => DueTime.LocalDateTime.ToString("h:mm tt");
}
