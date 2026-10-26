using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class MockClipboardPlatformAdapter : IClipboardPlatformAdapter
{
    public string? LastText { get; private set; }

    public Task<string?> GetTextAsync()
    {
        return Task.FromResult(LastText);
    }

    public Task SetTextAsync(string text)
    {
        LastText = text;
        return Task.CompletedTask;
    }
}

public class ClipboardServiceTests : IDisposable
{
    private readonly string _tempFile;

    public ClipboardServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dtb_clip_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch { }
    }

    [Fact]
    public void AddSnippet_WhenOptInDisabled_DoesNotCaptureSnippets()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);

        Assert.False(service.State.IsOptInEnabled);
        Assert.False(service.State.IsMonitoring);

        service.AddSnippet("Confidential password or sensitive text");

        Assert.Empty(service.Snippets);
        Assert.Equal(0, service.State.SnippetCount);
    }

    [Fact]
    public void SetOptIn_EnablesMonitoringAndAllowsCapture()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);

        bool stateFired = false;
        bool snippetsFired = false;
        service.StateChanged += (s, e) => stateFired = true;
        service.SnippetsChanged += (s, e) => snippetsFired = true;

        service.SetOptIn(true);

        Assert.True(stateFired);
        Assert.True(service.State.IsOptInEnabled);
        Assert.True(service.State.IsMonitoring);

        service.AddSnippet("https://github.com/microsoft/WindowsAppSDK");

        Assert.True(snippetsFired);
        Assert.Single(service.Snippets);
        Assert.Equal("https://github.com/microsoft/WindowsAppSDK", service.Snippets[0].Text);
    }

    [Fact]
    public void PauseMonitoring_And_ResumeMonitoring_ControlsCapture()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);
        service.SetOptIn(true);

        service.PauseMonitoring();
        Assert.False(service.State.IsMonitoring);

        service.AddSnippet("Should be ignored while paused");
        Assert.Empty(service.Snippets);

        service.ResumeMonitoring();
        Assert.True(service.State.IsMonitoring);

        service.AddSnippet("Captured after resume");
        Assert.Single(service.Snippets);
        Assert.Equal("Captured after resume", service.Snippets[0].Text);
    }

    [Fact]
    public void AddSnippet_ConsecutiveOrDuplicate_AvoidsDuplicateEntries()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);
        service.SetOptIn(true);

        service.AddSnippet("Repeated text");
        service.AddSnippet("Repeated text");

        Assert.Single(service.Snippets);
    }

    [Fact]
    public void TogglePin_PinsSnippet_ProtectsFromClearHistory()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);
        service.SetOptIn(true);

        service.AddSnippet("Pinned Snippet");
        service.AddSnippet("Unpinned Snippet");

        Assert.Equal(2, service.Snippets.Count);

        var pinnedItem = service.Snippets.First(s => s.Text == "Pinned Snippet");
        service.TogglePin(pinnedItem.Id);

        service.ClearHistory();

        Assert.Single(service.Snippets);
        Assert.Equal("Pinned Snippet", service.Snippets[0].Text);
        Assert.True(service.Snippets[0].IsPinned);
    }

    [Fact]
    public void AddSnippet_ExceedingMaxCapacity_PrunesUnpinnedFirst()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);
        service.SetOptIn(true);

        for (int i = 1; i <= 28; i++)
        {
            service.AddSnippet($"Snippet Item #{i}");
        }

        Assert.True(service.Snippets.Count <= 25);
    }

    [Fact]
    public async Task CopyToClipboardAsync_CallsPlatformAdapterWithText()
    {
        var adapter = new MockClipboardPlatformAdapter();
        var service = new ClipboardService(adapter, _tempFile);
        service.SetOptIn(true);

        service.AddSnippet("Text to copy back to clipboard");
        var snippet = service.Snippets[0];

        bool copied = await service.CopyToClipboardAsync(snippet.Id);

        Assert.True(copied);
        Assert.Equal("Text to copy back to clipboard", adapter.LastText);
    }
}
