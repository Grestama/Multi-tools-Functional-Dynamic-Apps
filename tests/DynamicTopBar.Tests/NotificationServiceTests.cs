using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void PostNotification_SetsCurrentActiveNotification_AndHistory()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);

        bool posted = service.PostNotification("Test Title", "Test Message", "Calendar");

        Assert.True(posted);
        Assert.NotNull(service.CurrentActiveNotification);
        Assert.Equal("Test Title", service.CurrentActiveNotification.Title);
        Assert.Equal("Test Message", service.CurrentActiveNotification.Message);
        Assert.Single(service.NotificationHistory);
    }

    [Fact]
    public void PostNotification_SuppressesDuplicate_WithinDeduplicationWindow()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);

        bool first = service.PostNotification("Meeting", "10:00 AM", "Outlook");
        bool duplicate = service.PostNotification("Meeting", "10:00 AM", "Outlook");

        Assert.True(first);
        Assert.False(duplicate);
        Assert.Single(service.NotificationHistory);
    }

    [Fact]
    public void PostNotification_SuppressesBlockedAppSources()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);
        service.UpdatePreferences(new NotificationPreferences
        {
            BlockedAppSources = new List<string> { "SpamApp" }
        });

        bool posted = service.PostNotification("Spam", "Click here", "SpamApp");

        Assert.False(posted);
        Assert.Null(service.CurrentActiveNotification);
        Assert.Empty(service.NotificationHistory);
    }

    [Fact]
    public void PrivacyMode_MasksMessage()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);
        service.UpdatePreferences(new NotificationPreferences { PrivacyMode = true });

        service.PostNotification("Confidential Title", "Secret message body", "MailApp");

        var active = service.CurrentActiveNotification;
        Assert.NotNull(active);
        Assert.Equal("Notification from MailApp", active.GetMaskedMessage(privacyMode: true));
    }

    [Fact]
    public void DismissActiveNotification_ClearsActive_AndAdvancesQueue()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);

        service.PostNotification("First", "Message 1", "App1");
        service.PostNotification("Second", "Message 2", "App2");

        Assert.Equal("First", service.CurrentActiveNotification?.Title);

        service.DismissActiveNotification();

        Assert.Equal("Second", service.CurrentActiveNotification?.Title);

        service.DismissActiveNotification();

        Assert.Null(service.CurrentActiveNotification);
    }

    [Fact]
    public void ClearHistory_EmptiesNotificationHistory()
    {
        var timerService = new TestTimerService();
        using var service = new NotificationService(timerService: timerService);

        service.PostNotification("Alert", "Message", "App");
        Assert.NotEmpty(service.NotificationHistory);

        service.ClearHistory();
        Assert.Empty(service.NotificationHistory);
    }
}
