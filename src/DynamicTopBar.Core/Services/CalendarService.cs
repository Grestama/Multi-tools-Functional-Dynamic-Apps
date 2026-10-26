using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service coordinating calendar events, iCalendar (ICS) import/export, and schedule glances.
/// </summary>
public sealed class CalendarService : ICalendarService
{
    private readonly ICalendarRepository _repository;
    private readonly object _lock = new();
    private readonly List<CalendarEvent> _events = new();
    private CalendarGlance _glance = CalendarGlance.Empty;
    private bool _isDisposed;

    public IReadOnlyList<CalendarEvent> Events
    {
        get { lock (_lock) return _events.OrderBy(e => e.StartTime).ToList(); }
    }

    public CalendarGlance Glance
    {
        get { lock (_lock) return _glance; }
        private set
        {
            lock (_lock) _glance = value;
            GlanceChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<CalendarGlance>? GlanceChanged;

    public CalendarService(ICalendarRepository? repository = null)
    {
        _repository = repository ?? new FileCalendarRepository();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;
        var loaded = await _repository.GetAllAsync(cancellationToken);
        lock (_lock)
        {
            _events.Clear();
            _events.AddRange(loaded);
        }
        RecalculateGlance();
    }

    public async Task<CalendarEvent> AddEventAsync(
        string title,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string? location = null,
        string? description = null,
        bool isAllDay = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (endTime < startTime)
        {
            endTime = startTime.AddHours(1);
        }

        var newEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            StartTime = startTime,
            EndTime = endTime,
            Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsAllDay = isAllDay,
            Source = "Local"
        };

        lock (_lock)
        {
            _events.Add(newEvent);
        }

        await SaveAsync(cancellationToken);
        RecalculateGlance();
        return newEvent;
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        bool removed;
        lock (_lock)
        {
            removed = _events.RemoveAll(e => e.Id == id) > 0;
        }

        if (removed)
        {
            await SaveAsync(cancellationToken);
            RecalculateGlance();
        }

        return removed;
    }

    public async Task<int> ImportIcsAsync(string icsContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icsContent)) return 0;

        var imported = ParseIcs(icsContent);
        if (imported.Count == 0) return 0;

        lock (_lock)
        {
            foreach (var evt in imported)
            {
                // Prevent duplicate event import with identical title & start time
                if (!_events.Any(e => e.Title.Equals(evt.Title, StringComparison.OrdinalIgnoreCase) && e.StartTime == evt.StartTime))
                {
                    _events.Add(evt);
                }
            }
        }

