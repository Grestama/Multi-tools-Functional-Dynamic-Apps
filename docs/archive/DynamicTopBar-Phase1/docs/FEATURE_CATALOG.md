# Feature Catalog — Modular Dynamic Top Bar

## Product rule

DynamicTopBar is a **multi-feature contextual bar**, not a music-only widget. The expanded panel displays one selected feature module at a time. Users move between enabled modules with the previous/next buttons, mouse wheel/trackpad swipe where supported, and keyboard arrows while the panel has intentional keyboard focus.

Feature availability depends on Windows APIs, user permissions, and installed integrations. Never show a control as functional until its service exists and the action has been tested. Hide unavailable features or show a clear unavailable state; never use fake live data.

## Feature tiers

### Tier 0 — Navigation foundation (required)

| Feature | Purpose | Initial behavior | Priority |
|---|---|---|---|
| Feature navigation | Switch between available feature modules | Previous/next arrow buttons, active title/icon, `2 of 4` or page dots | P0 |
| Feature registry | Know which features exist and are enabled | Stable IDs, display labels, icon keys, availability | P0 |
| Remember last feature | Restore the last selected module | Persist a small feature ID locally | P1 |
| Feature settings | Enable/disable and order modules | Add after navigation is stable | P2 |

### Tier 1 — MVP modules

| ID | Module | Useful content/actions | Dependencies | Priority |
|---|---|---|---|---|
| `media` | Media | Artwork, title, artist, progress, play/pause, previous/next if supported | Windows media session API | P0 |
| `focus` | Focus timer | Pomodoro countdown, start/pause/reset, optional session presets | Local timer/state | P0 |
| `quick-launch` | Quick launch | User-pinned apps, folders, URLs, and safe Windows settings shortcuts | User configuration, Shell launch | P1 |
| `system-glance` | System glance | Clock/date, battery when present, basic network/volume status where reliable | Windows system APIs | P1 |

The first functional milestone should support at least **Media + Focus Timer navigation**. Quick Launch and System Glance follow without redesigning the navigation shell.

### Tier 2 — Follow-up modules

| ID | Module | Useful content/actions | Important constraints | Priority |
|---|---|---|---|---|
| `stopwatch` | Stopwatch & timer | Stopwatch, countdown timer, optional alarm sound | Local timer; define behavior while app restarts | P2 |
| `notifications` | Notification glance | Recent notifications or app-generated reminders | Windows notification access is permission/API constrained; validate first | P2 |
| `clipboard` | Clipboard snippets | Explicitly saved recent snippets, copy/paste | Opt-in only, local-only by default; provide pause/clear; do not collect secretly | P2 |
| `audio-output` | Audio output | Show selected output and provide supported volume/device actions | Core Audio interop and device-change handling | P2 |
| `capture` | Capture shortcut | Open Windows Snipping Tool or launch a configured capture flow | Prefer OS-owned capture UX instead of recreating it | P2 |

### Tier 3 — Optional integrations

| ID | Module | Useful content/actions | Dependencies / risks | Priority |
|---|---|---|---|---|
| `calendar` | Calendar glance | Upcoming events, next meeting | Calendar provider/API, sign-in or file import, privacy | P3 |
| `weather` | Weather | Current conditions and forecast summary | Internet/provider; location must be optional and explicit | P3 |
| `download-transfer` | Transfer progress | Progress for downloads or file transfers | Requires supported app/service integrations; no universal Windows feed is assumed | P3 |
| `system-performance` | Performance glance | CPU/RAM/network trend | Sampling budget, clarity on readings; avoid high-frequency polling | P3 |
| `custom-shortcuts` | Custom actions | User-defined actions or links | Restrict unsafe commands; require explicit confirmation for risky actions | P3 |

## Feature boundary: what the app should not promise

- Do not claim universal control of every third-party app.
- Do not assume all Windows notifications can be read without explicit permission/configuration.
- Do not toggle protected/system settings through unsupported hacks. Prefer supported APIs or open the relevant Windows Settings page.
- Do not collect clipboard contents without a clear opt-in, visible status, local retention policy, and one-click clear/pause controls.
- Do not require location for weather; allow a manually entered city.
- Do not build a cloud backend for the MVP.

## Shared module contract

Each feature should declare:

- Stable `Id` (for preferences and navigation).
- `DisplayName` and `IconKey` (use the existing icon library/assets; never emoji as production icons).
- Availability status and a short reason when unavailable.
- Its own ViewModel/service boundary where meaningful.
- Loading, empty, error, and unsupported-action behavior as applicable.
- Unit/manual tests for state changes and capability fallbacks.

Keep the shared overlay/navigation independent from module-specific logic. Avoid one large ViewModel containing every feature's state.

## Suggested navigation UI

The expanded panel contains:

1. A compact header with the selected module icon and title.
2. A left arrow button (`Previous feature`).
3. A right arrow button (`Next feature`).
4. A small page indicator such as `2 / 4` or restrained dots.
5. One module content area that swaps using a short fade/slide transition.

Controls must be real accessible buttons with tooltips/accessibility names. Disable arrows at the first/last module or otherwise make wrap-around behavior explicit and consistent; default behavior is **no wrap-around**. Hide navigation when only one feature is enabled. Keep the pointer inside the entire overlay hit-test region while navigating so it does not collapse unexpectedly.

The active module must not unexpectedly change while the user is interacting with it. Contextual events may update a small activity indicator, but should not steal the selected page or interrupt a timer.
