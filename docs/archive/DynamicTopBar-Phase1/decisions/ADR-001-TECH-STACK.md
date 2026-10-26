# ADR-001: Native Windows Stack

- **Status:** Accepted for initial implementation; validate during Phase 2.
- **Date:** 2026-10-19

## Context

The application needs a low-footprint Windows desktop overlay, predictable control over top-level window behavior, system-tray integration, and potential media-session access. A web app alone cannot provide this desktop behavior without a native wrapper.

## Decision

Use C# + WinUI 3 + Windows App SDK for the desktop UI. Target .NET 10 LTS where supported by the selected current stable WinUI template. Keep platform-specific window/media code behind adapters. Use a small Core project for state transitions and neutral models.

## Reasons

- WinUI 3 is Microsoft's recommended native UI framework for new Windows desktop applications.
- C# and XAML support fast iteration on layout, animation, and system integration.
- A native desktop app fits the need for a persistent overlay and tray/lifecycle behavior.
- .NET 10 has a longer support runway than .NET 8/9 as of this ADR date; still verify the exact WinUI template compatibility before pinning target frameworks.

## Alternatives considered

- **Electron:** mature web UI ecosystem, but a Chromium runtime is a heavier starting point for a small always-running utility.
- **Tauri:** smaller web-based shell, but requires extra native integration for this windowing and media use case.
- **WPF:** mature desktop framework, but WinUI 3 is preferred for this project's current Windows 11 visual direction.

## Consequences

- Development and visual validation require a Windows machine and the Windows app toolchain (Visual Studio 2022 v17.13+ or .NET SDK CLI).
- Window z-order, hit testing, media API interop, and packaging need explicit validation.
- While .NET 10 is the preferred LTS forward target, current stable Windows App SDK templates target .NET 8 LTS or .NET 9 STS. If WinUI/Windows App SDK compatibility dictates .NET 8 or 9 during Phase 2 preflight, record the tested supported target in a follow-up ADR rather than silently diverging.

## References

- https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
- https://learn.microsoft.com/en-us/windows/apps/get-started/start-here
- https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
