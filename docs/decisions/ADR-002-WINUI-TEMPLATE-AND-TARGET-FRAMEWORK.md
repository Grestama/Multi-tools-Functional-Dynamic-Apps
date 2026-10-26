# ADR-002: WinUI 3 Template and Target Framework Selection

- **Status:** Accepted for Phase 2 implementation.
- **Date:** 2026-10-19

## Context

Phase 1 established that DynamicTopBar would target C# + WinUI 3 + Windows App SDK, conditionally choosing between .NET 8 LTS, .NET 9 STS, or .NET 10 LTS based on the installed toolchain and stable Microsoft templates.

During Phase 2 preflight on the Windows 11 target machine:
- Installed .NET SDK is **9.0.203** (with .NET 8.0.15 and .NET 9.0.4 runtimes).
- Installed IDE is **Visual Studio 2022 Community v17.13.6**.
- The official `Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` template was installed via CLI.
- Template evaluation showed that .NET 9 (`net9.0-windows10.0.26100.0` with `TargetPlatformMinVersion` 10.0.17763.0) aligns directly with the installed SDK and Windows App SDK packages.

## Decision

1. **Target Frameworks:**
   - `DynamicTopBar.App`: `net9.0-windows10.0.26100.0`
   - `DynamicTopBar.Core`: `net9.0` (pure, platform-independent C# class library)
   - `DynamicTopBar.Tests`: `net9.0` (xUnit test project)
2. **Packaging:**
   - Single-project MSIX packaging using `Microsoft.Windows.SDK.BuildTools.WinApp` (supporting `dotnet run` with package identity via winapp CLI).
3. **Windowing & Focus Policy:**
   - Use `AppWindow.Presenter` (`OverlappedPresenter`) with `SetBorderAndTitleBar(false, false)` and `IsAlwaysOnTop = true`.
   - Use native Win32 extended window styles: `WS_EX_NOACTIVATE` (0x08000000) to strictly avoid stealing keyboard focus upon display, `WS_EX_TOOLWINDOW` (0x80) to exclude the overlay from Alt+Tab and the Windows taskbar, and `WS_EX_TOPMOST` (0x08).
4. **System Tray Integration:**
   - Implement `TrayIcon` via native Win32 `Shell_NotifyIconW` and `SetWindowSubclass` for zero extra third-party dependencies, reliable right-click context menu, and clean icon removal on exit.
5. **Single-Instance Enforcement:**
   - Use a named system `Mutex` (`DynamicTopBar.SingleInstance.Mutex.7B0F33A9`) to guarantee that only one instance of the top bar runs at any time.

## Consequences

- The app builds with zero warnings and zero errors under .NET 9 SDK.
- The Core library remains 100% testable and decoupled from Windows/WinUI types.
- The overlay bar appears without stealing keyboard focus or cluttering the taskbar.
- Reliable application exit is provided through the system tray.
