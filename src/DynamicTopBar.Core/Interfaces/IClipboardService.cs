using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Platform adapter interface for interacting with the OS clipboard.
/// </summary>
public interface IClipboardPlatformAdapter
{
    Task<string?> GetTextAsync();
    Task SetTextAsync(string text);
}

/// <summary>
/// Service contract managing opt-in clipboard history, pause/resume, categorization, and local persistence.
/// </summary>
public interface IClipboardService : IDisposable
{
    IReadOnlyList<ClipboardSnippet> Snippets { get; }
    ClipboardState State { get; }
    ClipboardPreferences Preferences { get; }

    event EventHandler<ClipboardState>? StateChanged;
    event EventHandler<IReadOnlyList<ClipboardSnippet>>? SnippetsChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    void SetOptIn(bool enabled);
    void PauseMonitoring();
    void ResumeMonitoring();
    void AddSnippet(string text);
    void AddSnippet(string text, string? sourceAppName);
    bool DeleteSnippet(Guid id);
    void TogglePin(Guid id);
    void ClearHistory();
    Task<bool> CopyToClipboardAsync(Guid id);

    // Advanced Clipboard capabilities
    IReadOnlyList<ClipboardSnippet> Search(string? query, ClipboardContentType? category = null);
    void UpdatePreferences(ClipboardPreferences preferences);
    bool ExportSnippets(string filePath);
    bool ImportSnippets(string filePath);
    void AutoPrune();
}
