using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract managing global keyboard shortcuts, conflict detection, validation, and action dispatching.
/// </summary>
public interface IShortcutService : IDisposable
{
    IReadOnlyList<GlobalShortcut> Shortcuts { get; }

    event EventHandler<string>? ShortcutTriggered;
    event EventHandler<IReadOnlyList<GlobalShortcut>>? ShortcutsChanged;

    ShortcutValidationResult ValidateShortcut(ShortcutModifiers modifiers, ShortcutVirtualKey key, string? ignoreShortcutId = null);
    bool UpdateShortcut(string id, ShortcutModifiers modifiers, ShortcutVirtualKey key, bool isEnabled);
    bool SetShortcutEnabled(string id, bool isEnabled);
    bool ResetToDefaults();

    string ExportConfiguration();
    bool ImportConfiguration(string json);

    void HandleHotKeyTriggered(string shortcutId);
}
