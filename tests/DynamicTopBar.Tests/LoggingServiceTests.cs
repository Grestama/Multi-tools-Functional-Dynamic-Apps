using System;
using System.IO;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class LoggingServiceTests : IDisposable
{
    private readonly string _tempLogFile;

    public LoggingServiceTests()
    {
        _tempLogFile = Path.Combine(Path.GetTempPath(), $"dtb_test_log_{Guid.NewGuid():N}.log");
    }

    [Fact]
    public void LogInfo_AppendsFormattedEntry_AndRetainsInMemory()
    {
        using var loggingService = new LoggingService(_tempLogFile);
        loggingService.LogInfo("Application initialized", "Lifecycle");

        var recent = loggingService.GetRecentLogs();
        Assert.Single(recent);
        Assert.Contains("[INFO]", recent[0]);
        Assert.Contains("[Lifecycle]", recent[0]);
        Assert.Contains("Application initialized", recent[0]);

        string fileContent = File.ReadAllText(_tempLogFile);
        Assert.Contains("[INFO]", fileContent);
        Assert.Contains("Application initialized", fileContent);
    }

    [Fact]
    public void LogError_WithException_AppendsStackTrace()
    {
        using var loggingService = new LoggingService(_tempLogFile);
        var ex = new InvalidOperationException("Test exception failure");

        loggingService.LogError("Operation failed", ex, "TestCat");

        var recent = loggingService.GetRecentLogs();
        Assert.Single(recent);
        Assert.Contains("[ERROR]", recent[0]);
        Assert.Contains("Test exception failure", recent[0]);
        Assert.Contains("InvalidOperationException", recent[0]);
    }

    [Fact]
    public void LogEntryAdded_FiresOnNewLog()
    {
        using var loggingService = new LoggingService(_tempLogFile);
        string? receivedEntry = null;
        loggingService.LogEntryAdded += (s, entry) => receivedEntry = entry;

        loggingService.LogWarning("Warning condition triggered");

        Assert.NotNull(receivedEntry);
        Assert.Contains("[WARNING]", receivedEntry);
        Assert.Contains("Warning condition triggered", receivedEntry);
    }

    public void Dispose()
    {
        if (File.Exists(_tempLogFile))
        {
            try { File.Delete(_tempLogFile); } catch { }
        }
    }
}
