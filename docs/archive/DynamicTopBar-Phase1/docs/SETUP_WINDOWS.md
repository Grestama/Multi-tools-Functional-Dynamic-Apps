# Windows Development Setup

## Recommended environment

- Windows 11 development machine (Windows 10 1809+ is supported by WinUI/Windows App SDK, but Windows 11 is the primary target for visual validation).
- Visual Studio 2026 with the required .NET desktop and Windows App SDK/WinUI tooling, or .NET 10 SDK plus the WinUI command-line template.
- Git.
- Developer Mode enabled for local packaged app development.

Use Microsoft's current stable WinUI 3 / Windows App SDK template. Package versions and exact workload names can change; verify the current Microsoft Learn quickstart before installation.

## Before starting Phase 2

1. Install Visual Studio with the Windows app development/WinUI workload and required Windows SDK components.
2. Install Git and configure your preferred editor.
3. Enable Developer Mode in Windows Settings if the selected packaged template requires it.
4. Check that the .NET 10 SDK is installed when the chosen template targets .NET 10.
5. Create a blank packaged WinUI 3 desktop app using the current stable template.
6. Build and run it once before adding project logic.
7. Save the solution at repository root and the app project under `src/DynamicTopBar.App` (or update the documented layout if the template imposes another structure).

## Official starting points

- Windows app development: https://learn.microsoft.com/en-us/windows/apps/
- WinUI 3: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
- WinUI quickstart: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here
- Windows App SDK lifecycle: https://learn.microsoft.com/en-us/windows/apps/develop/launch/app-lifecycle
- .NET release support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## Notes

- Choose the latest stable SDK/template and record exact versions in the eventual project README or build properties.
- Do not add media-control dependencies yet; those are evaluated in Phase 3.
- Avoid changing multiple SDK versions and project configurations at once when troubleshooting build failures.
