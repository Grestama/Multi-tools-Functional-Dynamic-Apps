# AI Agent Prompt — Phase 4: UI Detailing & Desktop Behavior

Copy this prompt into your coding agent after Core behavior and a basic media panel work end-to-end.

---

You are implementing **Phase 4 only** of DynamicTopBar. Follow `AGENTS.md` and review the current screens, XAML, resources, ViewModels, architecture, and tests before making UI changes. Improve the existing component; do not replace it with a dashboard or rebuild the app shell.

## Objective

Refine the idle capsule and expanded panel into a polished, restrained desktop component inspired by macOS's visual discipline while respecting Windows typography, system behavior, and accessibility.

## Read first

- `AGENTS.md`
- `docs/PROJECT_BRIEF.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/ENGINEERING_RULES.md`
- `docs/ACCEPTANCE_CRITERIA.md`
- existing UI and design resources

## Visual direction

- Background: `#090909`; secondary surface: `#171717`; border: `#2A2A2A`.
- Primary text: `#F5F5F5`; secondary text: `#A7A7A7`; restrained accent: `#00B4FF`.
- Idle height: 44 effective UI units; capsule corner radius: 22; expanded panel padding: 16.
- Use Segoe UI or a suitable Windows system font. The visual hierarchy should be quiet, compact, crisp, and consistent.
- Reuse XAML resource tokens. Avoid duplicated magic numbers, excessive blur, neon gradients, multiple card layers, oversized controls, or website/dashboard patterns.
- The expanded panel must read as the same bar growing downward, not a separate app window/card disconnected from the header.

## Tasks

1. Inspect current layout and document visible issues before editing. Preserve working state logic and media-service integration.
2. Centralize colors, typography, spacing, corner radii, icon sizes, and motion values in reusable resources.
3. Implement/refine the expanded media panel: square album art with fallback, truncated song title/artist, progress only when valid, and playback controls reflecting available capabilities/status.
4. Make the transition compact → expanded and expanded → compact feel smooth and coherent. Honor the documented 500 ms hover and 4000 ms collapse timings in Core; animate visual properties in the UI without duplicating business timers.
5. Verify pointer movement from header into panel does not accidentally trigger collapse. Keep hover from stealing keyboard focus. Make icon-only buttons accessible and keyboard-reachable when the user intentionally interacts.
6. Add subtle delayed tooltips only when they add value. Do not use emoji as production system icons.
7. Test long metadata, no artwork, no media session, unsupported controls, reduced motion where available, and resizing/DPI changes.
8. Handle monitor topology and position changes to the degree appropriate for the current design. Keep multi-monitor behavior explicit, and do not claim reliable display above every fullscreen mode.
9. Add settings UI only for options needed to validate a polished MVP (e.g., launch visibility or preferred display) and only if stable. Do not introduce deferred widgets.
10. Update UX docs/screenshots if available, and mark acceptance criteria only after visual/manual verification.

## Deliverables

- Polished idle and expanded visuals implemented in the app.
- Reusable design resources and accessible interaction states.
- Verified fallback and display behavior for available test scenarios.
- Updated UX documentation for any intentional deviation.

## Done when

The idle bar remains discreet, the panel visibly belongs to the same component, metadata and controls remain legible, interactions are predictable, and there are no known clipping/unreachable-control issues in the tested configurations.

## Final report format

1. Design changes and key token decisions.
2. Files changed.
3. Build/test commands plus manual viewport/scaling scenarios tested.
4. Accessibility, fallback, monitor, and fullscreen limitations.
5. Open polish items and Phase 5 handoff.
