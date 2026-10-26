using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Service managing strictly opt-in clipboard history, categorization, auto-pruning,
/// sensitive data filtering, pause/resume, and local persistence.
/// Zero external telemetry or network communication.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private readonly string _persistencePath;
    private readonly string _prefsPersistencePath;
    private readonly IClipboardPlatformAdapter? _platformAdapter;
    private readonly IDataProtector _dataProtector;
    private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
    private readonly object _lock = new();
    private readonly List<ClipboardSnippet> _snippets = new();

    private ClipboardPreferences _preferences = new();
    private bool _isOptInEnabled;
    private bool _isMonitoring;
    private bool _isDisposed;

    public ClipboardPreferences Preferences
    {
        get
        {
            lock (_lock) return _preferences;
        }
    }

    public IReadOnlyList<ClipboardSnippet> Snippets
    {
        get
        {
            lock (_lock)
            {
                return _snippets
                    .OrderByDescending(s => s.IsPinned)
                    .ThenByDescending(s => s.Timestamp)
                    .ToList();
            }
        }
    }

    public ClipboardState State
    {
        get
        {
            lock (_lock)
            {
                string status;
                if (!_isOptInEnabled)
                {
                    status = "Disabled (Opt-in required)";
                }
                else if (!_isMonitoring)
                {
                    status = "Paused";
                }
                else
                {
                    status = $"Monitoring ({_snippets.Count} saved)";
                }

                return new ClipboardState
                {
                    IsOptInEnabled = _isOptInEnabled,
                    IsMonitoring = _isMonitoring,
                    SnippetCount = _snippets.Count,
                    StatusText = status
                };
            }
        }
    }

    public event EventHandler<ClipboardState>? StateChanged;
    public event EventHandler<IReadOnlyList<ClipboardSnippet>>? SnippetsChanged;

    public ClipboardService(IClipboardPlatformAdapter? platformAdapter = null, string? persistencePath = null, IDataProtector? dataProtector = null)
    {
        _platformAdapter = platformAdapter;
        _dataProtector = dataProtector ?? NoOpDataProtector.Instance;
        string basePath = persistencePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "clipboard_history.json");

        _persistencePath = basePath;
        _prefsPersistencePath = Path.Combine(
            Path.GetDirectoryName(basePath) ?? string.Empty,
            "clipboard_prefs.json");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed) return;

        LoadPreferences();

        if (File.Exists(_persistencePath))
        {
            await _fileSemaphore.WaitAsync(cancellationToken);
            try
            {
                string rawText = await File.ReadAllTextAsync(_persistencePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(rawText))
                {
                    string json = _dataProtector.Unprotect(rawText);
                    var loaded = JsonSerializer.Deserialize<List<ClipboardSnippet>>(json);
                    if (loaded != null)
                    {
                        lock (_lock)
                        {
                            _snippets.Clear();
                            _snippets.AddRange(loaded.Take(_preferences.MaxSnippetCapacity));
                        }
                    }
                }
            }
            catch
            {
                // Ignore corrupt clipboard cache
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        AutoPrune();
        NotifyStateChanged();
        NotifySnippetsChanged();
    }

    public void SetOptIn(bool enabled)
    {
        lock (_lock)
        {
            _isOptInEnabled = enabled;
            _isMonitoring = enabled; // Auto-start monitoring when user opts in
            if (!enabled)
            {
                _isMonitoring = false;
            }
        }

        NotifyStateChanged();
    }

    public void PauseMonitoring()
    {
        lock (_lock)
        {
            if (_isOptInEnabled)
            {
                _isMonitoring = false;
            }
        }
        NotifyStateChanged();
    }

    public void ResumeMonitoring()
    {
        lock (_lock)
        {
            if (_isOptInEnabled)
            {
                _isMonitoring = true;
            }
        }
        NotifyStateChanged();
    }

    public void AddSnippet(string text) => AddSnippet(text, sourceAppName: null);

    public void AddSnippet(string text, string? sourceAppName)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lock (_lock)
        {
            // Strict Privacy: Never capture if not opted in or currently paused
            if (!_isOptInEnabled || !_isMonitoring) return;

            // Application exclusion check
            if (!string.IsNullOrWhiteSpace(sourceAppName) &&
                _preferences.ExcludedApplications.Any(app => string.Equals(app, sourceAppName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            string trimmed = text.Trim();

            // Sensitive data pattern exclusion check
            if (_preferences.ExcludeSensitivePatterns && IsSensitivePattern(trimmed))
            {
                return;
            }

            // Ignore if identical to the latest snippet
            var latest = _snippets.OrderByDescending(s => s.Timestamp).FirstOrDefault();
            if (latest != null && string.Equals(latest.Text, trimmed, StringComparison.Ordinal))
            {
                return;
            }

            // Remove existing instance if already present to avoid duplication
            _snippets.RemoveAll(s => string.Equals(s.Text, trimmed, StringComparison.Ordinal));

            var snippet = new ClipboardSnippet
            {
                Id = Guid.NewGuid(),
                Text = trimmed,
                Timestamp = DateTimeOffset.UtcNow,
                IsPinned = false,
                ContentType = DetectContentType(trimmed),
                SourceAppName = sourceAppName
            };

            _snippets.Insert(0, snippet);

            // Prune unpinned items exceeding capacity
            while (_snippets.Count > _preferences.MaxSnippetCapacity)
            {
                int removeIdx = _snippets.FindLastIndex(s => !s.IsPinned);
                if (removeIdx >= 0)
                {
                    _snippets.RemoveAt(removeIdx);
                }
                else
                {
                    break;
                }
            }
        }

        _ = SaveAsync();
        NotifyStateChanged();
        NotifySnippetsChanged();
    }

    public bool DeleteSnippet(Guid id)
    {
        bool removed;
        lock (_lock)
        {
            removed = _snippets.RemoveAll(s => s.Id == id) > 0;
        }

        if (removed)
        {
            _ = SaveAsync();
            NotifyStateChanged();
            NotifySnippetsChanged();
        }

        return removed;
    }

    public void TogglePin(Guid id)
    {
        bool found = false;
        lock (_lock)
        {
            int idx = _snippets.FindIndex(s => s.Id == id);
            if (idx >= 0)
            {
                var existing = _snippets[idx];
                _snippets[idx] = existing with { IsPinned = !existing.IsPinned };
                found = true;
            }
        }

        if (found)
        {
            _ = SaveAsync();
            NotifySnippetsChanged();
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            // Remove all non-pinned snippets
            _snippets.RemoveAll(s => !s.IsPinned);
        }

        _ = SaveAsync();
        NotifyStateChanged();
        NotifySnippetsChanged();
    }

    public async Task<bool> CopyToClipboardAsync(Guid id)
    {
        ClipboardSnippet? snippet;
        lock (_lock)
        {
            snippet = _snippets.FirstOrDefault(s => s.Id == id);
        }

        if (snippet == null || _platformAdapter == null)
        {
            return false;
        }

        try
        {
            await _platformAdapter.SetTextAsync(snippet.Text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<ClipboardSnippet> Search(string? query, ClipboardContentType? category = null)
    {
        lock (_lock)
        {
            var queryable = _snippets.AsEnumerable();

            if (category.HasValue)
            {
                queryable = queryable.Where(s => s.ContentType == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(s => s.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
            }

            return queryable
                .OrderByDescending(s => s.IsPinned)
                .ThenByDescending(s => s.Timestamp)
                .ToList();
        }
    }

    public void UpdatePreferences(ClipboardPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        lock (_lock)
        {
            _preferences = preferences;
        }

        SavePreferences();
        AutoPrune();
    }

    public void AutoPrune()
    {
        bool changed = false;
        lock (_lock)
        {
            // Prune by age if configured
            if (_preferences.AutoPruneDays > 0)
            {
                var threshold = DateTimeOffset.UtcNow.AddDays(-_preferences.AutoPruneDays);
                int removed = _snippets.RemoveAll(s => !s.IsPinned && s.Timestamp < threshold);
                if (removed > 0) changed = true;
            }

            // Prune by capacity
            while (_snippets.Count > _preferences.MaxSnippetCapacity)
            {
                int removeIdx = _snippets.FindLastIndex(s => !s.IsPinned);
                if (removeIdx >= 0)
                {
                    _snippets.RemoveAt(removeIdx);
                    changed = true;
                }
                else
                {
                    break;
                }
            }
        }

        if (changed)
        {
            _ = SaveAsync();
            NotifySnippetsChanged();
            NotifyStateChanged();
        }
    }

    public bool ExportSnippets(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;

        try
        {
            List<ClipboardSnippet> copy;
            lock (_lock) copy = _snippets.ToList();

            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{filePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, filePath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ImportSnippets(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return false;

        try
        {
            string json = File.ReadAllText(filePath);
            var imported = JsonSerializer.Deserialize<List<ClipboardSnippet>>(json);
            if (imported == null || imported.Count == 0) return false;

            lock (_lock)
            {
                foreach (var item in imported)
                {
                    if (string.IsNullOrWhiteSpace(item.Text)) continue;
                    if (_snippets.Any(s => string.Equals(s.Text, item.Text, StringComparison.Ordinal))) continue;

                    _snippets.Add(item with
                    {
                        Id = Guid.NewGuid(),
                        ContentType = DetectContentType(item.Text)
                    });
                }
            }

            AutoPrune();
            _ = SaveAsync();
            NotifySnippetsChanged();
            NotifyStateChanged();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static ClipboardContentType DetectContentType(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return ClipboardContentType.Text;
        string trimmed = text.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return ClipboardContentType.Url;
        }

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return ClipboardContentType.Json;
            }
            catch { }
        }

        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            return ClipboardContentType.Number;
        }

        if (trimmed.Contains('@') && Regex.IsMatch(trimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return ClipboardContentType.Email;
        }

        if (trimmed.Contains("class ") || trimmed.Contains("function ") || trimmed.Contains("public ") ||
            trimmed.Contains("private ") || trimmed.Contains("import ") || trimmed.Contains("def ") ||
            trimmed.Contains("const ") || trimmed.Contains("let ") || trimmed.Contains("var ") ||
            trimmed.Contains("SELECT ") || trimmed.Contains("FROM ") || trimmed.Contains("<html>") ||
            trimmed.Contains("<div>") || trimmed.Contains("=>") || trimmed.Contains(";\n"))
        {
            return ClipboardContentType.Code;
        }

        return ClipboardContentType.Text;
    }

    public static bool IsSensitivePattern(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // JWT token
        if (Regex.IsMatch(text, @"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}"))
            return true;

        // Credit card numbers (Luhn/standard 16 digit pattern)
        if (Regex.IsMatch(text, @"\b(?:\d{4}[ -]?){3}\d{4}\b"))
            return true;

        // Private keys
        if (text.Contains("BEGIN PRIVATE KEY") || text.Contains("BEGIN RSA PRIVATE KEY") || text.Contains("BEGIN OPENSSH PRIVATE KEY"))
            return true;

        // Secret token indicators
        if (text.Contains("bearer ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("client_secret", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("api_key", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private void LoadPreferences()
    {
        if (!File.Exists(_prefsPersistencePath)) return;

        try
        {
            string json = File.ReadAllText(_prefsPersistencePath);
            var prefs = JsonSerializer.Deserialize<ClipboardPreferences>(json);
            if (prefs != null)
            {
                lock (_lock) _preferences = prefs;
            }
        }
        catch { }
    }

    private void SavePreferences()
    {
        try
        {
            ClipboardPreferences copy;
            lock (_lock) copy = _preferences;

            string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            string tmp = $"{_prefsPersistencePath}.tmp_{Guid.NewGuid():N}";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _prefsPersistencePath, overwrite: true);
        }
        catch { }
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_persistencePath)) return;

        await _fileSemaphore.WaitAsync();
        try
        {
            string? dir = Path.GetDirectoryName(_persistencePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            List<ClipboardSnippet> snapshot;
            lock (_lock)
            {
                snapshot = _snippets.ToList();
            }

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            string payload = _dataProtector.Protect(json);
            string tempPath = $"{_persistencePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                await File.WriteAllTextAsync(tempPath, payload);
                File.Move(tempPath, _persistencePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
        catch
        {
            // Ignore persistence write errors
        }
        finally
        {
            _fileSemaphore.Release();
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke(this, State);
    private void NotifySnippetsChanged() => SnippetsChanged?.Invoke(this, Snippets);

    public void Dispose()
    {
        _isDisposed = true;
    }
}
