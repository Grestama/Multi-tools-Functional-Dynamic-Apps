# Acceptance Criteria

This checklist is used across all five phases. Mark items only after observing the behavior, not merely because code exists.

## Phase 1 — Foundation

- [ ] Repository scaffold and all Markdown docs are present.
- [ ] MVP, deferred features, risks, and five phases are explicit.
- [ ] Core/App dependency direction is documented.
- [ ] UI states, timing defaults, and fallback behavior are specified.
- [ ] Windows setup links and environment requirements are included.
- [ ] No real app code is represented as implemented yet.

## Phase 2 — App shell

- [ ] A clean checkout builds and launches on the target Windows machine.
- [ ] Idle overlay has no normal title bar and renders at the intended top position.
- [ ] Overlay does not steal focus simply by appearing.
- [ ] System tray provides a reliable exit path.
- [ ] Re-launching the app does not leave multiple overlapping bars.

## Phase 3 — Core + media

- [ ] Hover delay and collapse delay match documented behavior.
- [ ] Re-entering the panel cancels collapse.
- [ ] Media metadata and supported actions are handled through an adapter.
- [ ] Missing session, missing metadata, or disappearing session does not crash the app.
- [ ] Core state transitions have unit tests.

## Phase 4 — UI detailing

- [ ] Idle state remains compact and unobtrusive.
- [ ] Expanded panel visibly belongs to the same component.
- [ ] Long titles and missing artwork have good fallbacks.
- [ ] Keyboard, accessibility names, and focus behavior are checked.
- [ ] Display/DPI changes do not leave the overlay off-screen.
- [ ] Fullscreen limitations are documented based on tests.

## Phase 5 — Finalization

- [ ] Unit tests pass and manual regression checklist is complete.
- [ ] Idle performance and background activity have been measured.
- [ ] Packaging/install/uninstall steps work on a clean test environment.
- [ ] Startup behavior and shutdown are documented.
- [ ] Known issues and limitations are documented accurately.
- [ ] A versioned release artifact can be reproduced.


## Multi-feature navigation acceptance

- [ ] Expanded panel has accessible Previous/Next buttons and a page indicator when at least two features are enabled.
- [ ] Buttons are disabled at the first/last available feature; navigation does not wrap unexpectedly.
- [ ] With only one feature enabled, navigation buttons and page indicator are hidden or sensibly simplified.
- [ ] Feature selection changes content inside the same panel rather than opening another window.
- [ ] Switching features does not reset Focus Timer, duplicate timers, or lose module state unexpectedly.
- [ ] Hovering/moving over navigation controls does not start collapse or lose pointer capture.
- [ ] An unavailable optional feature is omitted from navigation or displays a clear unavailable state. It must not show fake live data.
- [ ] Navigation is keyboard-accessible only when focus is intentionally inside the panel; global arrow-key interception is not used.
- [ ] Reduced-motion preference is honored for feature transitions where available.
