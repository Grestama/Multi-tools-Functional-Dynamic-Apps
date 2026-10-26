using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class GamificationEngineTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public GamificationEngineTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "DTB_GamifyTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "gamification_state.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch { }
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 282)]
    [InlineData(3, 519)]
    [InlineData(4, 800)]
    [InlineData(5, 1118)]
    public void CalculateXpRequiredForLevel_ReturnsCalibratedCurve(int level, long expectedXp)
    {
        long required = GamificationEngineService.CalculateXpRequiredForLevel(level);
        Assert.Equal(expectedXp, required);
    }

    [Fact]
    public async Task AddXpAsync_LevelsUp_AndUnlocksCosmetics()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        int levelUpEventCount = 0;
        int latestLevelAchieved = 0;
        engine.LevelUp += (s, lvl) =>
        {
            levelUpEventCount++;
            latestLevelAchieved = lvl;
        };

        // Level 1 requires 100 XP to reach Level 2
        await engine.AddXpAsync(150, "Completed tasks");

        Assert.Equal(2, engine.Snapshot.CurrentLevel);
        Assert.Equal(50, engine.Snapshot.CurrentLevelXp); // 150 - 100 = 50 rollover
        Assert.Equal(150, engine.Snapshot.TotalXp);
        Assert.Equal(1, levelUpEventCount);
        Assert.Equal(2, latestLevelAchieved);

        // Add more XP to reach Level 3 (Level 2 requires 282 XP)
        // 50 existing + 250 = 300 >= 282 -> reaches Level 3!
        await engine.AddXpAsync(250, "Deep work focus");
        Assert.Equal(3, engine.Snapshot.CurrentLevel);
        Assert.Contains("emerald", engine.Snapshot.UnlockedCosmetics);
    }

    [Fact]
    public async Task RecordFocusCompletedAsync_AwardsCalculatedXp_AndUpdatesDailyQuest()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        bool badgeFired = false;
        engine.BadgeUnlocked += (s, b) =>
        {
            if (b.Id == "first_focus") badgeFired = true;
        };

        // 25 minutes = (25 * 2) + 15 = 65 XP
        await engine.RecordFocusCompletedAsync(25);

        Assert.Equal(65, engine.Snapshot.TotalXp);
        Assert.Equal(1, engine.Snapshot.TodayFocusSessions);

        var focusQuest = engine.Snapshot.DailyQuests.FirstOrDefault(q => q.Id == "deep_focus");
        Assert.NotNull(focusQuest);
        Assert.True(focusQuest.IsCompleted);
        Assert.True(badgeFired);
    }

    [Fact]
    public async Task RecordTodoCompletedAsync_AwardsPriorityBasedXp_AndUpdatesTaskQuest()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        await engine.RecordTodoCompletedAsync(TodoPriority.High); // +25 XP
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal); // +10 XP

        Assert.Equal(35, engine.Snapshot.TotalXp);
        Assert.Equal(2, engine.Snapshot.TodayTodosCompleted);

        var taskQuest = engine.Snapshot.DailyQuests.FirstOrDefault(q => q.Id == "task_slayer");
        Assert.NotNull(taskQuest);
        Assert.Equal(2, taskQuest.CurrentCount);
        Assert.False(taskQuest.IsCompleted);

        // Complete 3rd task to finish quest
        await engine.RecordTodoCompletedAsync(TodoPriority.High); // +25 XP
        Assert.Equal(60, engine.Snapshot.TotalXp);

        taskQuest = engine.Snapshot.DailyQuests.FirstOrDefault(q => q.Id == "task_slayer");
        Assert.NotNull(taskQuest);
        Assert.True(taskQuest.IsCompleted);
    }

    [Fact]
    public async Task DailyCrown_CanOnlyBeClaimedWhenAllQuestsComplete()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        // Initially incomplete
        bool claimedEarly = await engine.ClaimDailyCrownAsync();
        Assert.False(claimedEarly);

        // Complete focus quest (1 needed)
        await engine.RecordFocusCompletedAsync(20);

        // Complete task quest (3 needed)
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);

        // Complete tool quest (1 needed)
        await engine.RecordToolUsedAsync("Base64 Encoder");

        Assert.True(engine.Snapshot.DailyQuests.All(q => q.IsCompleted));

        long xpBeforeCrown = engine.Snapshot.TotalXp;
        bool claimed = await engine.ClaimDailyCrownAsync();
        Assert.True(claimed);
        Assert.Equal(xpBeforeCrown + 50, engine.Snapshot.TotalXp);
        Assert.True(engine.Snapshot.IsDailyCrownClaimed);

        // Second claim attempt must fail
        bool secondClaim = await engine.ClaimDailyCrownAsync();
        Assert.False(secondClaim);
    }

    [Fact]
    public async Task StreaksAndShields_IncrementsStreak_AndPreservesStreakWithShield()
    {
        var repo = new FileGamificationRepository(_filePath);

        // Prepopulate snapshot with an active streak and 1 shield from yesterday
        string yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var initialSnapshot = new UserProgressionSnapshot
        {
            CurrentLevel = 5,
            TotalXp = 1200,
            CurrentStreakDays = 6,
            StreakShieldCount = 1,
            LastActivityDateString = yesterday,
            UnlockedCosmetics = new[] { "default", "emerald", "cyberpunk" }
        };
        await repo.SaveAsync(initialSnapshot);

        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        // Today user completes 1 focus session + 3 todos -> qualifies for 7th day streak!
        await engine.RecordFocusCompletedAsync(25);
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);
        await engine.RecordTodoCompletedAsync(TodoPriority.Normal);

        Assert.Equal(7, engine.Snapshot.CurrentStreakDays);
        // Hitting 7 days streak awards +1 streak shield (1 + 1 = 2)
        Assert.Equal(2, engine.Snapshot.StreakShieldCount);

        // Now simulate user missing 2 days (gap > 1 day)
        string threeDaysAgo = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd");
        var missedDaysSnapshot = engine.Snapshot with
        {
            LastActivityDateString = threeDaysAgo
        };
        await repo.SaveAsync(missedDaysSnapshot);

        using var engineAfterAbsence = new GamificationEngineService(repo);
        await engineAfterAbsence.InitializeAsync();

        // Perform activity today
        await engineAfterAbsence.RecordFocusCompletedAsync(15);

        // Because user had 2 shields, 1 shield was consumed to PRESERVE the 7-day streak!
        Assert.Equal(7, engineAfterAbsence.Snapshot.CurrentStreakDays);
        Assert.Equal(1, engineAfterAbsence.Snapshot.StreakShieldCount);
    }

    [Fact]
    public async Task SelectCosmeticTheme_OnlyAppliesIfUnlocked()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine = new GamificationEngineService(repo);
        await engine.InitializeAsync();

        // "solar" requires Level 15 (locked at Level 1)
        await engine.SelectCosmeticThemeAsync("solar");
        Assert.Equal("default", engine.Snapshot.ActiveCosmeticThemeId);

        // Add enough XP to reach Level 3 which unlocks "emerald"
        await engine.AddXpAsync(400, "Level up");
        Assert.True(engine.Snapshot.CurrentLevel >= 2);

        // Level 3 requirement
        await engine.AddXpAsync(300, "Level up further");
        Assert.True(engine.Snapshot.CurrentLevel >= 3);
        Assert.Contains("emerald", engine.Snapshot.UnlockedCosmetics);

        await engine.SelectCosmeticThemeAsync("emerald");
        Assert.Equal("emerald", engine.Snapshot.ActiveCosmeticThemeId);
    }

    [Fact]
    public async Task Persistence_SavesAndReloadsStateCleanly()
    {
        var repo = new FileGamificationRepository(_filePath);
        using var engine1 = new GamificationEngineService(repo);
        await engine1.InitializeAsync();

        await engine1.AddXpAsync(350, "Work");
        await engine1.RecordFocusCompletedAsync(30);

        long savedXp = engine1.Snapshot.TotalXp;
        int savedLevel = engine1.Snapshot.CurrentLevel;

        using var engine2 = new GamificationEngineService(repo);
        await engine2.InitializeAsync();

        Assert.Equal(savedXp, engine2.Snapshot.TotalXp);
        Assert.Equal(savedLevel, engine2.Snapshot.CurrentLevel);
        Assert.True(File.Exists(_filePath));
    }
}
