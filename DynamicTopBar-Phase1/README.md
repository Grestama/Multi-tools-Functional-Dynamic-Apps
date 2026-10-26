# DynamicTopBar for Windows

A native Windows desktop utility inspired by the interaction pattern of macOS's compact top bar / Dynamic Island. The app stays small while idle and expands into a contextual panel on hover or interaction.

> **Current phase:** Phase 1 — project foundation and documentation. The solution and executable app are intentionally not created yet; those belong to Phase 2.

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
- **Runtime:** .NET 10 LTS, where supported by the selected stable WinUI/Windows App SDK template
- **OS integration:** Win32 / Windows App SDK / WinRT APIs behind adapters
- **Architecture:** MVVM-oriented UI, a small Core layer for app state and rules, no web backend
- **Tests:** xUnit or the test template compatible with the chosen .NET SDK

Select the latest stable supported SDK/template when creating the solution; do not pin a package version in docs before verifying compatibility.

## Repository map

```text
DynamicTopBar/
├── README.md
├── AGENTS.md
├── .gitignore
├── .editorconfig
├── docs/
│   ├── PROJECT_BRIEF.md
│   ├── ROADMAP.md
│   ├── ARCHITECTURE.md
│   ├── UX_SPEC.md
│   ├── FEATURE_CATALOG.md
│   ├── FEATURE_DEVELOPMENT_PLAN.md
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
│       └── ADR-001-TECH-STACK.md
├── src/
│   ├── DynamicTopBar.App/
│   │   ├── UI/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Platform/
│   │   ├── Configuration/
│   │   └── Assets/
│   └── DynamicTopBar.Core/
│       ├── Models/
│       ├── States/
│       ├── Interfaces/
│       └── UseCases/
├── tests/
│   └── DynamicTopBar.Tests/
└── scripts/
```

Some empty folders are tracked with `.gitkeep`. Phase 2 will generate the real WinUI project in `src/DynamicTopBar.App` and a solution at repository root. If the project template creates a different internal layout, document it and preserve these layer boundaries rather than forcing folders blindly.

## Five-phase delivery plan

1. **Foundation:** repository structure, requirements, architecture, UX behavior, engineering rules, environment checklist.
2. **App shell:** create the real WinUI 3 solution, launchable app, single-instance behavior, tray/lifecycle foundation, and basic idle overlay.
3. **Core behavior + system services:** hover/expand state machine, media session proof-of-concept and adapter, event routing, persistence boundaries.
4. **UI detailing:** polished multi-feature panel, feature navigation arrows/page indicator, module-specific views, transitions, system icons, settings, monitor/DPI behavior, keyboard and accessibility behavior.
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
5. [`docs/FEATURE_CATALOG.md`](docs/FEATURE_CATALOG.md)
6. [`docs/FEATURE_DEVELOPMENT_PLAN.md`](docs/FEATURE_DEVELOPMENT_PLAN.md)
7. [`docs/ENGINEERING_RULES.md`](docs/ENGINEERING_RULES.md)

## Current non-goals

- No cloud backend, account system, or telemetry server.
- No attempt to replace the Windows taskbar or alter system-owned UI.
- No arbitrary always-on-top behavior over secure desktops.
- No promises that overlays will appear above every exclusive-fullscreen game.
- No broad widget marketplace in the MVP.