        await SaveAsync(cancellationToken);
        RecalculateGlance();
        return imported.Count;
    }

    public string ExportToIcs()
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//DynamicTopBar//Desktop Companion//EN");

        List<CalendarEvent> snapshot;
        lock (_lock)
        {
            snapshot = _events.ToList();
        }

        foreach (var evt in snapshot)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{evt.Id}");
            sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
            sb.AppendLine($"SUMMARY:{evt.Title}");

            if (evt.IsAllDay)
            {
                sb.AppendLine($"DTSTART;VALUE=DATE:{evt.StartTime:yyyyMMdd}");
                sb.AppendLine($"DTEND;VALUE=DATE:{evt.EndTime:yyyyMMdd}");
            }
            else
            {
                sb.AppendLine($"DTSTART:{evt.StartTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
                sb.AppendLine($"DTEND:{evt.EndTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
            }

            if (!string.IsNullOrEmpty(evt.Location))
            {
                sb.AppendLine($"LOCATION:{evt.Location}");
            }
            if (!string.IsNullOrEmpty(evt.Description))
            {
                sb.AppendLine($"DESCRIPTION:{evt.Description}");
            }

            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        List<CalendarEvent> snapshot;
        lock (_lock)
        {
            snapshot = _events.ToList();
        }
        await _repository.SaveAllAsync(snapshot, cancellationToken);
    }

    private void RecalculateGlance()
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);

        List<CalendarEvent> all;
        lock (_lock)
        {
            all = _events.OrderBy(e => e.StartTime).ToList();
        }

        var todayEvents = all.Where(e => DateOnly.FromDateTime(e.StartTime.LocalDateTime) == today).ToList();
        var upcomingEvents = all.Where(e => e.EndTime >= now).Take(10).ToList();

        var next = all.FirstOrDefault(e => e.EndTime >= now);
        string countdown;

        if (next == null)
        {
            countdown = "No upcoming events";
        }
        else if (next.IsOngoing)
        {
            countdown = $"Now: {next.Title}";
        }
        else
        {
            var diff = next.StartTime - now;
            if (diff.TotalMinutes < 60)
            {
                countdown = $"In {(int)diff.TotalMinutes}m: {next.Title}";
            }
            else if (diff.TotalHours < 24)
            {
                countdown = $"In {(int)diff.TotalHours}h {diff.Minutes}m: {next.Title}";
            }
            else
            {
                countdown = $"{next.StartTime.LocalDateTime:MMM d}: {next.Title}";
            }
        }

        Glance = new CalendarGlance
        {
            TodayEvents = todayEvents,
            UpcomingEvents = upcomingEvents,
            NextEvent = next,
            NextEventCountdown = countdown
        };
    }

    public static List<CalendarEvent> ParseIcs(string ics)
    {
        var result = new List<CalendarEvent>();
        var lines = ics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        bool inEvent = false;
        string? title = null;
        string? location = null;
        string? desc = null;
        DateTimeOffset start = DateTimeOffset.Now;
        DateTimeOffset end = DateTimeOffset.Now.AddHours(1);
        bool isAllDay = false;

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.Equals("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inEvent = true;
                title = null;
                location = null;
                desc = null;
                start = DateTimeOffset.Now;
                end = DateTimeOffset.Now.AddHours(1);
                isAllDay = false;
                continue;
            }

            if (!inEvent) continue;

            if (line.Equals("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inEvent = false;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    result.Add(new CalendarEvent
                    {
                        Id = Guid.NewGuid(),
                        Title = title,
                        Location = location,
                        Description = desc,
                        StartTime = start,
                        EndTime = end,
                        IsAllDay = isAllDay,
                        Source = "ICS Import"
                    });
                }
                continue;
            }

            int colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) continue;

            string key = line.Substring(0, colonIdx);
            string val = line.Substring(colonIdx + 1);

            if (key.Equals("SUMMARY", StringComparison.OrdinalIgnoreCase))
            {
                title = val;
            }
            else if (key.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
            {
                if (key.Contains("VALUE=DATE", StringComparison.OrdinalIgnoreCase))
                {
                    isAllDay = true;
                    if (DateOnly.TryParseExact(val, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    {
                        start = new DateTimeOffset(d.Year, d.Month, d.Day, 0, 0, 0, TimeSpan.Zero);
                    }
                }
                else if (TryParseIcsDateTime(val, out var parsedStart))
                {
                    start = parsedStart;
                }
            }
            else if (key.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
            {
                if (key.Contains("VALUE=DATE", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateOnly.TryParseExact(val, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    {
                        end = new DateTimeOffset(d.Year, d.Month, d.Day, 23, 59, 59, TimeSpan.Zero);
                    }
                }
                else if (TryParseIcsDateTime(val, out var parsedEnd))
                {
                    end = parsedEnd;
                }
            }
            else if (key.Equals("LOCATION", StringComparison.OrdinalIgnoreCase))
            {
                location = val;
            }
            else if (key.Equals("DESCRIPTION", StringComparison.OrdinalIgnoreCase))
            {
                desc = val;
            }
        }

        return result;
    }

    private static bool TryParseIcsDateTime(string val, out DateTimeOffset result)
    {
        string[] formats =
        {
            "yyyyMMddTHHmmssZ",
            "yyyyMMddTHHmmss",
            "yyyyMMdd"
        };

        if (DateTimeOffset.TryParseExact(val, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
        {
            return true;
        }

        return DateTimeOffset.TryParse(val, out result);
    }

    public void Dispose()
    {
        _isDisposed = true;
    }
}
