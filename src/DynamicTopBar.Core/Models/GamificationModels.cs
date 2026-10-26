using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Status and details of a daily micro-quest.
/// </summary>
public sealed record DailyQuestItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int TargetCount { get; init; }
    public int CurrentCount { get; init; }
    public int RewardXp { get; init; }
    public bool IsCompleted => CurrentCount >= TargetCount;
}

/// <summary>
/// Descriptor for an achievement badge.
/// </summary>
public sealed record BadgeDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string IconKey { get; init; }
    public DateTimeOffset? UnlockedAt { get; init; }
    public bool IsUnlocked => UnlockedAt.HasValue;
}

/// <summary>
/// Definition of an unlockable cosmetic capsule theme.
/// </summary>
public sealed record CapsuleCosmeticTheme
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int RequiredLevel { get; init; }
    public string AccentColorHex { get; init; } = "#3B82F6";
    public string BorderBrushHex { get; init; } = "#33FFFFFF";
}

/// <summary>
/// Immutable snapshot of the user's gamification progression, level, streak, and daily quests.
/// </summary>
public sealed record UserProgressionSnapshot
{
    public int CurrentLevel { get; init; } = 1;
    public long TotalXp { get; init; } = 0;
    public long CurrentLevelXp { get; init; } = 0;
    public long NextLevelRequiredXp { get; init; } = 100;
    public double LevelProgressRatio => NextLevelRequiredXp > 0
        ? Math.Clamp((double)CurrentLevelXp / NextLevelRequiredXp, 0.0, 1.0)
        : 1.0;

    public int CurrentStreakDays { get; init; } = 0;
    public int StreakShieldCount { get; init; } = 0;
    public string? LastActivityDateString { get; init; }
    public bool IsTodayStreakCounted { get; init; } = false;
    public int TodayFocusSessions { get; init; } = 0;
    public int TodayTodosCompleted { get; init; } = 0;
    public int TodayToolsUsed { get; init; } = 0;
    public bool IsDailyCrownClaimed { get; init; } = false;

    public IReadOnlyList<DailyQuestItem> DailyQuests { get; init; } = Array.Empty<DailyQuestItem>();
    public IReadOnlyList<BadgeDescriptor> Badges { get; init; } = Array.Empty<BadgeDescriptor>();
    public string ActiveCosmeticThemeId { get; init; } = "default";
    public IReadOnlyList<string> UnlockedCosmetics { get; init; } = new[] { "default" };

    public static readonly UserProgressionSnapshot Default = new()
    {
        CurrentLevel = 1,
        TotalXp = 0,
        CurrentLevelXp = 0,
        NextLevelRequiredXp = 100,
        CurrentStreakDays = 0,
        StreakShieldCount = 0,
        ActiveCosmeticThemeId = "default",
        UnlockedCosmetics = new[] { "default" }
    };
}
