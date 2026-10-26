# Project Brief

## 1. Purpose

Build a lightweight Windows desktop overlay with two main visual states:

- **Idle:** a slim, centered capsule at the top of the primary display, carrying a small activity indicator and carefully selected status information.
- **Expanded:** the same visual element grows downward to reveal contextual content, initially a now-playing music panel.

The design reference is macOS's restraint and visual hierarchy, adapted to Windows conventions. It is not intended to reproduce Apple's menu bar pixel-for-pixel.

DynamicTopBar is a local desktop utility executing entirely within a single native client process. There is **no remote backend, web server, cloud database, user account system, or telemetry pipeline** in the MVP; "backend" in this project refers strictly to local Core business logic, in-process state management, and Windows platform adapters.

## 2. Target user and problem

Users who want quick access to a small amount of contextual information without keeping a full app window open. The UI should not compete with the Windows taskbar or cover important work.

## 3. MVP scope

### Included

- One compact floating bar.
- Hover delay before expansion.
- Expanded music widget with artwork, title, artist, progress, previous, play/pause, and next controls where the active media session supports them.
- Collapse delay after the pointer leaves.
- System tray entry for show/hide, settings placeholder, and quit.
- Configurable launch behavior and display position can be added after the main interaction is stable.
- Graceful fallback when metadata or a media control is unavailable.

### Deferred (Non-Goals)

- **Out-of-scope widgets and integrations:** Notifications, calendar, clipboard, weather, AI features/assistants, and third-party plugin marketplaces/ecosystems.
- **Out-of-scope infrastructure:** Remote backends, web APIs, cloud synchronization, telemetry servers, and user accounts.
- **Out-of-scope styling:** Custom themes beyond a small set of defined tokens.
- **Out-of-scope distribution:** Installer auto-update pipelines.

## 4. Baseline interaction values

These are starting defaults, not immutable constants:

- Idle bar height: 44 px effective UI units.
- Hover-to-expand delay: 500 ms.
- Collapse delay after pointer leaves: 4 seconds.
- Expanded panel: one compact panel, growing downward from the idle bar.
- Motion: subtle ease-out; no overshoot or springy bounce by default.
- Reduced-motion preference should disable or shorten nonessential movement where practical.

Note: WinUI layout units are effective pixels (DIPs); do not assume they equal physical screen pixels under every display scale.

## 5. Quality goals

- No unnecessary focus stealing.
- No continuous high-frequency polling when event-based updates are possible.
- No crashes when a media session disappears, an album-art image fails, or a monitor is disconnected.
- Avoid duplicate app instances when practical; second launch should activate the existing app.
- Accessible names for interactive controls and visible keyboard focus where relevant.
- The user can reliably quit the background application.

## 6. Constraints and risks

- Media session APIs may expose different metadata/capabilities depending on the player and Windows version. Validate this early with a technical spike.
- Always-on-top ordering is a windowing behavior, not a guarantee of visibility over every fullscreen mode.
- Per-monitor DPI, display changes, taskbar placement, and multiple monitors need explicit testing.
- Desktop overlay hit-testing and click-through behavior require careful implementation; never make the panel impossible to interact with or dismiss.

## 7. Success definition

A user can launch the app, see a slim idle bar, hover to reveal a coherent music panel, use available playback controls, move the pointer away and see it collapse after the configured delay, and quit it from the system tray. The idle state must remain visually minimal.
