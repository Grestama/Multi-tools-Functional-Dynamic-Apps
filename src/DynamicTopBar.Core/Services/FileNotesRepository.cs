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
/// File-based JSON persistence for local Quick Notes with atomic writes, concurrency control,
/// and optional DPAPI data protection with backward compatibility.
/// </summary>
public sealed class FileNotesRepository : INotesRepository
{
    private readonly string _filePath;
    private readonly IDataProtector _dataProtector;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileNotesRepository(string? filePath = null, IDataProtector? dataProtector = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "notes.json");
        _dataProtector = dataProtector ?? NoOpDataProtector.Instance;
    }

    public async Task<IReadOnlyList<NoteItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<NoteItem>();
            }

            string rawText = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return Array.Empty<NoteItem>();
            }

            string json = _dataProtector.Unprotect(rawText);
            var notes = JsonSerializer.Deserialize<List<NoteItem>>(json, _jsonOptions);
            return notes ?? (IReadOnlyList<NoteItem>)Array.Empty<NoteItem>();
        }
        catch
        {
            return Array.Empty<NoteItem>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAllAsync(IReadOnlyList<NoteItem> notes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notes);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            string? dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempPath = $"{_filePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                string json = JsonSerializer.Serialize(notes, _jsonOptions);
                string payload = _dataProtector.Protect(json);
                await File.WriteAllTextAsync(tempPath, payload, cancellationToken);
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
        finally
        {
            _semaphore.Release();
        }
    }
}
