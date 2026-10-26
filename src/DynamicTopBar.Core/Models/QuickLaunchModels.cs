namespace DynamicTopBar.Core.Models;

/// <summary>
/// Type of destination target for a quick launch shortcut.
/// </summary>
public enum QuickLaunchTargetType
{
    Application,
    Folder,
    Url,
    WindowsTool
}

/// <summary>
/// Descriptor representing a user or system pinned quick launch item.
/// </summary>
public record QuickLaunchItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string PathOrUri { get; init; }
    public required string IconGlyph { get; init; }
    public QuickLaunchTargetType Type { get; init; }
    public string? Arguments { get; init; }
    public bool IsCustom { get; init; }
}

/// <summary>
/// Execution or validation result for a quick launch action.
/// </summary>
public record QuickLaunchResult(bool Success, string? ErrorMessage = null)
{
    public static QuickLaunchResult Ok() => new(true);
    public static QuickLaunchResult Fail(string error) => new(false, error);
}
