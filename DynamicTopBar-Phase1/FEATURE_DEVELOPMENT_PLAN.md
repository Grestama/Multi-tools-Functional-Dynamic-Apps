# Feature Development Plan — 3-Step Modular System

## Product goal

DynamicTopBar is a **modular Windows desktop top bar**, not a music-only widget. The expanded surface shows one selected feature at a time. Users move between enabled features with accessible Previous/Next controls; the top bar's idle/expanded lifecycle remains independent of the selected feature.

This three-step plan is the **feature implementation track**. It complements—not replaces—the repository's five engineering phases for foundation, app shell, core services, UI detailing, and release. Run the relevant feature prompt only after the repository has the app/Core structure needed by that task.

## Feature tiers

### P0 — Navigation foundation

- Feature descriptor/catalog with stable IDs and display names/icons.
- Previous/Next buttons, page indicator, selected-content host, keyboard support.
- Four registered MVP destinations: Media, Focus Timer, Quick Launch, System Glance.
- Placeholder pages are acceptable in Step 1 only and must be visually or textually identified as not yet implemented.
- No wrap-around by default; disable boundary arrows. Hide arrows if only one feature is available.

### P1 — MVP modules

| ID | Module | MVP scope | Dependency |
|---|---|---|---|
| `media` | Media Player | Metadata/artwork if available, playback state, progress and supported playback actions | Windows media-session API |
| `focus` | Focus Timer | Configurable work/break duration, start, pause, reset, countdown | Local Core timer/state |
| `quick-launch` | Quick Launch | User-pinned apps, folders, and URLs | Local settings and supported Windows Shell launch |
| `system-glance` | System Glance | Clock/date, battery when present, and only system indicators supported by verified APIs | Windows system APIs |

### P2/P3 — Optional modules

Candidates: stopwatch/countdown timer, app-generated reminders, notification glance, opt-in clipboard snippets, audio output/device actions, capture shortcut, calendar, weather, transfer progress, system performance, and custom shortcuts.

Optional modules are not guaranteed to be feasible in every Windows environment. Each must get an API/permission feasibility check before implementation. Calendar/weather may use external data providers; they remain opt-in and must not be a dependency of the MVP. Do not add a cloud backend to solve local feature requirements.

## Step 1 — Modular Navigation Foundation

### Build

1. Audit the actual repository and existing app shell; establish baseline build/test status.
2. Define a small, stable feature descriptor/catalog contract (stable ID, label, icon key, order, availability/reason where needed).
3. Keep platform-independent navigation rules in Core. Keep WinUI views and concrete services in App.
4. Implement Previous/Next, selected title/icon, `n / total` or restrained dots, selected-content host, accessible names/tooltips, boundary states, and a short low-distraction transition.
5. Register the four MVP destinations using honest placeholders only; do not fake live metadata or system status.
6. Keep `OverlayState` (Idle/HoverPending/Expanded/CollapsePending) separate from `SelectedFeatureId`.
7. Add unit tests for zero, one, multiple, unavailable, invalid persisted ID, first/last navigation, and stable selection while hovering/clicking.

### Acceptance gate

- Navigation works across at least two destinations in a real running app.
- Arrows never accidentally close the panel or reset an unrelated feature state.
- No feature-specific OS API is required for the navigation foundation.
- Core tests and app build pass in the supported environment, or exact environment blockers are documented.

## Step 2 — MVP Feature Modules

Implement in this order, keeping each module isolated and testable:

1. **Media Player:** use a verified supported Windows media-session API; provide a clear no-session state; show only actions the session supports.
2. **Focus Timer:** local timer state with start/pause/reset and sensible work/break presets. Navigating away must not reset, duplicate, or pause the timer unexpectedly.
3. **Quick Launch:** allow the user to configure apps, folders, and URLs. Validate configuration and fail safely if a target is missing. Never execute arbitrary commands from untrusted content.
4. **System Glance:** clock/date and verified system status only; battery is optional on devices without one. Avoid high-frequency polling where events or low-rate updates suffice.

Every module must implement its own view state and service boundary where appropriate, expose loading/empty/error/unavailable behavior, and avoid one giant ViewModel that owns all modules. Avoid fake media/system data outside explicit test doubles.

### Acceptance gate

- All four modules can be reached with Previous/Next.
- Media metadata/actions are sourced from a real supported API or shown unavailable; no fake live state.
- Focus timer continues correctly when another page is selected.
- Quick Launch uses user-configured targets and handles invalid targets safely.
- System Glance renders only supported readings and handles devices without a battery.
- Core tests and manual smoke checks pass, or remaining API/environment gaps are explicitly documented.

## Step 3 — Optional Modules, Preferences, and Product Polish

### Build in small increments

1. Add feature preferences: enable/disable and reorder modules; persist locally. Only enabled/available modules appear in navigation.
2. Evaluate optional modules one at a time. Before coding each one, record API feasibility, permission/privacy implications, dependencies, expected states, and acceptance tests.
3. Recommended optional order: stopwatch; capture shortcut; audio output controls; app-generated reminders; notification integration; clipboard snippets (opt-in only); calendar/weather integrations; system performance/transfer integrations if an actual supported source exists.
4. Add keyboard navigation, focus behavior, reduced-motion support, DPI scaling, multi-monitor positioning, localization-ready labels, and clear unavailable/error states.
5. Test startup/shutdown, event cleanup, timer cleanup, idle CPU/memory, and regression between features.
6. Update screenshots/docs/feature catalog and packaging/release notes only for implemented and verified behavior.

### Privacy and safety gates

- Clipboard capture must be explicitly opt-in, local-only by default, visibly enabled, pausable, and easy to clear.
- Location for weather must be optional; manual city input should be available.
- Calendar access must be explicit and use a documented provider/import mechanism.
- Notification and audio controls must use supported Windows APIs, not unsupported hooks or process memory tricks.
- Custom shortcuts must not silently execute arbitrary shell commands; validate targets and require confirmation for risky actions.
- Do not claim universal support for all third-party apps, all notifications, or exclusive-fullscreen overlays.

### Acceptance gate

- Optional modules can be disabled without breaking the app or core navigation.
- Preferences persist and invalid/stale module IDs recover safely.
- No optional module blocks the entire UI when unavailable or failing.
- Documented tests cover interaction, accessibility, performance, privacy controls, and release packaging.

## Shared navigation contract

- Stable feature IDs: `media`, `focus`, `quick-launch`, `system-glance`; optional IDs are documented before use.
- Previous/Next modifies selected feature only; it does not open a new window.
- No wrap-around by default. Disable Previous on the first page and Next on the last page.
- Hide navigation when only one feature is available.
- Pointer travel between capsule, arrows, and expanded content must not trigger unintended collapse.
- Context changes (e.g. music playback starts) may update an activity indicator but must not steal the selected page.
- Keep each module independent. An exception in one module must not make the shell unusable.
- Do not put WinUI, HWND, Win32, or Windows App SDK types in Core.

## Required AI-agent handoff report

At the end of each step, report:
1. Existing state found before editing.
2. Files created/changed.
3. Implementation decisions and APIs verified.
4. Exact build/test/manual-check commands and their observed results.
5. Working, unavailable, and unverified capabilities.
6. Remaining issues and whether the acceptance gate is met.

Never mark a gate passed based only on source-code presence or an agent's own assertion.
