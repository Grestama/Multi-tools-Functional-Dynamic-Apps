using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Core implementation of the Octalysis-inspired Gamification Engine.
/// Provides non-intrusive, privacy-respecting XP progression, streak preservation with shields,
/// daily micro-quests, badges, and cosmetic capsule themes.
/// </summary>
public sealed class GamificationEngineService : IGamificationService
{
    private readonly IGamificationRepository _repository;
    private readonly IFocusTimerService? _focusTimerService;
    private readonly ITodoService? _todoService;
    private readonly IDeveloperUtilitiesService? _devUtilsService;
    private readonly object _lock = new();
    private UserProgressionSnapshot _snapshot = UserProgressionSnapshot.Default;
    private bool _isDisposed;

    public event EventHandler<UserProgressionSnapshot>? ProgressionChanged;
    public event EventHandler<BadgeDescriptor>? BadgeUnlocked;
    public event EventHandler<int>? LevelUp;

    public UserProgressionSnapshot Snapshot
    {
        get
        {
            lock (_lock) return _snapshot;
        }
    }

    public static readonly IReadOnlyList<CapsuleCosmeticTheme> Themes = new List<CapsuleCosmeticTheme>
    {
        new() { Id = "default", Name = "Default Obsidian", Description = "Classic sleek dark pill aesthetic", RequiredLevel = 1, AccentColorHex = "#3B82F6", BorderBrushHex = "#33FFFFFF" },
        new() { Id = "emerald", Name = "Emerald Focus", Description = "Energizing calm green aura for deep work", RequiredLevel = 3, AccentColorHex = "#10B981", BorderBrushHex = "#4410B981" },
        new() { Id = "cyberpunk", Name = "Cyberpunk Neon", Description = "Vibrant electric magenta and cyan glow", RequiredLevel = 5, AccentColorHex = "#EC4899", BorderBrushHex = "#55EC4899" },
        new() { Id = "slate", Name = "Minimalist Slate", Description = "Understated matte graphite executive finish", RequiredLevel = 10, AccentColorHex = "#94A3B8", BorderBrushHex = "#4494A3B8" },
        new() { Id = "solar", Name = "Solar Flare", Description = "Radiant golden amber productivity energy", RequiredLevel = 15, AccentColorHex = "#F59E0B", BorderBrushHex = "#55F59E0B" }
    };

    public IReadOnlyList<CapsuleCosmeticTheme> AvailableThemes => Themes;

