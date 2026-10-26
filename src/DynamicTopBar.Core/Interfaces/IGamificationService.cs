using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service coordinating Octalysis-inspired gamification, XP progression, streaks, shields, daily quests, and badges.
/// </summary>
public interface IGamificationService : IDisposable
{
    event EventHandler<UserProgressionSnapshot>? ProgressionChanged;
    event EventHandler<BadgeDescriptor>? BadgeUnlocked;
    event EventHandler<int>? LevelUp;

    UserProgressionSnapshot Snapshot { get; }
    IReadOnlyList<CapsuleCosmeticTheme> AvailableThemes { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task AddXpAsync(long xp, string reason, CancellationToken cancellationToken = default);
    Task RecordFocusCompletedAsync(int durationMinutes, CancellationToken cancellationToken = default);
    Task RecordTodoCompletedAsync(TodoPriority priority, CancellationToken cancellationToken = default);
    Task RecordToolUsedAsync(string toolName, CancellationToken cancellationToken = default);
    Task<bool> ClaimDailyCrownAsync(CancellationToken cancellationToken = default);
    Task SelectCosmeticThemeAsync(string themeId, CancellationToken cancellationToken = default);
}
