using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// User preferences controlling which feature modules are enabled,
/// their display order, and the last active module ID.
/// </summary>
public record FeaturePreferences
{
    /// <summary>
    /// Set of stable module identifiers currently enabled for display.
    /// </summary>
    public List<string> EnabledFeatureIds { get; init; } = new()
    {
        "media",
        "spotify",
        "clock",
        "weather",
        "todo",
        "focus",
        "quick-launch",
        "system-glance",
        "calendar",
        "notes",
        "clipboard"
    };

    /// <summary>
    /// Sequential order of stable module identifiers.
    /// </summary>
    public List<string> FeatureOrder { get; init; } = new()
    {
        "media",
        "spotify",
        "clock",
        "weather",
        "todo",
        "focus",
        "quick-launch",
        "system-glance",
        "calendar",
        "notes",
        "clipboard"
    };

    /// <summary>
    /// Identifier of the module that was last actively selected by the user.
    /// </summary>
    public string? LastActiveFeatureId { get; init; } = "media";

    /// <summary>
    /// Optional pinned feature module that acts as primary startup landing.
    /// </summary>
    public string? PinnedFeatureId { get; init; }

    /// <summary>
    /// Display clock in 24-hour format instead of 12-hour AM/PM.
    /// </summary>
    public bool ClockUse24HourFormat { get; init; }

    /// <summary>
    /// Temperature unit for weather display (Celsius or Fahrenheit).
    /// </summary>
    public WeatherUnit WeatherUnit { get; init; } = WeatherUnit.Celsius;

    /// <summary>
    /// Active accent color hex code (e.g. #00B4FF, #00E676, #B388FF, #F5F5F5).
    /// </summary>
    public string AccentColorHex { get; init; } = "#00B4FF";

    /// <summary>
    /// Explicit privacy opt-in consent for clipboard snippet capture.
    /// Must remain false by default.
    /// </summary>
    public bool ClipboardOptIn { get; init; }

    /// <summary>
    /// Enables app-generated notifications for due tasks and calendar events.
    /// </summary>
    public bool RemindersEnabled { get; init; } = true;

    /// <summary>
    /// When enabled, the top bar automatically conceals itself to the top screen edge,
    /// displaying a sleek trigger line/notch until hovered or clicked.
    /// </summary>
    public bool AutoHideEnabled { get; init; } = true;

    public static FeaturePreferences Default => new();
}
