using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service coordinating evaluation and delivery of local notifications for due To-do tasks and Calendar events.
/// </summary>
public sealed class ReminderService : IReminderService
{
    private readonly ITodoService? _todoService;
    private readonly ICalendarService? _calendarService;
    private readonly object _lock = new();
    private readonly List<AppReminder> _activeReminders = new();
    private readonly HashSet<string> _dismissedIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDisposed;

    public IReadOnlyList<AppReminder> ActiveReminders
    {
        get { lock (_lock) return _activeReminders.Where(r => !r.IsDismissed).ToList(); }
    }

    public event EventHandler<AppReminder>? ReminderFired;
    public event EventHandler<IReadOnlyList<AppReminder>>? RemindersChanged;

    public ReminderService(ITodoService? todoService = null, ICalendarService? calendarService = null)
    {
        _todoService = todoService;
        _calendarService = calendarService;

        if (_todoService != null)
        {
            _todoService.ItemsChanged += OnTodoItemsChanged;
        }

        if (_calendarService != null)
        {
            _calendarService.GlanceChanged += OnCalendarGlanceChanged;
        }
    }

    private void OnTodoItemsChanged(object? sender, IReadOnlyList<TodoItem> e) => CheckReminders();
    private void OnCalendarGlanceChanged(object? sender, CalendarGlance e) => CheckReminders();

    public void CheckReminders()
    {
        if (_isDisposed) return;

        var now = DateTimeOffset.Now;
        var newReminders = new List<AppReminder>();

        // 1. Evaluate Due Tasks from TodoService (tasks due within next 60 minutes or overdue)
        if (_todoService != null)
        {
            foreach (var item in _todoService.Items)
            {
                if (!item.IsCompleted && item.DueDate.HasValue)
                {
                    if (item.DueDate.Value <= now.AddMinutes(60))
                    {
                        string reminderId = $"todo_{item.Id}";
                        lock (_lock)
                        {
                            if (!_dismissedIds.Contains(reminderId) && !_activeReminders.Any(r => r.Id == reminderId))
                            {
                                var reminder = new AppReminder
                                {
                                    Id = reminderId,
                                    SourceFeatureId = "todo",
                                    Title = item.DueDate.Value < now ? "Task Overdue" : "Task Due Soon",
                                    Message = item.Title,
                                    DueTime = item.DueDate.Value
                                };
                                _activeReminders.Add(reminder);
                                newReminders.Add(reminder);
                            }
                        }
                    }
                }
            }
        }

        // 2. Evaluate Upcoming Meetings from CalendarService (starts within next 15 minutes)
        if (_calendarService != null)
        {
            foreach (var evt in _calendarService.Events)
            {
                if (evt.StartTime >= now && evt.StartTime <= now.AddMinutes(15))
                {
                    string reminderId = $"cal_{evt.Id}";
                    lock (_lock)
                    {
                        if (!_dismissedIds.Contains(reminderId) && !_activeReminders.Any(r => r.Id == reminderId))
                        {
                            var reminder = new AppReminder
                            {
                                Id = reminderId,
                                SourceFeatureId = "calendar",
                                Title = "Upcoming Event",
                                Message = $"{evt.Title} ({evt.FormattedTimeRange})",
                                DueTime = evt.StartTime
                            };
                            _activeReminders.Add(reminder);
                            newReminders.Add(reminder);
                        }
                    }
                }
            }
        }

        if (newReminders.Count > 0)
        {
            foreach (var rem in newReminders)
            {
                ReminderFired?.Invoke(this, rem);
            }
            RemindersChanged?.Invoke(this, ActiveReminders);
        }
    }

    public void DismissReminder(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        bool updated = false;
        lock (_lock)
        {
            _dismissedIds.Add(id);
            int idx = _activeReminders.FindIndex(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _activeReminders[idx] = _activeReminders[idx] with { IsDismissed = true };
                updated = true;
            }
        }

        if (updated)
        {
            RemindersChanged?.Invoke(this, ActiveReminders);
        }
    }

    public void DismissAll()
    {
        lock (_lock)
        {
            foreach (var r in _activeReminders)
            {
                _dismissedIds.Add(r.Id);
            }
            _activeReminders.Clear();
        }

        RemindersChanged?.Invoke(this, ActiveReminders);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_todoService != null)
            {
                _todoService.ItemsChanged -= OnTodoItemsChanged;
            }

            if (_calendarService != null)
            {
                _calendarService.GlanceChanged -= OnCalendarGlanceChanged;
            }
        }
    }
}
