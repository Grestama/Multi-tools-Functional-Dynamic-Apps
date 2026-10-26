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
/// File-based JSON implementation of ITodoRepository with atomic writes, semaphore-based concurrency control, and corruption recovery.
/// </summary>
public sealed class FileTodoRepository : ITodoRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileTodoRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "todos.json");
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<TodoItem>();
            }

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<TodoItem>();
            }

            var items = JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOptions);
            return items?.AsReadOnly() ?? (IReadOnlyList<TodoItem>)Array.Empty<TodoItem>();
        }
        catch
        {
            // Recover safely from corrupted files
            return Array.Empty<TodoItem>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAllAsync(IEnumerable<TodoItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = $"{_filePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                string json = JsonSerializer.Serialize(items, _jsonOptions);
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
        finally
        {
            _semaphore.Release();
        }
    }
}
