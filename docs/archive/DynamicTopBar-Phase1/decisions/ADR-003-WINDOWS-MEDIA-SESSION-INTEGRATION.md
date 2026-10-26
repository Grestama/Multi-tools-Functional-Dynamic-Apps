# ADR-003: Windows Media Session Integration and Core Decoupling

- **Status:** Accepted for Phase 3 implementation.
- **Date:** 2026-10-19

## Context

Phase 3 requires DynamicTopBar to reflect real Windows media session data (title, artist, album art, playback state, and playback controls) and animate between an idle capsule and an expanded media panel.

The architectural constraints in `AGENTS.md` and `docs/ENGINEERING_RULES.md` require:
1. `DynamicTopBar.Core` must be completely platform-independent (no WinUI, Win32, HWND, or WinRT types).
2. The UI must remain responsive and lightweight, preferring event-driven updates over high-frequency polling.
3. Media sessions may be absent, close unexpectedly, lack artwork, or omit certain playback actions (e.g., skip next/previous). The app must handle all these edge cases without crashing.
4. Hover expansion (500 ms delay) and collapse (4000 ms delay) must follow the UX specification with re-entry cancellation and testable timing.

## Decision

1. **Neutral Core Media Models and Interface:**
   - Defined pure C# records and enums in `DynamicTopBar.Core`: `MediaSnapshot`, `MediaTrackInfo`, `MediaTimelineInfo`, `MediaPlaybackActions`, and `MediaPlaybackStatus`.
   - Artwork is represented as a raw `byte[]?` in `MediaTrackInfo` rather than a WinRT or WinUI image type, preserving strict platform independence in Core.
   - Defined `IMediaSessionService` with a `SnapshotChanged` event and asynchronous control operations (`TogglePlayPauseAsync`, `PlayAsync`, `PauseAsync`, `SkipNextAsync`, `SkipPreviousAsync`).

2. **Windows Platform Adapter (GSMTC):**
   - Implemented `WindowsMediaSessionService` in `DynamicTopBar.App.Platform` using the Windows Runtime `GlobalSystemMediaTransportControlsSessionManager` (GSMTC) API.
   - Subscribed to event-driven notifications:
     - `CurrentSessionChanged` on the session manager.
     - `PlaybackInfoChanged`, `MediaPropertiesChanged`, and `TimelinePropertiesChanged` on the active session.
   - Synchronized snapshot generation with a `SemaphoreSlim` to prevent race conditions during rapid track or session transitions.
   - Thumbnail streams are read into byte arrays inside the adapter; failures to read stream data fall back cleanly to `null` artwork without throwing.

3. **Core State Machine and Timer Decoupling:**
   - Implemented `OverlayStateMachine` in `DynamicTopBar.Core.States` implementing `Idle`, `HoverPending`, `Expanded`, and `CollapsePending`.
   - Injected `ITimerService` / `ITimerToken` abstraction:
     - Production uses `DefaultTimerService` wrapping `System.Threading.Timer`.
     - Tests use `TestTimerService` allowing deterministic, instant triggering and verification of cancel tokens without `Thread.Sleep`.
   - Configured baseline delays: 500 ms hover delay and 4000 ms collapse delay.
   - Pointer re-entry cancels collapse immediately; user interaction (clicking buttons) cancels collapse; pointer leaving during hover pending cancels expansion.

4. **UI Integration and Focus Preservation:**
   - Routed snapshot and state machine events to `TopBarWindow` on the UI thread via `DispatcherQueue.TryEnqueue`.
   - Retained `WS_EX_NOACTIVATE` so interacting with media buttons never steals keyboard focus from the active foreground window.
   - Maintained borderless presentation, expanding window height from 44 DIPs to 160 DIPs and width from 220 DIPs to 360 DIPs, re-centering horizontally on the primary display.
   - Fallback music glyph (`\uE8D6`) is displayed when artwork is unavailable.

## Consequences

- Core state machine and media models have 100% deterministic test coverage in `DynamicTopBar.Tests` without requiring Windows App SDK or WinRT runtime.
- Near-zero CPU utilization when idle or paused; media updates are purely reactive and event-driven.
- Complete robustness against missing sessions, app quits, and unsupported media commands.
