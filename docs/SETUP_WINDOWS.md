# Windows Development Setup

## Recommended environment

- Windows 11 development machine (Windows 10 1809+ is supported by WinUI/Windows App SDK, but Windows 11 is the primary target for visual validation).
- Visual Studio 2022 (v17.13 or newer) with the "Windows application development" workload (or "Desktop development with C#" + Windows App SDK C# templates), or the .NET SDK (.NET 8 LTS, .NET 9, or .NET 10 depending on the current stable Windows App SDK template) plus the WinUI command-line template.
- Git.
- Developer Mode enabled for local packaged app development.

Use Microsoft's current stable WinUI 3 / Windows App SDK template. Package versions and exact workload names can change; verify the current Microsoft Learn quickstart before installation.

## Before starting Phase 2

1. Install Visual Studio 2022 with the Windows app development/WinUI workload and required Windows SDK components (or install templates via `dotnet new install Microsoft.WindowsAppSDK.WinUI3.Templates`).
2. Install Git and configure your preferred editor.
3. Enable Developer Mode in Windows Settings if the selected packaged template requires it.
4. Verify the installed .NET SDK (`dotnet --list-sdks`) and check that it matches the target framework supported by the selected stable Windows App SDK template (.NET 8 LTS, .NET 9, or .NET 10).
5. Create a blank packaged WinUI 3 desktop app using the current stable template.
6. Build and run it once before adding project logic.
7. Save the solution at repository root and the app project under `src/DynamicTopBar.App` (or update the documented layout if the template imposes another structure).

## Repeatable build and run commands

Build the solution:
```powershell
dotnet build DynamicTopBar.sln -p:Platform=x64
```

Run unit tests:
```powershell
dotnet test DynamicTopBar.sln
```

Launch the application:
```powershell
dotnet run --project "src\DynamicTopBar.App\DynamicTopBar.App.csproj" -p:Platform=x64
```

## Official starting points

- Windows app development: https://learn.microsoft.com/en-us/windows/apps/
- WinUI 3: https://learn.microsoft.com/en-us/windows/apps/winui/winui3/
- WinUI quickstart: https://learn.microsoft.com/en-us/windows/apps/get-started/start-here
- Windows App SDK environment setup: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment
- Windows App SDK lifecycle: https://learn.microsoft.com/en-us/windows/apps/develop/launch/app-lifecycle
- .NET release support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## Notes

- Choose the latest stable SDK/template and record exact versions in the eventual project README or build properties.
- Do not add media-control dependencies yet; those are evaluated in Phase 3.
- Avoid changing multiple SDK versions and project configurations at once when troubleshooting build failures.
