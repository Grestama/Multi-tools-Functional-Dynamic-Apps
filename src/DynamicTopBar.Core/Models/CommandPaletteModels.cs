using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// A searchable, executable desktop action displayed in the Command Palette.
/// </summary>
public record PaletteCommand
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public string? Description { get; init; }
    public string? ModuleContextId { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public string IconGlyph { get; init; } = "\uE71D";
    public string? ShortcutHint { get; init; }
    public Func<Task>? AsyncAction { get; init; }
    public Action? SyncAction { get; init; }
    public Action? Action { init => SyncAction = value; }
    public Func<bool>? AvailabilityPredicate { get; init; }

    public bool IsAvailable => AvailabilityPredicate?.Invoke() ?? true;

    public async Task<bool> ExecuteAsync()
    {
        if (!IsAvailable) return false;

        if (AsyncAction != null)
        {
            await AsyncAction();
            return true;
        }

        if (SyncAction != null)
        {
            SyncAction();
            return true;
        }

        return false;
    }
}

public record CommandSearchResult(PaletteCommand Command, int MatchScore);
