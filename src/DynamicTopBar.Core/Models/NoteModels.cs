using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Model representing a single desktop scratchpad note.
/// </summary>
public record NoteItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = "Untitled Note";
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool IsPinned { get; init; }

    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Content)) return "Empty note";
            string firstLine = Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
            return firstLine.Length > 50 ? firstLine.Substring(0, 47) + "..." : firstLine;
        }
    }

    public string FormattedModified => ModifiedAt.LocalDateTime.ToString("MMM d, h:mm tt");
}
