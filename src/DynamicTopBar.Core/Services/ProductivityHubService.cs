using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Composite service aggregating Calendar, Todo, Notes, and Focus Timer without duplicate storage.
/// </summary>
public sealed class ProductivityHubService : IProductivityHubService
{
    private readonly ICalendarService _calendarService;
    private readonly ITodoService _todoService;
    private readonly INotesService _notesService;
    private readonly IFocusTimerService _focusTimerService;
    private readonly object _lock = new();

    private ProductivityGlance _currentGlance = ProductivityGlance.Empty();
    private bool _isDisposed;

    public ProductivityGlance CurrentGlance
    {
        get { lock (_lock) return _currentGlance; }
        private set
        {
            lock (_lock) _currentGlance = value;
            GlanceChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<ProductivityGlance>? GlanceChanged;

    public ProductivityHubService(
        ICalendarService calendarService,
        ITodoService todoService,
        INotesService notesService,
        IFocusTimerService focusTimerService)
    {
        _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        _notesService = notesService ?? throw new ArgumentNullException(nameof(notesService));
        _focusTimerService = focusTimerService ?? throw new ArgumentNullException(nameof(focusTimerService));

        _calendarService.GlanceChanged += OnCalendarGlanceChanged;
        _todoService.ItemsChanged += OnTodoItemsChanged;
        _notesService.NotesChanged += OnNotesChanged;
        _notesService.SelectedNoteChanged += OnSelectedNoteChanged;
        _focusTimerService.SnapshotChanged += OnFocusSnapshotChanged;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        RefreshGlance();
        return Task.CompletedTask;
    }

    public ProductivityGlance RefreshGlance()
    {
        var now = DateTimeOffset.Now;

        // Upcoming events (today and next 24 hours)
        var upcomingEvents = _calendarService.Events
            .Where(e => e.EndTime > now && e.StartTime <= now.AddDays(1))
            .OrderBy(e => e.StartTime)
            .Take(5)
            .ToList();

        // Active todo tasks (not completed)
        var activeTodos = _todoService.Items
            .Where(t => !t.IsCompleted)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue)
            .Take(5)
            .ToList();

        // Active note (selected or latest pinned or most recent)
        var activeNote = _notesService.SelectedNote
            ?? _notesService.Notes.FirstOrDefault(n => n.IsPinned)
            ?? _notesService.Notes.OrderByDescending(n => n.ModifiedAt).FirstOrDefault();

        // Focus timer state
        var focusSnapshot = _focusTimerService.CurrentSnapshot;

        // Overdue or due today reminders
        int overdueTasks = activeTodos.Count(t => t.DueDate.HasValue && t.DueDate.Value.Date <= now.Date);
        int todayEvents = upcomingEvents.Count(e => e.StartTime.Date == now.Date);
        int pendingReminders = overdueTasks + todayEvents;

        // Build human-friendly summary
        var summaryParts = new List<string>();
        if (activeTodos.Count > 0)
        {
            summaryParts.Add($"{activeTodos.Count} active task{(activeTodos.Count > 1 ? "s" : "")}");
        }
        else
        {
            summaryParts.Add("All tasks completed");
        }

        if (upcomingEvents.Count > 0)
        {
            summaryParts.Add($"{upcomingEvents.Count} event{(upcomingEvents.Count > 1 ? "s" : "")} soon");
        }

        if (focusSnapshot.State == FocusTimerState.Running)
        {
            summaryParts.Add($"Focus: {focusSnapshot.FormattedRemaining}");
        }

        string summary = summaryParts.Count > 0 ? string.Join(" · ", summaryParts) : "No pending items";

        var newGlance = new ProductivityGlance
        {
            UpcomingEvents = upcomingEvents,
            ActiveTodos = activeTodos,
            ActiveNote = activeNote,
            FocusTimer = focusSnapshot,
            PendingReminderCount = pendingReminders,
            SummaryText = summary
        };

        CurrentGlance = newGlance;
        return newGlance;
    }

    public async Task<TodoItem> QuickAddTodoAsync(string title, TodoPriority priority = TodoPriority.Normal, CancellationToken cancellationToken = default)
    {
        var item = await _todoService.AddItemAsync(title, priority, cancellationToken: cancellationToken);
        RefreshGlance();
        return item;
    }

    public async Task<NoteItem> QuickAddNoteAsync(string title, string content, CancellationToken cancellationToken = default)
    {
        var note = await _notesService.CreateNoteAsync(title, content, cancellationToken);
        RefreshGlance();
        return note;
    }

    public void QuickStartFocusTimer()
    {
        _focusTimerService.Start();
        RefreshGlance();
    }

    private void OnCalendarGlanceChanged(object? sender, CalendarGlance e) => RefreshGlance();
    private void OnTodoItemsChanged(object? sender, IReadOnlyList<TodoItem> e) => RefreshGlance();
    private void OnNotesChanged(object? sender, IReadOnlyList<NoteItem> e) => RefreshGlance();
    private void OnSelectedNoteChanged(object? sender, NoteItem? e) => RefreshGlance();
    private void OnFocusSnapshotChanged(object? sender, FocusTimerSnapshot e) => RefreshGlance();

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _calendarService.GlanceChanged -= OnCalendarGlanceChanged;
        _todoService.ItemsChanged -= OnTodoItemsChanged;
        _notesService.NotesChanged -= OnNotesChanged;
        _notesService.SelectedNoteChanged -= OnSelectedNoteChanged;
        _focusTimerService.SnapshotChanged -= OnFocusSnapshotChanged;
    }
}
