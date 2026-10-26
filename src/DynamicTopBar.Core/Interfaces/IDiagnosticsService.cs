using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract providing application health checks, tool failure diagnostics, resource metrics,
/// and privacy-sanitized diagnostic exports.
/// </summary>
public interface IDiagnosticsService : IDisposable
{
    void RecordToolFailure(string toolId, string errorMessage);
    void RegisterHealthCheck(string componentName, Func<Task<ComponentHealthCheck>> check);
    Task<IReadOnlyList<ComponentHealthCheck>> RunHealthChecksAsync(CancellationToken cancellationToken = default);
    ResourceMetrics GetResourceMetrics();
    Task<SystemDiagnosticReport> GenerateReportAsync(CancellationToken cancellationToken = default);
    string ExportSanitizedReportJson(SystemDiagnosticReport report);
    string SanitizeSecrets(string rawText);
}
