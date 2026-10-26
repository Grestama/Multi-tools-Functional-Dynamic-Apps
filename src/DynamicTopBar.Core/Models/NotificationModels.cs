using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Immutable representation of an incoming application notification.
/// </summary>
public record AppNotificationItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string SourceAppName { get; init; } = "System";
    public string SourceApp { get => SourceAppName; init => SourceAppName = value; }
    public string? AppSourceId { get; init; }
    public string IconGlyph { get; init; } = "\uEA8F"; // Segoe Info / Bell
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    public TimeSpan? ExpiresAfter { get; init; }
    public bool IsRead { get; init; }

    public string GetMaskedMessage(bool privacyMode)
    {
        if (privacyMode)
        {
            return $"Notification from {SourceAppName}";
        }
        return Message;
    }
}

/// <summary>
/// User configuration for notification alerts, privacy filters, and burst handling.
/// </summary>
public record NotificationPreferences
{
    public bool IsEnabled { get; init; } = true;
    public bool PrivacyModeEnabled { get; init; } = false;
    public bool PrivacyMode { get => PrivacyModeEnabled; init => PrivacyModeEnabled = value; }
    public int DisplayDurationSeconds { get; init; } = 4;
    public bool ShowOnCapsule { get; init; } = true;
    public List<string> BlockedAppSources { get; init; } = new();

    public static NotificationPreferences Default => new();
}
