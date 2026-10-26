using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Services;

/// <summary>
/// Platform-independent shortcut coordination service.
/// Validates key combinations, prevents conflicting assignments, and manages atomic persistence.
/// </summary>
public sealed class ShortcutService : IShortcutService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly object _lock = new();
    private readonly string _persistencePath;
    private readonly List<GlobalShortcut> _shortcuts = new();
    private bool _isDisposed;

    public IReadOnlyList<GlobalShortcut> Shortcuts
    {
        get
        {
            lock (_lock)
            {
                return _shortcuts.ToList();
            }
        }
    }

    public event EventHandler<string>? ShortcutTriggered;
    public event EventHandler<IReadOnlyList<GlobalShortcut>>? ShortcutsChanged;

    public ShortcutService(string? persistencePath = null)
    {
        _persistencePath = persistencePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar",
            "shortcuts_config.json");

        LoadShortcuts();
    }

    public static IReadOnlyList<GlobalShortcut> GetDefaultShortcuts() => new List<GlobalShortcut>
    {
        new()
        {
            Id = "command-palette",
            Name = "Command Palette",
            ActionId = "open-command-palette",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.P,
            IsEnabled = true
        },
        new()
        {
            Id = "toggle-overlay",
            Name = "Toggle Capsule Visibility",
            ActionId = "toggle-overlay-visibility",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.Space,
            IsEnabled = true
        },
        new()
        {
            Id = "expand-panel",
            Name = "Expand / Collapse Panel",
            ActionId = "toggle-panel-expansion",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.Down,
            IsEnabled = true
        },
        new()
        {
            Id = "next-module",
            Name = "Next Feature Module",
            ActionId = "navigate-next-feature",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.Right,
            IsEnabled = true
        },
        new()
        {
            Id = "prev-module",
            Name = "Previous Feature Module",
            ActionId = "navigate-prev-feature",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.Left,
            IsEnabled = true
        },
        new()
        {
            Id = "media-toggle",
            Name = "Media Play / Pause",
            ActionId = "media-play-pause",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.F8,
            IsEnabled = true
        },
        new()
        {
            Id = "focus-toggle",
            Name = "Focus Timer Start / Pause",
            ActionId = "focus-start-pause",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.F9,
            IsEnabled = true
        },
        new()
        {
            Id = "quick-note",
            Name = "Open Quick Notes",
            ActionId = "open-quick-notes",
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = ShortcutVirtualKey.N,
            IsEnabled = true
        }
    };

    public ShortcutValidationResult ValidateShortcut(ShortcutModifiers modifiers, ShortcutVirtualKey key, string? ignoreShortcutId = null)
    {
        if (key == ShortcutVirtualKey.None)
        {
            return ShortcutValidationResult.Fail("A valid key must be selected.");
        }

        if (modifiers == ShortcutModifiers.None)
        {
            return ShortcutValidationResult.Fail("At least one modifier key (Ctrl, Alt, Shift, or Win) is required for global hotkeys.");
        }

        // Avoid reserving pure system chords (e.g., Ctrl+Alt+Del, Alt+Tab, Win+L)
        if (modifiers == (ShortcutModifiers.Control | ShortcutModifiers.Alt) && key == ShortcutVirtualKey.Escape)
        {
            return ShortcutValidationResult.Fail("This key combination is reserved by the operating system.");
        }

        lock (_lock)
        {
            var conflict = _shortcuts.FirstOrDefault(s =>
                s.IsEnabled &&
                !string.Equals(s.Id, ignoreShortcutId, StringComparison.OrdinalIgnoreCase) &&
                s.Modifiers == modifiers &&
                s.Key == key);

            if (conflict != null)
            {
                return ShortcutValidationResult.Fail($"Combination '{conflict.FormattedKeyCombo}' is already assigned to '{conflict.Name}'.");
            }
        }

        return ShortcutValidationResult.Success;
    }

    public bool UpdateShortcut(string id, ShortcutModifiers modifiers, ShortcutVirtualKey key, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        var validation = ValidateShortcut(modifiers, key, id);
        if (!validation.IsValid && isEnabled)
        {
            return false;
        }

        bool updated = false;
        lock (_lock)
        {
            int idx = _shortcuts.FindIndex(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                var existing = _shortcuts[idx];
                _shortcuts[idx] = existing with
                {
                    Modifiers = modifiers,
                    Key = key,
                    IsEnabled = isEnabled
                };
                updated = true;
            }
        }

        if (updated)
        {
            SaveShortcuts();
            ShortcutsChanged?.Invoke(this, Shortcuts);
        }

        return updated;
    }

    public bool SetShortcutEnabled(string id, bool isEnabled)
    {
        bool updated = false;
        lock (_lock)
        {
            int idx = _shortcuts.FindIndex(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                _shortcuts[idx] = _shortcuts[idx] with { IsEnabled = isEnabled };
                updated = true;
            }
        }

        if (updated)
        {
            SaveShortcuts();
            ShortcutsChanged?.Invoke(this, Shortcuts);
        }

        return updated;
    }

    public bool ResetToDefaults()
    {
        lock (_lock)
        {
            _shortcuts.Clear();
            _shortcuts.AddRange(GetDefaultShortcuts());
        }

        SaveShortcuts();
        ShortcutsChanged?.Invoke(this, Shortcuts);
        return true;
    }

    public string ExportConfiguration()
    {
        lock (_lock)
        {
            return JsonSerializer.Serialize(_shortcuts, JsonOptions);
        }
    }

    public bool ImportConfiguration(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        try
        {
            var imported = JsonSerializer.Deserialize<List<GlobalShortcut>>(json, JsonOptions);
            if (imported == null || imported.Count == 0) return false;

            lock (_lock)
            {
                _shortcuts.Clear();
                _shortcuts.AddRange(imported);
            }

            SaveShortcuts();
            ShortcutsChanged?.Invoke(this, Shortcuts);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void HandleHotKeyTriggered(string shortcutId)
    {
        if (string.IsNullOrWhiteSpace(shortcutId)) return;

        GlobalShortcut? match;
        lock (_lock)
        {
            match = _shortcuts.FirstOrDefault(s => s.IsEnabled && string.Equals(s.Id, shortcutId, StringComparison.OrdinalIgnoreCase));
        }

        if (match != null)
        {
            ShortcutTriggered?.Invoke(this, match.ActionId);
        }
    }

    private void LoadShortcuts()
    {
        lock (_lock)
        {
            _shortcuts.Clear();
            if (File.Exists(_persistencePath))
            {
                try
                {
                    string json = File.ReadAllText(_persistencePath);
                    var loaded = JsonSerializer.Deserialize<List<GlobalShortcut>>(json, JsonOptions);
                    if (loaded != null && loaded.Count > 0)
                    {
                        _shortcuts.AddRange(loaded);
                        return;
                    }
                }
                catch
                {
                    // Fallback to defaults on corrupted file
                }
            }

            _shortcuts.AddRange(GetDefaultShortcuts());
        }
    }

    private void SaveShortcuts()
    {
        if (string.IsNullOrEmpty(_persistencePath)) return;

        try
        {
            string? dir = Path.GetDirectoryName(_persistencePath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            List<GlobalShortcut> snapshot;
            lock (_lock)
            {
                snapshot = _shortcuts.ToList();
            }

            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            string tempPath = $"{_persistencePath}.tmp_{Guid.NewGuid():N}";
            try
            {
                File.WriteAllText(tempPath, json);
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
            // Ignore persistence errors
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
    }
}