    public GamificationEngineService(
        IGamificationRepository? repository = null,
        IFocusTimerService? focusTimerService = null,
        ITodoService? todoService = null,
        IDeveloperUtilitiesService? devUtilsService = null)
    {
        _repository = repository ?? new FileGamificationRepository();
        _focusTimerService = focusTimerService;
        _todoService = todoService;
        _devUtilsService = devUtilsService;

        if (_focusTimerService != null)
        {
            _focusTimerService.SnapshotChanged += OnFocusTimerSnapshotChanged;
        }

        if (_todoService != null)
        {
            _todoService.ItemsChanged += OnTodoItemsChanged;
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        var loaded = await _repository.LoadAsync(cancellationToken);
        lock (_lock)
        {
            _snapshot = EnsureValidQuestsAndBadges(loaded);
        }

        NotifyChanged();
    }

    public static long CalculateXpRequiredForLevel(int level)
    {
        if (level < 1) level = 1;
        return (long)(100 * Math.Pow(level, 1.5));
    }

    public async Task AddXpAsync(long xp, string reason, CancellationToken cancellationToken = default)
    {
        if (xp <= 0 || _isDisposed) return;

        UserProgressionSnapshot updated;
        int newLevelAchieved = 0;

        lock (_lock)
        {
            long total = _snapshot.TotalXp + xp;
            int level = _snapshot.CurrentLevel;
            long currentLevelXp = _snapshot.CurrentLevelXp + xp;
            long required = _snapshot.NextLevelRequiredXp;

            while (currentLevelXp >= required)
            {
                currentLevelXp -= required;
                level++;
                required = CalculateXpRequiredForLevel(level);
                newLevelAchieved = level;
            }

            var unlockedCosmetics = new HashSet<string>(_snapshot.UnlockedCosmetics, StringComparer.OrdinalIgnoreCase);
            foreach (var theme in Themes)
            {
                if (level >= theme.RequiredLevel)
                {
                    unlockedCosmetics.Add(theme.Id);
                }
            }

            updated = _snapshot with
            {
                TotalXp = total,
                CurrentLevel = level,
                CurrentLevelXp = currentLevelXp,
                NextLevelRequiredXp = required,
                UnlockedCosmetics = unlockedCosmetics.ToList()
            };

            _snapshot = updated;
        }

        await _repository.SaveAsync(updated, cancellationToken);
        NotifyChanged();

        if (newLevelAchieved > 0)
        {
            LevelUp?.Invoke(this, newLevelAchieved);
        }
    }

    public async Task RecordFocusCompletedAsync(int durationMinutes, CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        // Base reward: 2 XP per minute + 15 XP session completion bonus
        long earnedXp = Math.Max(1, (durationMinutes * 2) + 15);

        lock (_lock)
        {
            _snapshot = EnsureDayBoundary(_snapshot);
            _snapshot = _snapshot with { TodayFocusSessions = _snapshot.TodayFocusSessions + 1 };
            _snapshot = UpdateQuestProgress(_snapshot, "deep_focus", 1);
            _snapshot = CheckAndApplyStreakCriteria(_snapshot);
            _snapshot = CheckBadges(_snapshot);
        }

        await AddXpAsync(earnedXp, $"Focus session completed ({durationMinutes}m)", cancellationToken);
    }

    public async Task RecordTodoCompletedAsync(TodoPriority priority, CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        long earnedXp = priority switch
        {
            TodoPriority.High => 25,
            TodoPriority.Normal => 10,
            _ => 10
        };

        lock (_lock)
        {
            _snapshot = EnsureDayBoundary(_snapshot);
            _snapshot = _snapshot with { TodayTodosCompleted = _snapshot.TodayTodosCompleted + 1 };
            _snapshot = UpdateQuestProgress(_snapshot, "task_slayer", 1);
            _snapshot = CheckAndApplyStreakCriteria(_snapshot);
            _snapshot = CheckBadges(_snapshot);
        }

        await AddXpAsync(earnedXp, $"Task completed ({priority})", cancellationToken);
    }

    public async Task RecordToolUsedAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            _snapshot = EnsureDayBoundary(_snapshot);
            _snapshot = _snapshot with { TodayToolsUsed = _snapshot.TodayToolsUsed + 1 };
            _snapshot = UpdateQuestProgress(_snapshot, "power_user", 1);
            _snapshot = CheckBadges(_snapshot);
        }

