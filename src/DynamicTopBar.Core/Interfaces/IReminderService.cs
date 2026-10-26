using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing evaluation, firing, and dismissal of local reminders for due tasks and calendar events.
/// </summary>
public interface IReminderService : IDisposable
{
    IReadOnlyList<AppReminder> ActiveReminders { get; }

    event EventHandler<AppReminder>? ReminderFired;
    event EventHandler<IReadOnlyList<AppReminder>>? RemindersChanged;

    void CheckReminders();
    void DismissReminder(string id);
    void DismissAll();
}
