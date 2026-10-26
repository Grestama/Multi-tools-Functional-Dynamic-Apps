using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe local file persistence for gamification progression with atomic writes and corruption recovery.
/// </summary>
public sealed class FileGamificationRepository : IGamificationRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileGamificationRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "gamification_state.json");
    }

    public async Task<UserProgressionSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                return UserProgressionSnapshot.Default;
            }

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return UserProgressionSnapshot.Default;
            }

            var loaded = JsonSerializer.Deserialize<UserProgressionSnapshot>(json, _jsonOptions);
            return loaded ?? UserProgressionSnapshot.Default;
        }
        catch
        {
            return UserProgressionSnapshot.Default;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync(UserProgressionSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

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
                string json = JsonSerializer.Serialize(snapshot, _jsonOptions);
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
