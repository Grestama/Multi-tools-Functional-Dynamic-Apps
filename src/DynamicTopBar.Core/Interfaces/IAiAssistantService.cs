using System;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract providing local, privacy-preserving AI assistant intent planning,
/// parameter extraction, and mediated tool execution with strict permission gates.
/// </summary>
public interface IAiAssistantService : IDisposable
{
    bool IsEnabled { get; set; }
    bool LocalProcessingOnly { get; set; }

    Task<AiIntentPlan?> PlanIntentAsync(string naturalLanguageQuery, CancellationToken cancellationToken = default);

    Task<AiExecutionResult> ProcessQueryAsync(
        string naturalLanguageQuery,
        Func<AiIntentPlan, Task<bool>>? userConfirmationCallback = null,
        CancellationToken cancellationToken = default);
}
