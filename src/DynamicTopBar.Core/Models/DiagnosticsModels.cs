using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Status and latency check for an internal or external subsystem.
/// </summary>
public record ComponentHealthCheck
{
    public required string ComponentName { get; init; }
    public HealthStatus Status { get; init; } = HealthStatus.Healthy;
    public string Message { get; init; } = "Operational";
    public double LatencyMs { get; init; }
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Telemetry record capturing an execution failure of a registered tool.
/// </summary>
public record ToolFailureRecord
{
    public required string ToolId { get; init; }
    public required string ErrorMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Snapshot of managed memory, thread allocation, and process uptime.
/// </summary>
public record ResourceMetrics
{
    public long ManagedMemoryBytes { get; init; }
    public int ThreadCount { get; init; }
    public TimeSpan ProcessUptime { get; init; }
}

/// <summary>
/// Comprehensive system report for diagnostics and issue reporting.
/// </summary>
public record SystemDiagnosticReport
{
    public string AppVersion { get; init; } = "1.0.0";
    public string DotNetVersion { get; init; } = Environment.Version.ToString();
    public string OsDescription { get; init; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string ProcessArchitecture { get; init; } = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public ResourceMetrics ResourceUsage { get; init; } = new();
    public IReadOnlyList<ComponentHealthCheck> HealthChecks { get; init; } = Array.Empty<ComponentHealthCheck>();
    public IReadOnlyList<ToolFailureRecord> RecentToolFailures { get; init; } = Array.Empty<ToolFailureRecord>();
    public IReadOnlyList<string> RecentLogs { get; init; } = Array.Empty<string>();
}
