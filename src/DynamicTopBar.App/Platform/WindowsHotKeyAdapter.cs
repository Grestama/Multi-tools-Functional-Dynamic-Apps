using System;
using System.Collections.Generic;
using System.Linq;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Connects the platform-independent IShortcutService to Windows RegisterHotKey / UnregisterHotKey APIs.
/// </summary>
public sealed class WindowsHotKeyAdapter : IDisposable
{
    private const int BaseHotKeyId = 0x9000;

    private readonly IntPtr _hwnd;
    private readonly IShortcutService _shortcutService;
    private readonly TrayIcon _trayIcon;
    private readonly Dictionary<int, string> _idToShortcutId = new();
    private readonly Dictionary<string, int> _shortcutIdToId = new();
    private readonly object _lock = new();
    private int _nextId = BaseHotKeyId;
    private bool _isDisposed;

    public WindowsHotKeyAdapter(IntPtr hwnd, IShortcutService shortcutService, TrayIcon trayIcon)
    {
        _hwnd = hwnd;
        _shortcutService = shortcutService ?? throw new ArgumentNullException(nameof(shortcutService));
        _trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));

        _trayIcon.HotKeyTriggered += OnTrayHotKeyTriggered;
        _shortcutService.ShortcutsChanged += OnShortcutsChanged;

        RegisterConfiguredHotKeys();
    }

    public void RegisterConfiguredHotKeys()
    {
        lock (_lock)
        {
            UnregisterAllHotKeys();

            foreach (var shortcut in _shortcutService.Shortcuts.Where(s => s.IsEnabled))
            {
                uint fsModifiers = Win32Interop.MOD_NOREPEAT;
                if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Alt)) fsModifiers |= Win32Interop.MOD_ALT;
                if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Control)) fsModifiers |= Win32Interop.MOD_CONTROL;
                if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Shift)) fsModifiers |= Win32Interop.MOD_SHIFT;
                if (shortcut.Modifiers.HasFlag(ShortcutModifiers.Windows)) fsModifiers |= Win32Interop.MOD_WIN;

                uint vk = (uint)shortcut.Key;
                if (vk == 0) continue;

                int hotKeyId = _nextId++;
                bool success = Win32Interop.RegisterHotKey(_hwnd, hotKeyId, fsModifiers, vk);
                if (success)
                {
                    _idToShortcutId[hotKeyId] = shortcut.Id;
                    _shortcutIdToId[shortcut.Id] = hotKeyId;
                }
            }
        }
    }

    private void UnregisterAllHotKeys()
    {
        foreach (var id in _idToShortcutId.Keys)
        {
            Win32Interop.UnregisterHotKey(_hwnd, id);
        }
        _idToShortcutId.Clear();
        _shortcutIdToId.Clear();
    }

    private void OnTrayHotKeyTriggered(int hotKeyId)
    {
        string? shortcutId;
        lock (_lock)
        {
            _idToShortcutId.TryGetValue(hotKeyId, out shortcutId);
        }

        if (!string.IsNullOrEmpty(shortcutId))
        {
            _shortcutService.HandleHotKeyTriggered(shortcutId);
        }
    }

    private void OnShortcutsChanged(object? sender, IReadOnlyList<GlobalShortcut> e)
    {
        RegisterConfiguredHotKeys();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _trayIcon.HotKeyTriggered -= OnTrayHotKeyTriggered;
        _shortcutService.ShortcutsChanged -= OnShortcutsChanged;

        lock (_lock)
        {
            UnregisterAllHotKeys();
        }
    }
}
