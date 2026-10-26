# AI Agent Prompt — Phase 3: Core State Machine & Windows Media Services

Copy this prompt into your coding agent after Phase 2 has a runnable app shell.

---

You are implementing **Phase 3 only** of DynamicTopBar. Follow `AGENTS.md`; inspect the current code before proposing or applying changes. Do not assume Phase 2 is complete unless the repository proves it.

## Objective

Implement predictable state transitions and integrate real Windows media-session data behind a local adapter. The result should respond to hover and playback changes without tightly polling or coupling Core to Windows APIs.

## Read first

- `AGENTS.md`
- `docs/PROJECT_BRIEF.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/FEATURE_CATALOG.md`
- `docs/FEATURE_DEVELOPMENT_PLAN.md`
- `docs/ENGINEERING_RULES.md`
- `docs/ACCEPTANCE_CRITERIA.md`
- relevant app/Core source and tests

## Tasks

1. Audit the current solution, build status, existing abstractions, and tests. Run baseline tests before editing if possible.
2. Implement a testable Core state model with `Idle`, `HoverPending`, `Expanded`, and `CollapsePending`. Use an explicit transition/event model rather than scattered view booleans.
3. Respect the baseline timing: hover delay 500 ms; collapse delay 4000 ms. Pointer re-entry must cancel pending collapse; leaving before hover delay cancels expansion. Active interaction must not be interrupted. Make timing controllable in tests instead of relying on long real sleeps.
4. Define neutral media models: title, artist, artwork reference or bytes strategy, playback status, position/duration when available, and supported actions. Avoid Windows-specific types in Core.
5. Research and implement the appropriate Windows media-session adapter for the actual target framework. Verify exact API types and availability against current Microsoft documentation and the selected target. Add an ADR if API constraints require a material architectural adjustment.
6. Expose current media snapshot and change notifications through a purposeful service boundary such as `IMediaSessionService`. Prefer event-driven updates; justify any fallback polling and set appropriate cancellation/cleanup.
7. Route the Core state/media changes into the existing ViewModel/UI without blocking the UI thread. Keep platform-specific calls inside `App/Platform` or the documented adapter layer.
8. Handle no media session, media app closed, partial metadata, missing artwork, unsupported next/previous/play-pause controls, and API exceptions without crashing.
9. Add unit tests for state transitions, re-entry/cancellation, metadata mapping, and fallbacks. Ensure event handlers/tasks are unsubscribed or cancelled during shutdown.
10. Implement the shared feature navigation contract: a stable feature ID, an available/enabled feature list, selected-feature state, Previous/Next commands, and first/last boundary behavior. Add tests for zero/one/multiple modules, invalid persisted selection, and navigation boundaries.
11. Make Media and Focus Timer the first real selectable modules. Focus Timer must support start/pause/reset and must not reset or duplicate when navigating away and back.
12. Keep the UI visually simple and functional in this phase. Do not implement all optional modules, elaborate animations, settings customization, multi-monitor polish, installers, or third-party integrations.

## Critical architecture rules

- Dependency direction remains `App → Core`.
- No WinUI, HWND, Win32, or Windows App SDK types in Core.
- No web server, database, account system, telemetry, or remote backend.
- Do not implement fake media data as if it came from Windows. Test doubles must be clearly confined to tests or a documented development mode.

## Deliverables

- Tested Core state machine, neutral media models, and feature-navigation rules.
- Media and Focus Timer available as separate modules selected using Previous/Next controls.
- Working Windows media-session adapter for supported capabilities, or a documented technical spike with the exact blocker if the target API cannot be implemented in this environment.
- Event flow connected to the UI and a test suite covering expected transitions/fallbacks.
- Updated architecture and setup docs when needed.

## Done when

Hover/collapse and feature navigation conform to the UX specification; Media and Focus Timer are both selectable; navigating does not reset module state; unavailable media capabilities fail gracefully; and Core unit tests pass in the supported environment.

## Final report format

1. State model and media integration design.
2. Files created/changed.
3. Build/test commands and actual results.
4. Media capabilities verified vs unsupported/unverified.
5. Remaining bugs/limitations and Phase 4 handoff.
