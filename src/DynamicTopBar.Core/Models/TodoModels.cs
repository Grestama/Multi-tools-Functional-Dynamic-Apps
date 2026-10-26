using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Priority level for a task.
/// </summary>
public enum TodoPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

/// <summary>
/// A task entry in the To-do List.
/// </summary>
public record TodoItem
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Notes { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public TodoPriority Priority { get; init; } = TodoPriority.Normal;
    public bool IsCompleted { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; init; }

    public string PriorityLabel => Priority switch
    {
        TodoPriority.High => "High",
        TodoPriority.Low => "Low",
        _ => "Normal"
    };

    public string PriorityColor => Priority switch
    {
        TodoPriority.High => "#FF5555",
        TodoPriority.Low => "#66BB6A",
        _ => "#A7A7A7"
    };
}
