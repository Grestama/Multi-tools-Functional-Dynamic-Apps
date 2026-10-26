using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Manages the system notification area (tray) icon and context menu using native Win32 APIs.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    public static readonly uint WmShowDynamicTopBar = Win32Interop.RegisterWindowMessage("DynamicTopBar.ShowOverlayMessage");

    private const uint TrayIconId = 1001;
    private const uint CmdHeader = 2000;
    private const uint CmdToggleVisibility = 2001;
    private const uint CmdCommandPalette = 2002;
    private const uint CmdSettings = 2003;
    private const uint CmdQuit = 2004;

    private readonly IntPtr _hwnd;
    private readonly Win32Interop.SubclassProc _subclassProc;
    private readonly UIntPtr _subclassId = new(1);
    private Win32Interop.NOTIFYICONDATA _notifyData;
    private IntPtr _hIcon = IntPtr.Zero;
    private bool _isDisposed;

    public bool IsOverlayVisible { get; set; } = true;

    public event Action? ToggleVisibilityRequested;
    public event Action? ShowOverlayRequested;
    public event Action? CommandPaletteRequested;
    public event Action? SettingsRequested;
    public event Action? SystemTimeChanged;
    public event Action? SystemResumed;
    public event Action<int>? HotKeyTriggered;
    public event Action? QuitRequested;

    public TrayIcon(IntPtr hwnd, string? iconPath = null)
    {
        _hwnd = hwnd;
        _subclassProc = WindowSubclassProc;

        // Subclass window to capture WM_TRAYICON messages
        Win32Interop.SetWindowSubclass(_hwnd, _subclassProc, _subclassId, UIntPtr.Zero);

        // Load application icon from disk or fallback to standard application icon
        if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
        {
            _hIcon = Win32Interop.LoadImage(IntPtr.Zero, iconPath, Win32Interop.IMAGE_ICON, 16, 16, Win32Interop.LR_LOADFROMFILE);
        }

        if (_hIcon == IntPtr.Zero)
        {
            _hIcon = Win32Interop.LoadIcon(IntPtr.Zero, (IntPtr)Win32Interop.IDI_APPLICATION);
        }

        _notifyData = new Win32Interop.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<Win32Interop.NOTIFYICONDATA>(),
            hWnd = _hwnd,
            uID = TrayIconId,
            uFlags = Win32Interop.NIF_MESSAGE | Win32Interop.NIF_ICON | Win32Interop.NIF_TIP,
            uCallbackMessage = Win32Interop.WM_TRAYICON,
            hIcon = _hIcon,
            szTip = "DynamicTopBar"
        };

        Win32Interop.Shell_NotifyIcon(Win32Interop.NIM_ADD, ref _notifyData);
    }

    private IntPtr WindowSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData)
    {
        if (uMsg == WmShowDynamicTopBar)
        {
            ShowOverlayRequested?.Invoke();
            return IntPtr.Zero;
        }

        if (uMsg == Win32Interop.WM_TIMECHANGE)
        {
            SystemTimeChanged?.Invoke();
            return IntPtr.Zero;
        }

        if (uMsg == Win32Interop.WM_POWERBROADCAST)
        {
            long pbt = wParam.ToInt64();
            if (pbt == Win32Interop.PBT_APMRESUMEAUTOMATIC || pbt == Win32Interop.PBT_APMRESUMESUSPEND)
            {
                SystemResumed?.Invoke();
            }
            return IntPtr.Zero;
        }

        if (uMsg == Win32Interop.WM_HOTKEY)
        {
            int hotKeyId = wParam.ToInt32();
            HotKeyTriggered?.Invoke(hotKeyId);
            return IntPtr.Zero;
        }

        if (uMsg == Win32Interop.WM_TRAYICON)
        {
            uint mouseMsg = (uint)(lParam.ToInt64() & 0xFFFF);
            if (mouseMsg == Win32Interop.WM_RBUTTONUP || mouseMsg == Win32Interop.WM_CONTEXTMENU)
            {
                ShowContextMenu();
                return IntPtr.Zero;
            }
            else if (mouseMsg == Win32Interop.WM_LBUTTONUP)
            {
                ToggleVisibilityRequested?.Invoke();
                return IntPtr.Zero;
            }
        }

        return Win32Interop.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void ShowContextMenu()
    {
        if (!Win32Interop.GetCursorPos(out var pt))
        {
            return;
        }

        // Must foreground the window before TrackPopupMenu so clicking outside dismisses the menu
        Win32Interop.SetForegroundWindow(_hwnd);

        IntPtr hMenu = Win32Interop.CreatePopupMenu();
        if (hMenu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_STRING | Win32Interop.MF_DISABLED | Win32Interop.MF_GRAYED, CmdHeader, "DynamicTopBar");
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_SEPARATOR, 0, string.Empty);
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_STRING, CmdToggleVisibility, IsOverlayVisible ? "Hide Top Bar" : "Show Top Bar");
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_STRING, CmdCommandPalette, "Command Palette (Ctrl+Alt+Space)");
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_STRING, CmdSettings, "Settings & Preferences...");
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_SEPARATOR, 0, string.Empty);
            Win32Interop.AppendMenu(hMenu, Win32Interop.MF_STRING, CmdQuit, "Quit DynamicTopBar");

            uint selected = Win32Interop.TrackPopupMenuEx(
                hMenu,
                Win32Interop.TPM_LEFTALIGN | Win32Interop.TPM_RIGHTBUTTON | Win32Interop.TPM_RETURNCMD,
                pt.X,
                pt.Y,
                _hwnd,
                IntPtr.Zero);

            if (selected == CmdToggleVisibility)
            {
                ToggleVisibilityRequested?.Invoke();
            }
            else if (selected == CmdCommandPalette)
            {
                CommandPaletteRequested?.Invoke();
            }
            else if (selected == CmdSettings)
            {
                SettingsRequested?.Invoke();
            }
            else if (selected == CmdQuit)
            {
                QuitRequested?.Invoke();
            }
        }
        finally
        {
            Win32Interop.DestroyMenu(hMenu);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Win32Interop.Shell_NotifyIcon(Win32Interop.NIM_DELETE, ref _notifyData);
        Win32Interop.RemoveWindowSubclass(_hwnd, _subclassProc, _subclassId);

        if (_hIcon != IntPtr.Zero)
        {
            Win32Interop.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
