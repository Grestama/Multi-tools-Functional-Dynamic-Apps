using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class MemoryCalendarRepository : ICalendarRepository
{
    private List<CalendarEvent> _events = new();

    public Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(_events.ToList());
    }

    public Task SaveAllAsync(IReadOnlyList<CalendarEvent> events, CancellationToken cancellationToken = default)
    {
        _events = events.ToList();
        return Task.CompletedTask;
    }
}

public class CalendarServiceTests
{
    [Fact]
    public async Task AddEventAsync_ValidEvent_AddsToEventsAndGlance()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        bool glanceChanged = false;
        service.GlanceChanged += (s, e) => glanceChanged = true;

        var start = DateTimeOffset.Now.AddDays(1);
        var end = start.AddHours(1);

        var evt = await service.AddEventAsync("Sprint Planning", start, end, "Room 101", "Sprint review and roadmap");

        Assert.True(glanceChanged);
        Assert.Single(service.Events);
        Assert.Equal("Sprint Planning", service.Events[0].Title);
        Assert.Equal("Room 101", service.Events[0].Location);
        Assert.Equal("Sprint review and roadmap", service.Events[0].Description);
        Assert.False(service.Events[0].IsAllDay);
        Assert.Single(service.Glance.UpcomingEvents);
    }

    [Fact]
    public async Task AddEventAsync_TodayEvent_CalculatesCountdown()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        var start = DateTimeOffset.Now.AddMinutes(35);
        var end = start.AddMinutes(30);

        await service.AddEventAsync("Team Standup", start, end);

        Assert.Single(service.Glance.TodayEvents);
        Assert.Contains("In 3", service.Glance.NextEventCountdown);
        Assert.Contains("Team Standup", service.Glance.NextEventCountdown);
    }

    [Fact]
    public async Task DeleteEventAsync_ExistingId_RemovesEventAndUpdatesGlance()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        var start = DateTimeOffset.Now.AddHours(2);
        var evt = await service.AddEventAsync("Design Sync", start, start.AddHours(1));

        Assert.Single(service.Events);

        bool deleted = await service.DeleteEventAsync(evt.Id);

        Assert.True(deleted);
        Assert.Empty(service.Events);
        Assert.Equal("No upcoming events", service.Glance.NextEventCountdown);
    }

    [Fact]
    public async Task ExportToIcs_ContainsStandardRfc5545HeadersAndVEvent()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        var start = new DateTimeOffset(2026, 10, 25, 14, 0, 0, TimeSpan.Zero);
        await service.AddEventAsync("Architecture Review", start, start.AddHours(2), "Meeting Room B");

        string ics = service.ExportToIcs();

        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.Contains("VERSION:2.0", ics);
        Assert.Contains("BEGIN:VEVENT", ics);
        Assert.Contains("SUMMARY:Architecture Review", ics);
        Assert.Contains("LOCATION:Meeting Room B", ics);
        Assert.Contains("DTSTART:20261025T140000Z", ics);
        Assert.Contains("DTEND:20261025T160000Z", ics);
        Assert.Contains("END:VEVENT", ics);
        Assert.Contains("END:VCALENDAR", ics);
    }

    [Fact]
    public async Task ImportIcsAsync_ValidIcsString_ParsesAndAddsEvents()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        string sampleIcs = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Test//Calendar//EN
BEGIN:VEVENT
UID:event-12345
SUMMARY:Sprint Retrospective
LOCATION:Zoom
DESCRIPTION:Weekly team retro
DTSTART:20261101T090000Z
DTEND:20261101T100000Z
END:VEVENT
BEGIN:VEVENT
UID:event-67890
SUMMARY:Company All-Hands
DTSTART;VALUE=DATE:20261102
DTEND;VALUE=DATE:20261102
END:VEVENT
END:VCALENDAR";

        int importedCount = await service.ImportIcsAsync(sampleIcs);

        Assert.Equal(2, importedCount);
        Assert.Equal(2, service.Events.Count);

        var retro = service.Events.FirstOrDefault(e => e.Title == "Sprint Retrospective");
        Assert.NotNull(retro);
        Assert.Equal("Zoom", retro.Location);
        Assert.Equal("Weekly team retro", retro.Description);
        Assert.False(retro.IsAllDay);

        var allHands = service.Events.FirstOrDefault(e => e.Title == "Company All-Hands");
        Assert.NotNull(allHands);
        Assert.True(allHands.IsAllDay);
    }

    [Fact]
    public async Task ImportIcsAsync_DuplicateEvents_SuppressesDuplicates()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        string sampleIcs = @"BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
UID:evt-1
SUMMARY:One-on-One
DTSTART:20261105T150000Z
DTEND:20261105T153000Z
END:VEVENT
END:VCALENDAR";

        int first = await service.ImportIcsAsync(sampleIcs);
        int second = await service.ImportIcsAsync(sampleIcs);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Single(service.Events);
    }

    [Fact]
    public async Task Glance_OngoingEvent_IndicatesNow()
    {
        var repo = new MemoryCalendarRepository();
        var service = new CalendarService(repo);

        var start = DateTimeOffset.Now.AddMinutes(-10);
        var end = DateTimeOffset.Now.AddMinutes(20);

        await service.AddEventAsync("Active Workshop", start, end);

        Assert.Contains("Now: Active Workshop", service.Glance.NextEventCountdown);
    }
}
