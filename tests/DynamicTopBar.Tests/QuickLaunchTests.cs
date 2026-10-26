using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class TestProcessLauncher : IProcessLauncher
{
    public List<(string FileName, string? Arguments)> Launched { get; } = new();
    public bool ShouldSucceed { get; set; } = true;

    public bool Launch(string fileName, string? arguments = null)
    {
        Launched.Add((fileName, arguments));
        return ShouldSucceed;
    }
}

public class QuickLaunchTests : IDisposable
{
    private readonly string _tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dtb_quicklaunch_test_{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        try
        {
            if (System.IO.File.Exists(_tempFile))
            {
                System.IO.File.Delete(_tempFile);
            }
        }
        catch { }
    }

    private QuickLaunchService CreateService(IProcessLauncher? launcher = null)
    {
        return new QuickLaunchService(launcher, persistenceFilePath: _tempFile);
    }

    [Fact]
    public void DefaultItems_HasExpectedSixSystemShortcuts()
    {
        var service = CreateService();

        Assert.Equal(6, service.Items.Count);
        Assert.Contains(service.Items, i => i.Id == "explorer");
        Assert.Contains(service.Items, i => i.Id == "browser");
        Assert.Contains(service.Items, i => i.Id == "terminal");
        Assert.Contains(service.Items, i => i.Id == "calculator");
        Assert.Contains(service.Items, i => i.Id == "settings");
        Assert.Contains(service.Items, i => i.Id == "taskmgr");
    }

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("http://localhost:5000")]
    [InlineData("ms-settings:privacy")]
    public void ValidateTarget_AllowedUrlsAndSettings_Succeeds(string target)
    {
        var service = CreateService();
        var result = service.ValidateTarget(target, QuickLaunchTargetType.Url);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("javascript:alert('pwned')")]
    [InlineData("data:text/html,<html>")]
    [InlineData("ftp://unsafe.example.com")]
    public void ValidateTarget_ProhibitedUrlSchemes_Fails(string target)
    {
        var service = CreateService();
        var result = service.ValidateTarget(target, QuickLaunchTargetType.Url);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ValidateTarget_MalformedUrl_Fails()
    {
        var service = CreateService();
        var result = service.ValidateTarget("not a valid url ://", QuickLaunchTargetType.Url);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("calc.exe")]
    [InlineData("explorer.exe")]
    [InlineData("cmd.exe")]
    public void ValidateTarget_KnownSystemTools_Succeeds(string tool)
    {
        var service = CreateService();
        var result = service.ValidateTarget(tool, QuickLaunchTargetType.WindowsTool);

        Assert.True(result.Success);
    }

    [Fact]
    public void Launch_ValidItem_CallsProcessLauncher()
    {
        var launcher = new TestProcessLauncher();
        var service = CreateService(launcher);

        var result = service.Launch("browser");

        Assert.True(result.Success);
        Assert.Single(launcher.Launched);
        Assert.Equal("https://www.google.com", launcher.Launched[0].FileName);
    }

    [Fact]
    public void Launch_InvalidId_ReturnsFailure()
    {
        var launcher = new TestProcessLauncher();
        var service = CreateService(launcher);

        var result = service.Launch("unknown-id-12345");

        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Empty(launcher.Launched);
    }

    [Fact]
    public void AddItem_ValidCustomItem_AddsAndRaisesEvent()
    {
        var service = CreateService();
        bool eventFired = false;
        service.ItemsChanged += (s, e) => eventFired = true;

        var newItem = new QuickLaunchItem
        {
            Id = "custom-site",
            Title = "My Site",
            PathOrUri = "https://github.com",
            IconGlyph = "\uE774",
            Type = QuickLaunchTargetType.Url,
            IsCustom = true
        };

        var result = service.AddItem(newItem);

        Assert.True(result.Success);
        Assert.True(eventFired);
        Assert.Equal(7, service.Items.Count);
        Assert.Contains(service.Items, i => i.Id == "custom-site");
    }

    [Fact]
    public void AddItem_DuplicateId_Fails()
    {
        var service = CreateService();
        var duplicate = new QuickLaunchItem
        {
            Id = "explorer",
            Title = "Duplicate Files",
            PathOrUri = "explorer.exe",
            IconGlyph = "\uED25",
            Type = QuickLaunchTargetType.WindowsTool
        };

        var result = service.AddItem(duplicate);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public void RemoveItem_ExistingItem_RemovesAndRaisesEvent()
    {
        var service = CreateService();
        bool eventFired = false;
        service.ItemsChanged += (s, e) => eventFired = true;

        bool removed = service.RemoveItem("calculator");

        Assert.True(removed);
        Assert.True(eventFired);
        Assert.Equal(5, service.Items.Count);
        Assert.DoesNotContain(service.Items, i => i.Id == "calculator");
    }

    [Fact]
    public void Launch_SuccessfulLaunch_RecordsItemInRecentItems()
    {
        var launcher = new TestProcessLauncher { ShouldSucceed = true };
        var service = CreateService(launcher);

        var result = service.Launch("explorer");

        Assert.True(result.Success);
        Assert.Single(service.RecentItems);
        Assert.Equal("explorer", service.RecentItems[0].Id);
    }

    [Fact]
    public void ClearRecentHistory_ClearsAllRecentItems()
    {
        var launcher = new TestProcessLauncher { ShouldSucceed = true };
        var service = CreateService(launcher);

        service.Launch("explorer");
        service.Launch("browser");
        Assert.Equal(2, service.RecentItems.Count);

        service.ClearRecentHistory();

        Assert.Empty(service.RecentItems);
    }
}
