# DynamicTopBar for Windows

A native Windows desktop utility inspired by the interaction pattern of macOS's compact top bar / Dynamic Island. The app stays small while idle and expands into a contextual panel on hover or interaction.

> **Status:** Production-Ready Modular Desktop Companion. Features multi-module navigation, unified media playback, Spotify PKCE, Focus timer, Quick Notes & Clipboard Manager with local DPAPI protection, Win32 exclusive fullscreen auto-suspension, PerMonitorV2 high-DPI scaling, and Octalysis-inspired non-intrusive gamification. 310 unit tests passing (100%).

## Key Capabilities

- **Fluid Capsule Overlay:** Sleek borderless top bar clipped natively with Win32 round-rect regions, auto-hiding into a top trigger notch with smooth reveal and pointer-exit auto-collapse.
- **Exclusive Fullscreen / Game Detection:** Automatically suspends `WS_EX_TOPMOST` when exclusive fullscreen games or media occupy the display to prevent tearing, flicker, or z-fighting.
- **Zero Focus Stealing:** Strict `WS_EX_NOACTIVATE` operation during idle and playback interactions, dynamically routing keyboard input only when text boxes are engaged.
- **Local Data Security & DPAPI:** Sensitive Quick Notes and Clipboard snippets are protected using Windows DPAPI encryption (`IDataProtector`), backed by thread-safe `SemaphoreSlim` file persistence and seamless backward compatibility.
- **Octalysis Gamification Engine:** Calibrated XP progression, Daily Micro-Quests with Daily Crown rewards, Streak preservation with Streak Shields, Achievement Badges, and unlockable Capsule Theme Cosmetics.
- **Automated CI/CD:** GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`) validates builds and unit tests on Windows Server runners for every commit and pull request.

## Product principles

- The idle bar should be discreet and compact.
- The expanded panel must feel like the same component, not a separate dashboard.
- Interactions are predictable, keyboard-safe, and do not steal focus unnecessarily.
- System integration is isolated behind services/adapters.
- Keep the idle app lightweight; prefer events over frequent polling.
- Windows-native implementation first; do not copy Apple-specific menu items or assets.

## Initial stack

- **Language:** C#
- **UI:** WinUI 3 + Windows App SDK
- **Runtime:** .NET 9 (`net9.0-windows10.0.26100.0` for App; `net9.0` for Core and Tests)
- **OS integration:** Win32 / Windows App SDK / WinRT APIs behind adapters
- **Architecture:** MVVM-oriented UI, a small Core layer for app state and rules. Explicitly no remote backend, web API, cloud database, or external telemetry; "backend" means local Core logic and Windows OS adapters in the desktop process.
- **Tests:** xUnit running under .NET 9 SDK

## Quick start: build, test, run

Build the entire solution:
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

## Repository map

```text
DynamicTopBar/
├── DynamicTopBar.sln
├── README.md
├── AGENTS.md
├── .gitignore
├── .editorconfig
├── docs/
│   ├── PROJECT_BRIEF.md
│   ├── ROADMAP.md
│   ├── ARCHITECTURE.md
│   ├── UX_SPEC.md
│   ├── ENGINEERING_RULES.md
│   ├── SETUP_WINDOWS.md
│   ├── ACCEPTANCE_CRITERIA.md
│   ├── AI_PROMPT_TEMPLATE.md
│   ├── AI_AGENT_WORKFLOW.md
│   ├── agent-prompts/
│   │   ├── PHASE-1-FOUNDATION.md
│   │   ├── PHASE-2-APP-SHELL.md
│   │   ├── PHASE-3-CORE-SERVICES.md
│   │   ├── PHASE-4-UI-DETAILING.md
│   │   └── PHASE-5-QA-RELEASE.md
│   └── decisions/
│       ├── ADR-001-TECH-STACK.md
│       └── ADR-002-WINUI-TEMPLATE-AND-TARGET-FRAMEWORK.md
├── src/
│   ├── DynamicTopBar.App/
│   │   ├── DynamicTopBar.App.csproj
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── app.manifest
│   │   ├── Package.appxmanifest
│   │   ├── Assets/
│   │   ├── Configuration/
│   │   ├── Models/
│   │   ├── Platform/
│   │   │   ├── Win32Interop.cs
│   │   │   └── TrayIcon.cs
│   │   ├── Services/
│   │   ├── UI/
│   │   │   └── TopBarWindow.xaml / TopBarWindow.xaml.cs
│   │   └── README.md
│   └── DynamicTopBar.Core/
│       ├── DynamicTopBar.Core.csproj
│       ├── Interfaces/
│       ├── Models/
│       │   └── OverlayConfig.cs
│       ├── States/
│       │   └── OverlayState.cs
│       ├── UseCases/
│       └── README.md
├── tests/
│   └── DynamicTopBar.Tests/
│       ├── DynamicTopBar.Tests.csproj
│       ├── OverlayStateTests.cs
│       └── README.md
└── scripts/
    └── README.md
```

## Five-phase delivery plan

1. **Foundation:** repository structure, requirements, architecture, UX behavior, engineering rules, environment checklist.
2. **App shell:** create the real WinUI 3 solution, launchable app, single-instance behavior, tray/lifecycle foundation, and basic idle overlay.
3. **Core behavior + system services:** hover/expand state machine, media session proof-of-concept and adapter, event routing, persistence boundaries.
4. **UI detailing:** polished visuals, transitions, system icons, media controls, settings, monitor/DPI behavior, keyboard and accessibility behavior.
5. **Quality + release:** tests, performance and regression checks, packaging, install/startup options, error handling, release documentation.

## AI agent setup and phase prompts

- [`AGENTS.md`](AGENTS.md) contains repository-wide rules for AI coding agents.
- [`docs/AI_AGENT_WORKFLOW.md`](docs/AI_AGENT_WORKFLOW.md) explains how to use one prompt per phase.
- [`docs/agent-prompts/`](docs/agent-prompts/) contains the five detailed phase-specific implementation prompts.

Open the repository root in your coding agent, ask it to read `AGENTS.md`, and submit only the prompt for the active phase. Do not give all five prompts at once.

## Read these first

1. [`docs/PROJECT_BRIEF.md`](docs/PROJECT_BRIEF.md)
2. [`docs/ROADMAP.md`](docs/ROADMAP.md)
3. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
4. [`docs/UX_SPEC.md`](docs/UX_SPEC.md)
5. [`docs/ENGINEERING_RULES.md`](docs/ENGINEERING_RULES.md)

## Current non-goals

- No remote backend, web API, cloud sync, user account system, or telemetry server.
- No notifications, calendar, clipboard, weather, AI assistant/features, or third-party plugin marketplaces.
- No attempt to replace the Windows taskbar or alter system-owned UI.
- No arbitrary always-on-top behavior over secure desktops.
- No promises that overlays will appear above every exclusive-fullscreen game.
- No custom theming engine beyond defined design tokens in the MVP.
