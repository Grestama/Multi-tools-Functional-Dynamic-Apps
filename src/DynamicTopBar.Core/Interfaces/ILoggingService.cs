using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Interfaces;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// Platform-independent logging and diagnostic service contract.
/// </summary>
public interface ILoggingService : IDisposable
{
    void Log(LogLevel level, string message, Exception? exception = null, string? category = null);
    void LogDebug(string message, string? category = null);
    void LogInfo(string message, string? category = null);
    void LogWarning(string message, Exception? exception = null, string? category = null);
    void LogError(string message, Exception? exception = null, string? category = null);

    IReadOnlyList<string> GetRecentLogs(int count = 100);
    event EventHandler<string>? LogEntryAdded;
}
