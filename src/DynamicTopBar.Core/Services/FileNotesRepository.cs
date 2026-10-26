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
/// File-based JSON persistence for local Quick Notes with atomic writes.
/// </summary>
public sealed class FileNotesRepository : INotesRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileNotesRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "notes.json");
    }

    public async Task<IReadOnlyList<NoteItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<NoteItem>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<NoteItem>();
            }

            var notes = JsonSerializer.Deserialize<List<NoteItem>>(json, _jsonOptions);
            return notes ?? (IReadOnlyList<NoteItem>)Array.Empty<NoteItem>();
        }
        catch
        {
            return Array.Empty<NoteItem>();
        }
    }

    public async Task SaveAllAsync(IReadOnlyList<NoteItem> notes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notes);

        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string tempPath = $"{_filePath}.tmp_{Guid.NewGuid():N}";
        try
        {
            string json = JsonSerializer.Serialize(notes, _jsonOptions);
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
