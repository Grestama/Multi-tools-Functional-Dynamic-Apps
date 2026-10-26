using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Subsystem health monitoring and diagnostic reporting service.
/// Collects service health, tool failure telemetry, and exports reports with automatic secret redaction.
/// </summary>
public sealed class DiagnosticsService : IDiagnosticsService
{
    private const int MaxFailureHistory = 50;

    private readonly object _lock = new();
    private readonly ILoggingService _logger;
    private readonly DateTimeOffset _processStartTime = DateTimeOffset.UtcNow;
    private readonly Dictionary<string, Func<Task<ComponentHealthCheck>>> _healthChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ToolFailureRecord> _failureHistory = new();
    private bool _isDisposed;

    // Secret sanitization patterns
    private static readonly Regex JwtRegex = new(@"ey[A-Za-z0-9_-]{10,}\.ey[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}", RegexOptions.Compiled);
    private static readonly Regex BearerRegex = new(@"(Bearer\s+)[A-Za-z0-9_\-\.]{15,}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PrivateKeyRegex = new(@"-----BEGIN [A-Z ]+ PRIVATE KEY-----[\s\S]+?-----END [A-Z ]+ PRIVATE KEY-----", RegexOptions.Compiled);
    private static readonly Regex PasswordKeyValueRegex = new(@"(""(?:password|secret|apikey|token|access_token|refresh_token)""\s*:\s*"")[^""]+("")", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d{4}[ -]?){3}\d{4}\b", RegexOptions.Compiled);

    public DiagnosticsService(ILoggingService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordToolFailure(string toolId, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(toolId)) return;

        lock (_lock)
        {
            _failureHistory.Insert(0, new ToolFailureRecord
            {
                ToolId = toolId,
                ErrorMessage = SanitizeSecrets(errorMessage ?? "Unknown failure"),
                Timestamp = DateTimeOffset.UtcNow
            });

            if (_failureHistory.Count > MaxFailureHistory)
            {
                _failureHistory.RemoveAt(_failureHistory.Count - 1);
            }
        }
    }

    public void RegisterHealthCheck(string componentName, Func<Task<ComponentHealthCheck>> check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (string.IsNullOrWhiteSpace(componentName)) return;

        lock (_lock)
        {
            _healthChecks[componentName] = check;
        }
    }

    public async Task<IReadOnlyList<ComponentHealthCheck>> RunHealthChecksAsync(CancellationToken cancellationToken = default)
    {
        List<KeyValuePair<string, Func<Task<ComponentHealthCheck>>>> checksCopy;
        lock (_lock) checksCopy = _healthChecks.ToList();

        var results = new List<ComponentHealthCheck>();

        foreach (var kvp in checksCopy)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sw = Stopwatch.StartNew();

            try
            {
                var check = await kvp.Value();
                sw.Stop();
                results.Add(check with { LatencyMs = sw.Elapsed.TotalMilliseconds });
            }
            catch (Exception ex)
            {
                sw.Stop();
                results.Add(new ComponentHealthCheck
                {
                    ComponentName = kvp.Key,
                    Status = HealthStatus.Unhealthy,
                    Message = $"Check failed: {ex.Message}",
                    LatencyMs = sw.Elapsed.TotalMilliseconds
                });
            }
        }

        return results;
    }

    public ResourceMetrics GetResourceMetrics()
    {
        long managedMem = GC.GetTotalMemory(false);
        int threadCount = Process.GetCurrentProcess().Threads.Count;
        TimeSpan uptime = DateTimeOffset.UtcNow - _processStartTime;

        return new ResourceMetrics
        {
            ManagedMemoryBytes = managedMem,
            ThreadCount = threadCount,
            ProcessUptime = uptime
        };
    }

    public async Task<SystemDiagnosticReport> GenerateReportAsync(CancellationToken cancellationToken = default)
    {
        var healthResults = await RunHealthChecksAsync(cancellationToken);
        var metrics = GetResourceMetrics();

        List<ToolFailureRecord> failures;
        lock (_lock) failures = _failureHistory.ToList();

        var recentLogs = _logger.GetRecentLogs(50)
            .Select(SanitizeSecrets)
            .ToList();

        return new SystemDiagnosticReport
        {
            AppVersion = "1.0.0",
            ResourceUsage = metrics,
            HealthChecks = healthResults,
            RecentToolFailures = failures,
            RecentLogs = recentLogs,
            GeneratedAt = DateTimeOffset.UtcNow
        };
    }

    public string ExportSanitizedReportJson(SystemDiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return SanitizeSecrets(json);
    }

    public string SanitizeSecrets(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return string.Empty;

        string sanitized = rawText;
        sanitized = PrivateKeyRegex.Replace(sanitized, "[REDACTED_PRIVATE_KEY]");
        sanitized = JwtRegex.Replace(sanitized, "[REDACTED_JWT_TOKEN]");
        sanitized = BearerRegex.Replace(sanitized, "$1[REDACTED_BEARER_TOKEN]");
        sanitized = PasswordKeyValueRegex.Replace(sanitized, "$1[REDACTED]$2");
        sanitized = CreditCardRegex.Replace(sanitized, "[REDACTED_CARD_NUMBER]");

        return sanitized;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
