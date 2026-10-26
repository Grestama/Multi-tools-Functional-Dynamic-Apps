using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Thread-safe local file and in-memory logging service.
/// </summary>
public sealed class LoggingService : ILoggingService
{
    private const int MaxInMemoryLogEntries = 200;
    private readonly object _lock = new();
    private readonly string? _logFilePath;
    private readonly List<string> _recentLogs = new();
    private bool _isDisposed;

    public event EventHandler<string>? LogEntryAdded;

    public LoggingService(string? logFilePath = null)
    {
        if (logFilePath != null)
        {
            _logFilePath = logFilePath;
        }
        else
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = Path.Combine(appData, "DynamicTopBar", "logs");
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, "app.log");
            }
            catch
            {
                _logFilePath = null;
            }
        }
    }

    public void Log(LogLevel level, string message, Exception? exception = null, string? category = null)
    {
        if (_isDisposed) return;

        string catStr = string.IsNullOrWhiteSpace(category) ? "App" : category;
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string entry = $"[{timestamp}] [{level.ToString().ToUpperInvariant()}] [{catStr}] {message}";

        if (exception != null)
        {
            entry += $"\nException: {exception.GetType().FullName}: {exception.Message}\nStackTrace:\n{exception.StackTrace}";
        }

        lock (_lock)
        {
            _recentLogs.Add(entry);
            if (_recentLogs.Count > MaxInMemoryLogEntries)
            {
                _recentLogs.RemoveAt(0);
            }

            if (!string.IsNullOrEmpty(_logFilePath))
            {
                try
                {
                    string? dir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.AppendAllText(_logFilePath, entry + Environment.NewLine);
                }
                catch
                {
                    // Fail silently to avoid crashing during logging failures
                }
            }
        }

        LogEntryAdded?.Invoke(this, entry);
    }

    public void LogDebug(string message, string? category = null) =>
        Log(LogLevel.Debug, message, null, category);

    public void LogInfo(string message, string? category = null) =>
        Log(LogLevel.Info, message, null, category);

    public void LogWarning(string message, Exception? exception = null, string? category = null) =>
        Log(LogLevel.Warning, message, exception, category);

    public void LogError(string message, Exception? exception = null, string? category = null) =>
        Log(LogLevel.Error, message, exception, category);

    public IReadOnlyList<string> GetRecentLogs(int count = 100)
    {
        lock (_lock)
        {
            return _recentLogs.TakeLast(count).ToList();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }
    }
}
