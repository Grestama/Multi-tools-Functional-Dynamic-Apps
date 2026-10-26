using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing notification queueing, deduplication, privacy masking, and capsule ticker alerts.
/// </summary>
public interface INotificationService : IDisposable
{
    AppNotificationItem? CurrentActiveNotification { get; }
    IReadOnlyList<AppNotificationItem> NotificationHistory { get; }
    NotificationPreferences Preferences { get; }

    event EventHandler<AppNotificationItem?>? ActiveNotificationChanged;
    event EventHandler<IReadOnlyList<AppNotificationItem>>? HistoryChanged;

    bool PostNotification(
        string title,
        string message,
        string sourceAppName = "System",
        NotificationPriority priority = NotificationPriority.Normal,
        string? iconGlyph = null);

    bool PostNotification(AppNotificationItem item) => PostNotification(item.Title, item.Message, item.SourceAppName, item.Priority, item.IconGlyph);
    bool Post(AppNotificationItem item) => PostNotification(item);
    bool Post(string title, string message, string sourceAppName = "System", NotificationPriority priority = NotificationPriority.Normal) => PostNotification(title, message, sourceAppName, priority);

    void DismissActiveNotification();
    void DismissActive() => DismissActiveNotification();
    void ClearHistory();
    void UpdatePreferences(NotificationPreferences preferences);
}
