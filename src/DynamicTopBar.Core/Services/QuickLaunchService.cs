using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Safe local quick launch service managing validated shortcuts.
/// Uses IProcessLauncher to abstract physical execution.
/// </summary>
public sealed class QuickLaunchService : IQuickLaunchService
{
    private static readonly HashSet<string> AllowedUrlSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "https",
        "ms-settings"
    };

    private static readonly HashSet<string> KnownSafeSystemTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer.exe",
        "calc.exe",
        "cmd.exe",
        "wt.exe",
        "taskmgr.exe",
        "notepad.exe"
    };

    private readonly object _lock = new();
    private readonly List<QuickLaunchItem> _items = new();
    private readonly List<QuickLaunchItem> _recentItems = new();
    private readonly IProcessLauncher? _processLauncher;
    private readonly string? _persistenceFilePath;

    public IReadOnlyList<QuickLaunchItem> Items
    {
        get
        {
            lock (_lock)
            {
                return _items.ToList();
            }
        }
    }

    public IReadOnlyList<QuickLaunchItem> RecentItems
    {
        get
        {
            lock (_lock)
            {
                return _recentItems.ToList();
            }
        }
    }

    public event EventHandler? ItemsChanged;

    public QuickLaunchService(IProcessLauncher? processLauncher = null, IEnumerable<QuickLaunchItem>? initialItems = null, string? persistenceFilePath = null)
    {
        _processLauncher = processLauncher;
        _persistenceFilePath = persistenceFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "shortcuts.json");

        if (initialItems != null)
        {
            _items.AddRange(initialItems);
        }
        else
        {
            _items.AddRange(GetDefaultItems());
            LoadCustomItems();
        }
    }

    public static IReadOnlyList<QuickLaunchItem> GetDefaultItems() => new List<QuickLaunchItem>
    {
        new()
        {
            Id = "explorer",
            Title = "Files",
            PathOrUri = "explorer.exe",
            IconGlyph = "\uED25",
            Type = QuickLaunchTargetType.WindowsTool,
            IsCustom = false
        },
        new()
        {
            Id = "browser",
            Title = "Browser",
            PathOrUri = "https://www.google.com",
            IconGlyph = "\uE774",
            Type = QuickLaunchTargetType.Url,
            IsCustom = false
        },
        new()
        {
            Id = "terminal",
            Title = "Terminal",
            PathOrUri = "cmd.exe",
            IconGlyph = "\uE756",
            Type = QuickLaunchTargetType.WindowsTool,
            IsCustom = false
        },
        new()
        {
            Id = "calculator",
            Title = "Calculator",
            PathOrUri = "calc.exe",
            IconGlyph = "\uE8EF",
            Type = QuickLaunchTargetType.WindowsTool,
            IsCustom = false
        },
        new()
        {
            Id = "settings",
            Title = "Settings",
            PathOrUri = "ms-settings:",
            IconGlyph = "\uE713",
            Type = QuickLaunchTargetType.WindowsTool,
            IsCustom = false
        },
        new()
        {
            Id = "taskmgr",
            Title = "Task Mgr",
            PathOrUri = "taskmgr.exe",
            IconGlyph = "\uE9D9",
            Type = QuickLaunchTargetType.WindowsTool,
            IsCustom = false
        }
    };

    public QuickLaunchResult ValidateTarget(string pathOrUri, QuickLaunchTargetType type)
    {
        if (string.IsNullOrWhiteSpace(pathOrUri))
        {
            return QuickLaunchResult.Fail("Target path or URI cannot be empty.");
        }

        switch (type)
        {
            case QuickLaunchTargetType.Url:
                if (!Uri.TryCreate(pathOrUri, UriKind.Absolute, out var uri))
                {
                    return QuickLaunchResult.Fail("Malformed URL format.");
                }

                if (!AllowedUrlSchemes.Contains(uri.Scheme))
                {
                    return QuickLaunchResult.Fail($"Unsupported or prohibited URL scheme: '{uri.Scheme}'. Only http, https, and ms-settings are permitted.");
                }
                return QuickLaunchResult.Ok();

            case QuickLaunchTargetType.Folder:
                if (pathOrUri.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return QuickLaunchResult.Fail("Folder path contains invalid characters.");
                }

                // If a full path, verify directory existence
                if (Path.IsPathRooted(pathOrUri) && !Directory.Exists(pathOrUri))
                {
                    return QuickLaunchResult.Fail($"Folder does not exist: {pathOrUri}");
                }
                return QuickLaunchResult.Ok();

            case QuickLaunchTargetType.Application:
            case QuickLaunchTargetType.WindowsTool:
                string fileName = Path.GetFileName(pathOrUri);
                if (KnownSafeSystemTools.Contains(fileName) || KnownSafeSystemTools.Contains(pathOrUri))
                {
                    return QuickLaunchResult.Ok();
                }

                if (pathOrUri.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase))
                {
                    return QuickLaunchResult.Ok();
                }

                if (pathOrUri.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    return QuickLaunchResult.Fail("File path contains invalid characters.");
                }

                if (Path.IsPathRooted(pathOrUri) && !File.Exists(pathOrUri))
                {
                    return QuickLaunchResult.Fail($"Executable target not found: {pathOrUri}");
                }
                return QuickLaunchResult.Ok();

            default:
                return QuickLaunchResult.Fail("Unknown target type.");
        }
    }

    public QuickLaunchResult Launch(string itemId)
    {
        QuickLaunchItem? item;
        lock (_lock)
        {
            item = _items.FirstOrDefault(i => string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
        }

        if (item == null)
        {
            return QuickLaunchResult.Fail($"Shortcut '{itemId}' not found.");
        }

        var validation = ValidateTarget(item.PathOrUri, item.Type);
        if (!validation.Success)
        {
            return validation;
        }

        bool success = true;
        if (_processLauncher != null)
        {
            success = _processLauncher.Launch(item.PathOrUri, item.Arguments);
        }

        if (success)
        {
            lock (_lock)
            {
                _recentItems.RemoveAll(r => string.Equals(r.Id, item.Id, StringComparison.OrdinalIgnoreCase));
                _recentItems.Insert(0, item);
                if (_recentItems.Count > 5)
                {
                    _recentItems.RemoveAt(_recentItems.Count - 1);
                }
            }
            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return QuickLaunchResult.Ok();
        }

        return QuickLaunchResult.Fail($"Failed to launch '{item.Title}'.");
    }

    public void ClearRecentHistory()
    {
        lock (_lock)
        {
            _recentItems.Clear();
        }
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    public QuickLaunchResult AddItem(QuickLaunchItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var validation = ValidateTarget(item.PathOrUri, item.Type);
        if (!validation.Success)
        {
            return validation;
        }

        lock (_lock)
        {
            if (_items.Any(i => string.Equals(i.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
            {
                return QuickLaunchResult.Fail($"Item with ID '{item.Id}' already exists.");
            }

            _items.Add(item);
        }

        SaveCustomItems();
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        return QuickLaunchResult.Ok();
    }

    public bool RemoveItem(string itemId)
    {
        bool removed = false;
        lock (_lock)
        {
            int index = _items.FindIndex(i => string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _items.RemoveAt(index);
                removed = true;
            }
        }

        if (removed)
        {
            SaveCustomItems();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    private void SaveCustomItems()
    {
        if (string.IsNullOrEmpty(_persistenceFilePath)) return;

        try
        {
            string? dir = Path.GetDirectoryName(_persistenceFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var customItems = Items.Where(i => i.IsCustom).ToList();
            string json = System.Text.Json.JsonSerializer.Serialize(customItems, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            string tempPath = $"{_persistenceFilePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _persistenceFilePath, overwrite: true);
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
            // Ignore persistence errors
        }
    }

    private void LoadCustomItems()
    {
        if (string.IsNullOrEmpty(_persistenceFilePath) || !File.Exists(_persistenceFilePath)) return;

        try
        {
            string json = File.ReadAllText(_persistenceFilePath);
            var customItems = System.Text.Json.JsonSerializer.Deserialize<List<QuickLaunchItem>>(json);
            if (customItems != null)
            {
                foreach (var item in customItems)
                {
                    if (!_items.Any(i => string.Equals(i.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        _items.Add(item);
                    }
                }
            }
        }
        catch
        {
            // Ignore corrupted shortcuts file
        }
    }
}
