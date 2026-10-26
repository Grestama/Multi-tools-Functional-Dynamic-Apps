using System;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class ReminderServiceTests
{
    [Fact]
    public async Task CheckReminders_WhenTaskDueWithinHour_FiresReminder()
    {
        var todoRepo = new MemoryTodoRepository();
        var todoService = new TodoService(todoRepo);
        var calRepo = new MemoryCalendarRepository();
        var calService = new CalendarService(calRepo);

        var reminderService = new ReminderService(todoService, calService);

        bool fired = false;
        reminderService.ReminderFired += (s, e) => fired = true;

        var dueTime = DateTimeOffset.Now.AddMinutes(25);
        await todoService.AddItemAsync("Prepare sprint presentation", TodoPriority.High, dueTime);

        reminderService.CheckReminders();

        Assert.True(fired);
        Assert.Single(reminderService.ActiveReminders);
        var rem = reminderService.ActiveReminders[0];
        Assert.Equal("todo", rem.SourceFeatureId);
        Assert.Contains("Prepare sprint presentation", rem.Message);
    }

    [Fact]
    public async Task CheckReminders_WhenMeetingStartingSoon_FiresReminder()
    {
        var todoRepo = new MemoryTodoRepository();
        var todoService = new TodoService(todoRepo);
        var calRepo = new MemoryCalendarRepository();
        var calService = new CalendarService(calRepo);

        var reminderService = new ReminderService(todoService, calService);

        var start = DateTimeOffset.Now.AddMinutes(10);
        await calService.AddEventAsync("Executive Sync", start, start.AddMinutes(30), "Boardroom");

        reminderService.CheckReminders();

        Assert.Single(reminderService.ActiveReminders);
        var rem = reminderService.ActiveReminders[0];
        Assert.Equal("calendar", rem.SourceFeatureId);
        Assert.Contains("Executive Sync", rem.Message);
    }

    [Fact]
    public async Task DismissReminder_RemovesFromActiveReminders_AndSuppressesFutureTriggers()
    {
        var todoRepo = new MemoryTodoRepository();
        var todoService = new TodoService(todoRepo);
        var calRepo = new MemoryCalendarRepository();
        var calService = new CalendarService(calRepo);

        var reminderService = new ReminderService(todoService, calService);

        var dueTime = DateTimeOffset.Now.AddMinutes(15);
        await todoService.AddItemAsync("Complete timesheet", TodoPriority.Normal, dueTime);

        reminderService.CheckReminders();
        Assert.Single(reminderService.ActiveReminders);
        string id = reminderService.ActiveReminders[0].Id;

        reminderService.DismissReminder(id);
        Assert.Empty(reminderService.ActiveReminders);

        // Re-checking must not re-add the dismissed reminder
        reminderService.CheckReminders();
        Assert.Empty(reminderService.ActiveReminders);
    }

    [Fact]
    public async Task CheckReminders_WhenTaskFarInFuture_DoesNotFireReminder()
    {
        var todoRepo = new MemoryTodoRepository();
        var todoService = new TodoService(todoRepo);
        var calRepo = new MemoryCalendarRepository();
        var calService = new CalendarService(calRepo);

        var reminderService = new ReminderService(todoService, calService);

        var farFuture = DateTimeOffset.Now.AddDays(2);
        await todoService.AddItemAsync("Future review", TodoPriority.Low, farFuture);

        reminderService.CheckReminders();
        Assert.Empty(reminderService.ActiveReminders);
    }
}
