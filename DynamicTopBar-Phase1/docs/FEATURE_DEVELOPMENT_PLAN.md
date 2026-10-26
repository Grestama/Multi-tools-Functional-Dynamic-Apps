# Feature Development Plan — Modular Navigation

## Goal

Evolve the single media-oriented prototype into a modular multi-feature Windows top bar while protecting the idle-first UX and keeping each module independently testable.

## Delivery order

### F0 — Shared navigation contract

- Define a stable feature descriptor/model and registry.
- Define selected feature ID, enabled feature list, and navigation commands.
- Add previous/next buttons, page count/dots, accessible names, disabled end states, and a selected-content host.
- Add tests for no feature, one feature, multiple features, invalid persisted ID, and first/last boundaries.
- Keep selection stable while pointer moves between the pill, navigation buttons, and module content.

**Exit gate:** navigation works with at least two placeholder modules and does not collapse due to pointer gaps.

### F1 — First real modules

- Integrate Media through the Windows media-session adapter.
- Implement Focus Timer as local app state, with start/pause/reset and safe timer cancellation.
- Verify that navigating away from Focus does not reset or duplicate its timer.
- Show a lightweight activity badge when media is active, but do not force the user away from the currently selected feature.

**Exit gate:** Media and Focus Timer are both selectable and functional; Core tests cover timer and navigation state.

### F2 — Daily utility modules

- Add Quick Launch with user-pinned apps/folders/URLs.
- Add System Glance (clock/date and system information that can be read through supported APIs).
- Use event-driven updates when available and conservative sampling otherwise.
- Add local preferences for feature enablement/order only after the base navigation is stable.

**Exit gate:** four modules can be enabled/reordered without touching the core overlay state machine.

### F3 — Opt-in and integration modules

- Evaluate notifications, clipboard, audio output, and capture separately.
- Write a brief API/permission spike before implementing any OS-sensitive module.
- Require explicit settings and pause/clear affordances for clipboard-related functionality.
- Keep calendar/weather and third-party service integrations optional.

**Exit gate:** each module has an API feasibility note, permission story, error states, and acceptance tests.

### F4 — Quality, extensibility, and release

- Validate keyboard navigation, reduced motion, display scaling, multi-monitor position, feature absence, and service errors.
- Measure idle CPU/memory and ensure inactive modules do not run unnecessary loops.
- Add a release checklist for each enabled module.
- Update `FEATURE_CATALOG.md` and `UX_SPEC.md` whenever a module is added, moved, deferred, or removed.

## State and navigation rules

- Keep overlay state (Idle / HoverPending / Expanded / CollapsePending) separate from selected feature ID.
- Navigation is only interactive while the panel is expanded or intentionally keyboard-focused.
- Previous/next changes selected feature; it does not open a new window.
- The selected module persists while switching modules unless a reset behavior is explicitly designed.
- Disabled or unavailable modules should not appear as navigable pages by default.
- Navigation button clicks cancel pending collapse while interaction is active.
- If a module raises an error, contain it to its own view and provide retry/diagnostic details; the top bar must stay usable.

## Suggested Core models

Names are illustrative; adapt to the project after reading the existing code.

- `FeatureDescriptor`: stable ID, display name, icon key, availability, order.
- `FeatureNavigationState`: available feature IDs and selected ID.
- `IFeatureCatalog` or `FeatureRegistry`: supplies the feature descriptors.
- `IFeatureNavigationService` (only if required): next/previous/select operations.

Do not put WinUI `UserControl`, XAML types, Win32 handles, or WinRT API objects inside Core models. Keep views and OS adapters in the App project.

## Quality gates per feature

1. User problem and scope are stated.
2. API feasibility and permission requirements are verified.
3. Loading/empty/error/unavailable states are defined.
4. Selection and overlay state do not conflict.
5. Unit tests cover the feature's pure logic.
6. Manual tests cover interaction, keyboard access, scaling, and realistic Windows behavior.
7. Documentation and AI prompts are updated.
