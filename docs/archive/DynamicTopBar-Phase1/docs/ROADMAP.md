# Five-Phase Implementation Roadmap

## Phase 1 — Foundation and documentation (current)

**Goal:** establish a single source of truth before writing application code.

Deliverables:
- Repository and folder convention.
- Product scope, MVP and non-goals.
- Technical architecture and dependency boundaries.
- UI behavior specification and state transitions.
- Coding, testing, and AI-assisted development rules.
- Windows environment checklist and acceptance criteria.
- Initial architecture decision record (ADR).

Exit criteria:
- The MVP and out-of-scope features are unambiguous.
- The team can explain how UI, Core, and OS adapters communicate.
- Development environment prerequisites are checked.
- No contradictory requirements remain in the Markdown docs.

## Phase 1A — Feature catalog and navigation specification (part of Foundation)

**Goal:** make multi-feature scope explicit before the app shell is coded.

Deliverables:
- `FEATURE_CATALOG.md` with prioritized feature candidates and integration constraints.
- `FEATURE_DEVELOPMENT_PLAN.md` with delivery order and quality gates.
- UX rules for previous/next buttons, page indicator, selection persistence, and module isolation.

Exit criteria:
- The initial navigation contract is clear.
- MVP initial modules are Media and Focus Timer; Quick Launch and System Glance follow.
- Optional modules with permissions/provider dependencies are clearly deferred.

## Phase 2 — WinUI app shell and idle overlay

**Goal:** produce a real, runnable native Windows app before integrating media.

Tasks:
- Create the WinUI 3 packaged desktop project using a currently supported template.
- Add the solution and baseline build configuration.
- Confirm build/run/debug on the target Windows machine.
- Implement app startup, single-instance policy, window/lifecycle cleanup, and system tray foundation.
- Create a borderless compact overlay window and a simple static idle bar.
- Validate top position, z-order, focus behavior, DPI, and exit path.

Exit criteria:
- A clean checkout builds.
- App can launch and quit predictably.
- Idle bar renders without a title bar and without stealing focus.

## Phase 3 — State machine and OS/media integration

**Goal:** make the component respond to actual events.

Tasks:
- Implement the explicit Idle → HoverPending → Expanded → CollapsePending state machine.
- Implement hover/collapse timing with cancellation when pointer re-enters.
- Create interfaces for media-session access and window operations.
- Spike the Windows media-session API on the selected .NET/Windows App SDK target.
- Map metadata and supported media controls into neutral Core models.
- Add reliable fallbacks when metadata/controls are missing.

Exit criteria:
- Hover and collapse behavior follows UX_SPEC.
- Media status updates the UI without tight polling loops.
- Media integration failures do not crash the app.

## Phase 4 — Visual polish and product details

**Goal:** achieve a coherent, high-quality desktop experience.

Tasks:
- Apply design tokens, spacing, typography, icons, surfaces, borders, and shadows.
- Add transitions between compact and expanded layouts.
- Build album art fallback and metadata truncation rules.
- Add keyboard/focus/accessibility behavior.
- Add settings for position/visibility and optional startup behavior if stable.
- Handle display changes, monitor boundaries, taskbar arrangements, and scaling.
- Validate fullscreen scenarios and document expected limitations.

Exit criteria:
- Idle state is visually unobtrusive.
- Expanded state looks like the same component, not a separate dashboard.
- No clipping or unreachable controls at tested display scales.

## Phase 5 — Test, package, and release

**Goal:** make the app dependable to install, use, and troubleshoot.

Tasks:
- Unit-test Core state transitions and metadata mapping.
- Run manual UI/monitor/focus regressions.
- Measure idle CPU and memory; investigate unnecessary timers and event leaks.
- Add structured local diagnostics without collecting personal data.
- Package and test clean installation/uninstallation.
- Document startup options, known limitations, and troubleshooting.
- Tag a version and keep a short release checklist.

Exit criteria:
- Acceptance criteria in ACCEPTANCE_CRITERIA.md pass.
- Build and install process is repeatable.
- Known limitations are documented honestly.
