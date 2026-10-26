using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.States;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service managing desktop notification queueing, deduplication, burst handling, and capsule alerts.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const int MaxHistoryCount = 30;
    private const int MaxQueueCount = 10;
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(5);

    private readonly object _lock = new();
    private readonly ITimerService _timerService;
    private readonly Queue<AppNotificationItem> _queue = new();
    private readonly List<AppNotificationItem> _history = new();
    private readonly List<(string Key, DateTimeOffset Timestamp)> _recentSignatures = new();

    private NotificationPreferences _preferences;
    private AppNotificationItem? _currentActive;
    private ITimerToken? _activeDismissToken;
    private bool _isDisposed;

    public AppNotificationItem? CurrentActiveNotification
    {
        get { lock (_lock) return _currentActive; }
    }

    public IReadOnlyList<AppNotificationItem> NotificationHistory
    {
        get
        {
            lock (_lock)
            {
                return _history.OrderByDescending(n => n.Timestamp).ToList();
            }
        }
    }

    public NotificationPreferences Preferences
    {
        get { lock (_lock) return _preferences; }
    }

    public event EventHandler<AppNotificationItem?>? ActiveNotificationChanged;
    public event EventHandler<IReadOnlyList<AppNotificationItem>>? HistoryChanged;

    public NotificationService(ITimerService? timerService = null, NotificationPreferences? initialPreferences = null)
    {
        _timerService = timerService ?? new DefaultTimerService();
        _preferences = initialPreferences ?? NotificationPreferences.Default;
    }

    public bool PostNotification(
        string title,
        string message,
        string sourceAppName = "System",
        NotificationPriority priority = NotificationPriority.Normal,
        string? iconGlyph = null)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        AppNotificationItem item;

        lock (_lock)
        {
            if (_isDisposed || !_preferences.IsEnabled)
            {
                return false;
            }

            // Check blocked apps
            if (_preferences.BlockedAppSources.Any(b => string.Equals(b, sourceAppName, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Deduplication check
            string signature = $"{sourceAppName}::{title}::{message}";
            _recentSignatures.RemoveAll(s => now - s.Timestamp > DeduplicationWindow);
            if (_recentSignatures.Any(s => s.Key.Equals(signature, StringComparison.Ordinal)))
            {
                return false; // Suppress duplicate
            }
            _recentSignatures.Add((signature, now));

            item = new AppNotificationItem
            {
                Title = title.Trim(),
                Message = message.Trim(),
                SourceAppName = sourceAppName.Trim(),
                IconGlyph = !string.IsNullOrEmpty(iconGlyph) ? iconGlyph : "\uEA8F",
                Timestamp = now,
                Priority = priority
            };

            // Add to history (prune excess)
            _history.Insert(0, item);
            while (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            // Add to queue if active notification is currently showing
            if (_currentActive != null)
            {
                if (_queue.Count < MaxQueueCount)
                {
                    _queue.Enqueue(item);
                }
            }
            else
            {
                SetActiveNotificationUnderLock(item);
            }
        }

        HistoryChanged?.Invoke(this, NotificationHistory);
        return true;
    }

    public void DismissActiveNotification()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            CancelDismissTimerUnderLock();
            ProcessNextInQueueUnderLock();
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }
        HistoryChanged?.Invoke(this, NotificationHistory);
    }

    public void UpdatePreferences(NotificationPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        lock (_lock)
        {
            _preferences = preferences;
            if (!_preferences.IsEnabled && _currentActive != null)
            {
                DismissActiveNotification();
            }
        }
    }

    private void SetActiveNotificationUnderLock(AppNotificationItem item)
    {
        _currentActive = item;
        ScheduleDismissTimerUnderLock();
        ActiveNotificationChanged?.Invoke(this, _currentActive);
    }

    private void ProcessNextInQueueUnderLock()
    {
        if (_queue.Count > 0)
        {
            var next = _queue.Dequeue();
            SetActiveNotificationUnderLock(next);
        }
        else
        {
            _currentActive = null;
            ActiveNotificationChanged?.Invoke(this, null);
        }
    }

    private void ScheduleDismissTimerUnderLock()
    {
        CancelDismissTimerUnderLock();
        int durationMs = Math.Max(1, _preferences.DisplayDurationSeconds) * 1000;

        _activeDismissToken = _timerService.StartTimer(durationMs, () =>
        {
            lock (_lock)
            {
                if (_isDisposed) return;
                ProcessNextInQueueUnderLock();
            }
        });
    }

    private void CancelDismissTimerUnderLock()
    {
        _activeDismissToken?.Cancel();
        _activeDismissToken = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
            CancelDismissTimerUnderLock();
            _queue.Clear();
        }
    }
}
