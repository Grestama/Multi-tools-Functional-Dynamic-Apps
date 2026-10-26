using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum ClipboardContentType
{
    Text,
    Url,
    Code,
    Json,
    Email,
    Number
}

/// <summary>
/// Model representing a single captured text snippet with content categorization and metadata.
/// </summary>
public record ClipboardSnippet
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Text { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool IsPinned { get; init; }
    public ClipboardContentType ContentType { get; init; } = ClipboardContentType.Text;
    public string? SourceAppName { get; init; }

    public int CharCount => Text.Length;

    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Text)) return "Empty snippet";
            string clean = Text.Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length > 60 ? clean.Substring(0, 57) + "..." : clean;
        }
    }

    public string FormattedTime => Timestamp.LocalDateTime.ToString("h:mm tt");
}

/// <summary>
/// Status snapshot for the Clipboard module, enforcing transparent privacy state.
/// </summary>
public record ClipboardState
{
    public bool IsOptInEnabled { get; init; }
    public bool IsMonitoring { get; init; }
    public int SnippetCount { get; init; }
    public string StatusText { get; init; } = "Disabled (Opt-in required)";

    public static ClipboardState DefaultDisabled => new()
    {
        IsOptInEnabled = false,
        IsMonitoring = false,
        SnippetCount = 0,
        StatusText = "Disabled (Opt-in required)"
    };
}

/// <summary>
/// Advanced preferences and security filters for clipboard history management.
/// </summary>
public record ClipboardPreferences
{
    public int MaxSnippetCapacity { get; init; } = 25;
    public int AutoPruneDays { get; init; } = 7;
    public bool ExcludeSensitivePatterns { get; init; } = true;
    public List<string> ExcludedApplications { get; init; } = new()
    {
        "1Password",
        "Bitwarden",
        "KeePass",
        "KeePassXC",
        "LastPass"
    };
}