        await AddXpAsync(5, $"Tool used: {toolName}", cancellationToken);
    }

    public async Task<bool> ClaimDailyCrownAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return false;

        bool canClaim = false;
        lock (_lock)
        {
            if (!_snapshot.IsDailyCrownClaimed && _snapshot.DailyQuests.Count > 0 && _snapshot.DailyQuests.All(q => q.IsCompleted))
            {
                canClaim = true;
                _snapshot = _snapshot with { IsDailyCrownClaimed = true };
            }
        }

        if (canClaim)
        {
            await AddXpAsync(50, "Daily Crown Achieved", cancellationToken);
            return true;
        }

        return false;
    }

    public async Task SelectCosmeticThemeAsync(string themeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(themeId) || _isDisposed) return;

        UserProgressionSnapshot updated;
        lock (_lock)
        {
            if (!_snapshot.UnlockedCosmetics.Contains(themeId, StringComparer.OrdinalIgnoreCase))
            {
                return; // Not unlocked yet
            }

            updated = _snapshot with { ActiveCosmeticThemeId = themeId };
            _snapshot = updated;
        }

        await _repository.SaveAsync(updated, cancellationToken);
        NotifyChanged();
    }

    private UserProgressionSnapshot EnsureDayBoundary(UserProgressionSnapshot snapshot)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (snapshot.LastActivityDateString == today)
        {
            return snapshot;
        }

        // New day detected
        int streak = snapshot.CurrentStreakDays;
        int shields = snapshot.StreakShieldCount;

        if (!string.IsNullOrEmpty(snapshot.LastActivityDateString))
        {
            if (DateTime.TryParse(snapshot.LastActivityDateString, out var lastDate))
            {
                var daysDiff = (DateTime.UtcNow.Date - lastDate.Date).TotalDays;
                if (daysDiff > 1)
                {
                    // Missed one or more days!
                    if (shields > 0)
                    {
                        // Consume 1 shield and preserve streak
                        shields--;
                    }
                    else
                    {
                        // Streak lost
                        streak = 0;
                    }
                }
            }
        }

        return snapshot with
        {
            LastActivityDateString = today,
            IsTodayStreakCounted = false,
            TodayFocusSessions = 0,
            TodayTodosCompleted = 0,
            TodayToolsUsed = 0,
            IsDailyCrownClaimed = false,
            CurrentStreakDays = streak,
            StreakShieldCount = shields,
            DailyQuests = CreateDailyQuests()
        };
    }

    private UserProgressionSnapshot CheckAndApplyStreakCriteria(UserProgressionSnapshot snapshot)
    {
        // Daily streak increment threshold: at least 1 focus session AND at least 3 todos
        if (!snapshot.IsTodayStreakCounted && snapshot.TodayFocusSessions >= 1 && snapshot.TodayTodosCompleted >= 3)
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            int newStreak = snapshot.CurrentStreakDays + 1;
            int shields = snapshot.StreakShieldCount;

            // Award +1 Streak Shield every 7 consecutive days (capped at 3)
            if (newStreak % 7 == 0 && shields < 3)
            {
                shields++;
            }

            snapshot = snapshot with
            {
                CurrentStreakDays = newStreak,
                StreakShieldCount = shields,
                IsTodayStreakCounted = true,
                LastActivityDateString = today
            };
        }

        return snapshot;
    }

    private static UserProgressionSnapshot UpdateQuestProgress(UserProgressionSnapshot snapshot, string questId, int increment)
    {
        var updatedQuests = snapshot.DailyQuests.Select(q =>
        {
            if (string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase))
            {
                return q with { CurrentCount = Math.Min(q.TargetCount, q.CurrentCount + increment) };
            }
            return q;
        }).ToList();

        return snapshot with { DailyQuests = updatedQuests };
    }

    private UserProgressionSnapshot CheckBadges(UserProgressionSnapshot snapshot)
    {
        var badges = snapshot.Badges.ToList();
        bool anyUnlocked = false;

        void UnlockIf(string id, bool condition)
        {
            int idx = badges.FindIndex(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && !badges[idx].IsUnlocked && condition)
            {
                badges[idx] = badges[idx] with { UnlockedAt = DateTimeOffset.UtcNow };
                anyUnlocked = true;
                BadgeUnlocked?.Invoke(this, badges[idx]);
            }
        }

        UnlockIf("first_focus", snapshot.TodayFocusSessions >= 1);
        UnlockIf("task_master", snapshot.TodayTodosCompleted >= 20 || (snapshot.TodayTodosCompleted >= 3 && snapshot.CurrentStreakDays >= 3));
        UnlockIf("streak_master", snapshot.CurrentStreakDays >= 7);
        UnlockIf("code_alchemist", snapshot.TodayToolsUsed >= 5);
        UnlockIf("workflow_automator", snapshot.TodayToolsUsed >= 10);

        if (anyUnlocked)
        {
            return snapshot with { Badges = badges };
        }

        return snapshot;
    }

    private static UserProgressionSnapshot EnsureValidQuestsAndBadges(UserProgressionSnapshot snapshot)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var quests = snapshot.DailyQuests;
        if (quests == null || quests.Count == 0 || snapshot.LastActivityDateString != today)
        {
            quests = CreateDailyQuests();
        }

        var badges = DefaultBadges.Select(def =>
        {
            var existing = snapshot.Badges?.FirstOrDefault(b => string.Equals(b.Id, def.Id, StringComparison.OrdinalIgnoreCase));
            return existing ?? def;
        }).ToList();

        return snapshot with
        {
            DailyQuests = quests,
            Badges = badges,
            NextLevelRequiredXp = CalculateXpRequiredForLevel(snapshot.CurrentLevel)
        };
    }

    private static IReadOnlyList<DailyQuestItem> CreateDailyQuests() => new List<DailyQuestItem>
    {
        new() { Id = "deep_focus", Title = "Deep Focus", Description = "Complete 1 focused work session", TargetCount = 1, CurrentCount = 0, RewardXp = 30 },
        new() { Id = "task_slayer", Title = "Task Slayer", Description = "Complete 3 to-do items", TargetCount = 3, CurrentCount = 0, RewardXp = 35 },
        new() { Id = "power_user", Title = "Power User", Description = "Use 1 developer or file tool", TargetCount = 1, CurrentCount = 0, RewardXp = 20 }
    };

    private static readonly IReadOnlyList<BadgeDescriptor> DefaultBadges = new List<BadgeDescriptor>
    {
        new() { Id = "first_focus", Title = "First Focus", Description = "Completed your first focus session", IconKey = "Timer" },
        new() { Id = "deep_worker", Title = "Deep Worker", Description = "Completed 50 hours of deep work", IconKey = "Clock" },
        new() { Id = "task_master", Title = "Task Master", Description = "Completed 20 to-do list tasks", IconKey = "Checkmark" },
        new() { Id = "streak_master", Title = "Streak Master", Description = "Maintained a 7-day productivity streak", IconKey = "Flame" },
        new() { Id = "workflow_automator", Title = "Workflow Automator", Description = "Executed 10 workflow or utility actions", IconKey = "Play" },
        new() { Id = "code_alchemist", Title = "Code Alchemist", Description = "Used Developer Utilities 15 times", IconKey = "Code" }
    };

    private int _lastCompletedFocusSessions;
    private void OnFocusTimerSnapshotChanged(object? sender, FocusTimerSnapshot e)
    {
        if (e.CompletedSessionsCount > _lastCompletedFocusSessions)
        {
            _lastCompletedFocusSessions = e.CompletedSessionsCount;
            _ = RecordFocusCompletedAsync((int)e.TotalDuration.TotalMinutes);
        }
    }

    private int _lastCompletedTodos;
    private void OnTodoItemsChanged(object? sender, IReadOnlyList<TodoItem> e)
    {
        int completedCount = e.Count(t => t.IsCompleted);
        if (completedCount > _lastCompletedTodos)
        {
            int diff = completedCount - _lastCompletedTodos;
            _lastCompletedTodos = completedCount;
            for (int i = 0; i < diff; i++)
            {
                _ = RecordTodoCompletedAsync(TodoPriority.Normal);
            }
        }
        else
        {
            _lastCompletedTodos = completedCount;
        }
    }

    private void NotifyChanged()
    {
        UserProgressionSnapshot copy;
        lock (_lock) copy = _snapshot;
        ProgressionChanged?.Invoke(this, copy);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_focusTimerService != null)
        {
            _focusTimerService.SnapshotChanged -= OnFocusTimerSnapshotChanged;
        }

        if (_todoService != null)
        {
            _todoService.ItemsChanged -= OnTodoItemsChanged;
        }
    }
}
