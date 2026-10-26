using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// File-based JSON persistence for local calendar events with atomic writes.
/// </summary>
public sealed class FileCalendarRepository : ICalendarRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileCalendarRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "calendar_events.json");
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<CalendarEvent>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<CalendarEvent>();
            }

            var events = JsonSerializer.Deserialize<List<CalendarEvent>>(json, _jsonOptions);
            return events ?? (IReadOnlyList<CalendarEvent>)Array.Empty<CalendarEvent>();
        }
        catch
        {
            return Array.Empty<CalendarEvent>();
        }
    }

    public async Task SaveAllAsync(IReadOnlyList<CalendarEvent> events, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = $"{_filePath}.tmp_{Guid.NewGuid():N}";
        try
        {
            string json = JsonSerializer.Serialize(events, _jsonOptions);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
        }
    }
}
