using System;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class ProductivityHubTests
{
    [Fact]
    public async Task RefreshGlance_AggregatesDataAcrossServices()
    {
        var calendarService = new CalendarService(new MemoryCalendarRepository());
        var todoService = new TodoService(new MemoryTodoRepository());
        var notesService = new NotesService(new MemoryNotesRepository());
        var focusTimerService = new FocusTimerService(new TestTimerService());

        using var hub = new ProductivityHubService(calendarService, todoService, notesService, focusTimerService);

        // Add calendar event
        var now = DateTimeOffset.Now;
        await calendarService.AddEventAsync("Standup Meeting", now.AddHours(1), now.AddHours(2));

        // Add todo item
        await todoService.AddItemAsync("Ship Phase 2", TodoPriority.High);

        // Add note
        await notesService.CreateNoteAsync("Release Notes", "Phase 2 done");

        var glance = hub.RefreshGlance();

        Assert.Single(glance.UpcomingEvents);
        Assert.Equal("Standup Meeting", glance.UpcomingEvents[0].Title);

        Assert.Single(glance.ActiveTodos);
        Assert.Equal("Ship Phase 2", glance.ActiveTodos[0].Title);

        Assert.NotNull(glance.ActiveNote);
        Assert.Equal("Release Notes", glance.ActiveNote.Title);

        Assert.Contains("1 active task", glance.SummaryText);
    }

    [Fact]
    public async Task QuickAddTodoAsync_CreatesTodoAndRefreshesGlance()
    {
        var calendarService = new CalendarService(new MemoryCalendarRepository());
        var todoService = new TodoService(new MemoryTodoRepository());
        var notesService = new NotesService(new MemoryNotesRepository());
        var focusTimerService = new FocusTimerService(new TestTimerService());

        using var hub = new ProductivityHubService(calendarService, todoService, notesService, focusTimerService);

        var item = await hub.QuickAddTodoAsync("Write documentation", TodoPriority.High);

        Assert.NotNull(item);
        Assert.Equal("Write documentation", item.Title);
        Assert.Single(hub.CurrentGlance.ActiveTodos);
        Assert.Single(todoService.Items);
    }

    [Fact]
    public async Task QuickAddNoteAsync_CreatesNoteAndRefreshesGlance()
    {
        var calendarService = new CalendarService(new MemoryCalendarRepository());
        var todoService = new TodoService(new MemoryTodoRepository());
        var notesService = new NotesService(new MemoryNotesRepository());
        var focusTimerService = new FocusTimerService(new TestTimerService());

        using var hub = new ProductivityHubService(calendarService, todoService, notesService, focusTimerService);

        var note = await hub.QuickAddNoteAsync("Architecture Decision", "Modular hotkeys implemented");

        Assert.NotNull(note);
        Assert.Equal("Architecture Decision", note.Title);
        Assert.Equal("Architecture Decision", hub.CurrentGlance.ActiveNote?.Title);
        Assert.Single(notesService.Notes);
    }

    [Fact]
    public void QuickStartFocusTimer_StartsFocusSessionAndRefreshesGlance()
    {
        var calendarService = new CalendarService(new MemoryCalendarRepository());
        var todoService = new TodoService(new MemoryTodoRepository());
        var notesService = new NotesService(new MemoryNotesRepository());
        var timerService = new TestTimerService();
        var focusTimerService = new FocusTimerService(timerService);

        using var hub = new ProductivityHubService(calendarService, todoService, notesService, focusTimerService);

        hub.QuickStartFocusTimer();

        Assert.Equal(FocusTimerState.Running, hub.CurrentGlance.FocusTimer.State);
        Assert.Contains("Focus:", hub.CurrentGlance.SummaryText);
    }

    [Fact]
    public async Task UnderlyingServiceUpdates_RaiseGlanceChangedAutomatically()
    {
        var calendarService = new CalendarService(new MemoryCalendarRepository());
        var todoService = new TodoService(new MemoryTodoRepository());
        var notesService = new NotesService(new MemoryNotesRepository());
        var focusTimerService = new FocusTimerService(new TestTimerService());

        using var hub = new ProductivityHubService(calendarService, todoService, notesService, focusTimerService);

        int glanceChangeEvents = 0;
        hub.GlanceChanged += (s, e) => glanceChangeEvents++;

        await todoService.AddItemAsync("Task 1");
        await todoService.AddItemAsync("Task 2");

        Assert.True(glanceChangeEvents >= 2);
        Assert.Equal(2, hub.CurrentGlance.ActiveTodos.Count);
    }
}
