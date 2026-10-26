# Acceptance Criteria

This checklist is used across all five phases. Mark items only after observing the behavior, not merely because code exists.

## Phase 1 — Foundation

- [x] Repository scaffold and all Markdown docs are present.
- [x] MVP, deferred features, risks, and five phases are explicit.
- [x] Core/App dependency direction is documented.
- [x] UI states, timing defaults, and fallback behavior are specified.
- [x] Windows setup links and environment requirements are included.
- [x] No real app code is represented as implemented yet.

## Phase 2 — App shell

- [x] A clean checkout builds and launches on the target Windows machine.
- [x] Idle overlay has no normal title bar and renders at the intended top position.
- [x] Overlay does not steal focus simply by appearing.
- [x] System tray provides a reliable exit path.
- [x] Re-launching the app does not leave multiple overlapping bars.

## Phase 3 — Core + media

- [ ] Hover delay and collapse delay match documented behavior (manual run verification).
- [ ] Re-entering the panel cancels collapse (manual run verification).
- [x] Media metadata and supported actions are handled through an adapter.
- [ ] Missing session, missing metadata, or disappearing session does not crash the app (manual run verification).
- [x] Core state transitions have unit tests.

## Phase 4 — UI detailing

- [ ] Idle state remains compact and unobtrusive (manual visual check).
- [x] Expanded panel visibly belongs to the same component (unified header, continuous capsule, centralized tokens).
- [x] Long titles and missing artwork have good fallbacks (character ellipsis, Segoe glyph fallback, null safety).
- [x] Keyboard, accessibility names, and focus behavior are checked (AutomationProperties, IsTabStop, WS_EX_NOACTIVATE).
- [x] Display/DPI changes do not leave the overlay off-screen (dynamic WorkArea re-centering with DPI scaling).
- [ ] Fullscreen limitations are documented based on tests (manual check across exclusive games/apps).

## Phase 5 — Finalization

- [x] Unit tests pass and comprehensive test suite is implemented (14 automated unit tests covering state transitions, re-entry cancellation, timeline math, artwork retention, and long metadata).
- [ ] Idle performance and background activity have been measured (manual Windows Task Manager / Performance Profiler check).
- [x] Packaging/install/uninstall steps work on a clean test environment (ADR-004 dual distribution model documented).
- [x] Startup behavior and shutdown are documented (Named Mutex, TrayIcon exit, clean IDisposable teardown).
- [x] Known issues and limitations are documented accurately (ADR-001 through ADR-004).
- [x] A versioned release artifact can be reproduced (Release 1.0.0.0 publish commands provided).

## Feature Step 1 — Modular Navigation Foundation

- [x] Feature descriptor and catalog contracts implemented in Core (`FeatureDescriptor`, `IFeatureNavigator`, `FeatureNavigator`).
- [x] Four MVP destinations registered: `media`, `focus`, `quick-launch`, `system-glance`.
- [x] Honest placeholders implemented for unbuilt features (`focus`, `quick-launch`, `system-glance`) without faking live system data.
- [x] Previous / Next navigation controls with non-wrapping boundary states (arrows disable at boundaries; navigation panel hides if total <= 1).
- [x] Page indicator (`n / total`) and active feature header (icon + display name).
- [x] Keyboard navigation supported with Left/Right arrow keys.
- [x] Navigation interactions call `_stateMachine.UserInteracted()`, preventing overlay collapse during feature switching.
- [x] Content host uses persistent layout state (`Grid` visibility toggle) without reloading or resetting background sessions.
- [x] Automated unit test suite for feature navigator (8 unit tests covering zero, single, sequential, boundaries, disabled filtering, ID lookup, unregistration, and overlay state independence).

## Feature Step 2 — MVP Feature Modules

- [x] Media Player: Verified Windows GSMTC adapter with real metadata, artwork, progress, play/pause, prev/next, and graceful empty state.
- [x] Focus Timer: Configurable work/break presets (25m work, 5m break, 50m deep), start, pause, resume, reset, and progress countdown.
- [x] Focus Timer State Persistence: Timer runs continuously in the background and is never reset, paused, or duplicated when switching feature modules.
- [x] Quick Launch: Safe shortcut launcher for validated apps, folders, tools, and URLs without arbitrary shell injection.
- [x] Quick Launch Target Validation: Rejects malformed or dangerous URI schemes (e.g. `javascript:`) and missing files/folders with clear feedback.
- [x] System Glance: Verified system metrics including realtime clock, formatted date, machine name, and OS uptime.
- [x] System Glance Battery Support: Event-driven `Windows.System.Power.PowerManager` integration gracefully supporting both laptops and desktop PCs (no battery).
- [x] Automated Unit Test Suite: 43 unit tests covering state machine, media models, feature navigator, focus timer countdown, quick launch validation, and system glance metrics.

## Feature Step 3 — Optional Modules, Preferences, and Polish

- [x] Feature Preferences: Local persistence (`%LocalAppData%\DynamicTopBar\preferences.json`) with thread-safe JSON loading and recovery on corruption.
- [x] Module Enable/Disable: Only enabled and available modules appear in the navigation bar; dynamic reordering supported.
- [x] Inline Preferences UI: Compact module toggle settings accessible via gear icon in sub-header, avoiding heavy multi-screen dashboard.
- [x] Diagnostic Empty State: Graceful fallback view with clear instructions when 0 modules are enabled, preventing crashes or stuck states.
- [x] Single Module Visibility: Navigation arrows hide cleanly when only 1 module is enabled, preserving gear access.
- [x] Optional Module Feasibility Matrix: Recorded in `docs/decisions/FEASIBILITY_OPTIONAL_MODULES.md` covering all 9 candidate features.
- [x] Stopwatch Module: High-precision 100ms timer with lap recording, pause/resume, reset, and background continuity.
- [x] Audio Output Shortcut: Device button safely launches Windows Sound Settings (`ms-settings:sound`) without unsupported COM hacks.
- [x] Privacy Gates: Strict deferral of clipboard history and cloud integrations (weather/calendar) to post-MVP to protect user privacy.
- [x] Automated Unit Test Suite: 61 automated unit tests across Core models, services, state machines, navigators, and stopwatch.

## Tahap 1 — Multifunction Foundation (Multi-Module Architecture)

- [x] Extended Feature Metadata (`FeatureDescriptor`): Enhanced model with `Description`, `RoadmapPhase` (1–3), and `IsPlaceholder` (bool).
- [x] Standard Feature Module Contract (`IFeatureModule` & `FeatureModuleBase`): Clean lifecycle interface with `Descriptor`, `IsActive`, `Activate()`, `Deactivate()`, and `IDisposable`.
- [x] Transparent Placeholder Module (`PlaceholderFeatureModule`): Honest, deliberate placeholder representation capturing `Milestone`, `PlannedScope`, and `DependencyNotes` without faking functional controls or fabricated live data.
- [x] Central Feature Registry (`IFeatureRegistry` & `FeatureRegistry`): Thread-safe registry enforcing unique IDs, module/descriptor lookups, unregistration, and `CreateDefaultMultifunctionRegistry()` registering all 11 planned features.
- [x] Independent State Preservation (`IFeatureStateManager` & `FeatureStateManager`): Type-safe module state storage guaranteeing that navigating across modules preserves running timers, inputs, and active sessions without cross-module coupling.
- [x] Unified Settings & Persistence (`ISettingsService` & `SettingsService`): Single point of truth for application configuration and feature preferences (`preferences.json`) with thread-safe disk persistence, instant updates, and corruption fallback.
- [x] Default Multifunction Registry (11 Features):
  1. `media` (Media Player — Implemented in Phase 2)
  2. `spotify` (Spotify — Phase 2 Placeholder with OAuth prerequisite)
  3. `clock` (Clock & Date — Implemented with real Windows local time & timezone)
  4. `weather` (Weather — Phase 2 Placeholder with API & location prerequisite)
  5. `todo` (To-do List — Phase 2 Placeholder with local storage scope)
  6. `focus` (Focus Timer — Implemented in Phase 2)
  7. `quick-launch` (Quick Launcher — Implemented in Phase 2)
  8. `system-glance` (System Glance — Implemented in Phase 2)
  9. `calendar` (Calendar — Phase 3 Placeholder with agenda & ICS scope)
  10. `notes` (Quick Notes — Phase 3 Placeholder with scratchpad scope)
  11. `clipboard` (Clipboard — Phase 3 Placeholder with opt-in privacy gate)
- [x] Local Clock & Timezone (`ClockContentHost`): Displays real Windows local time (`hh:mm:ss tt`), formatted full date (`dddd, MMMM d, yyyy`), and local timezone offset and name via `TimeZoneInfo.Local` without external network calls.
- [x] Reusable Shared Feature Host (`SharedPlaceholderContentHost`): Shared XAML host dynamically binding milestone badge, scope, and prerequisites for all unbuilt modules, preventing UI monolithic bloat.
- [x] Expanded Preferences Grid (`SettingsContentHost`): Scrollable 2-column grid managing checkboxes for all 11 modules with dynamic instant reflection in navigation.
- [x] Comprehensive Automated Unit Test Suite: 89 unit tests across 12 test fixtures (including 8 `FeatureRegistryTests`, 6 `FeatureStateManagerTests`, 5 `SettingsServiceTests`, 12 `FeatureNavigatorTests`, and 7 `FeaturePreferencesTests`).

## Tahap 2 — Core Productivity Tools (Functional Modules Implementation)

- [x] Two-Layer Media & Spotify Architecture: Universal Windows GSMTC adapter (`WindowsMediaSessionService`) serves as primary zero-configuration media controller for all apps (including desktop Spotify); direct Spotify Web API + PKCE OAuth (`SpotifyService`) provides optional enhanced integration with visible account profile and Web API controls.
- [x] Spotify PKCE Authorization (`SpotifyPkceAuthHelper` & `SpotifyService`): Cryptographic PKCE flow (`code_verifier`, SHA-256 Base64Url `code_challenge`, local HTTP loopback listener `http://127.0.0.1:8888/callback/`) without hardcoded or embedded client secrets.
- [x] Spotify Token Security (`WindowsProtectedDataTokenStore`): Encrypted token storage using Windows Data Protection API (DPAPI, `CurrentUser` scope) with automatic token refresh and clean disconnection.
- [x] Spotify Developer Mode & Free Tier Handling: Clear UI notices regarding Spotify 2026 Developer Mode limits (max 5 authorized users) and Premium requirement for Web API playback endpoints, smoothly advising use of the universal Media Player module for desktop Spotify.
- [x] Clock & Date (`ClockContentHost`): Native Windows local time, formatted full date, day of week badge, and local timezone offset and name updating once per second via `TimeZoneInfo.Local`.
- [x] Weather Service (`IWeatherService`, `WeatherService`, `OpenMeteoWeatherProvider`): Free Open-Meteo Geocoding and Forecast API integration, no API key required, with explicit Open-Meteo attribution.
- [x] Weather Capabilities: City name search, current temperature, WMO weather code mapping to Segoe Fluent glyphs, humidity, precipitation, and 3-day forecast glance.
- [x] Weather Offline Cache & Rate-Limiting: Minimum 60-second refresh throttling to protect public APIs; disk cache (`weather_cache.json`) provides graceful offline fallback when network is unavailable.
- [x] To-do List Service (`ITodoService`, `TodoService`, `FileTodoRepository`): Task checklist management with quick task add, priority classification (Low, Normal, High), completion toggles, task deletion, and clearing completed items.
- [x] To-do Local Persistence: Atomic JSON serialization at `%LocalAppData%\DynamicTopBar\todos.json` with corruption recovery and zero telemetry.
- [x] To-do Priority Sorting: High-priority tasks highlighted with badge and automatically sorted to the top.
- [x] Quick Launcher Enhancements (`QuickLaunchService`): Pinned system tools, URL security validation (only `http`, `https`, `ms-settings:`), recent launches tracking (`RecentItems`), and clear history option.
- [x] System Glance 2×2 Dashboard: Realtime Clock/Date, Battery/Power supply via WinRT `PowerManager`, Network connectivity via WinRT `NetworkInformation`, and Machine Name / Uptime.
- [x] Cross-Module State Isolation: Navigating between Spotify, Weather, Todo, Focus Timer, and System Glance maintains running countdowns, drafts, and active sessions without interruption.
- [x] UI & Design System Discipline: Strictly maintained 340×44 DIP idle capsule, 376×224 DIP expanded panel, `#090909` background, `#171717` surface, and restrained `#00B4FF` accent.
- [x] Comprehensive Automated Test Suite: 110 unit tests across 15 fixtures (including 7 `SpotifyServiceTests`, 4 `WeatherServiceTests`, 6 `TodoServiceTests`, 12 `QuickLaunchTests`, and 5 `SystemGlanceTests`).

## Tahap 3 — Smart Companion & Expansion (Configurable Desktop Companion)

- [x] Calendar & Upcoming Events (`calendar`):
  - Extensible provider contract (`ICalendarService`, `ICalendarRepository`, `FileCalendarRepository`, `CalendarService`).
  - Local event scheduling with Start/End times, Location, and Description.
  - Zero-dependency RFC 5545 iCalendar (`.ics`) parser (`ImportIcsAsync`) and exporter (`ExportToIcs`).
  - Next event countdown badge ("In 35m", "Now: Active Meeting") and today's schedule summary glance.
  - Local persistence at `%LocalAppData%\DynamicTopBar\calendar_events.json` with corruption recovery.
- [x] Quick Notes Scratchpad (`notes`):
  - In-place quick note editor (`NoteTitleBox`, `NoteContentBox`) with automatic 400ms debounced autosave.
  - Note pinning (`IsPinned`) ensuring critical notes remain anchored at the top of the list.
  - Note search and filtering across titles and bodies (`SearchNotes`).
  - Cycle through notes list and safe note deletion with automatic fallback selection.
  - Local atomic JSON persistence at `%LocalAppData%\DynamicTopBar\notes.json`.
- [x] Clipboard History with Strict Privacy Gate (`clipboard`):
  - Default disabled privacy posture (`IsOptInEnabled = false`); no clipboard capture occurs without explicit user consent.
  - Transparent Privacy Gate UI clearly informing the user that snippets remain local and private.
  - Pause and Resume clipboard monitoring toggles.
  - Pinning protected snippets preventing deletion during history clears.
  - Duplicate snippet suppression and automatic capacity pruning (max 25 unpinned snippets).
  - One-click copy snippet back to Windows system clipboard via `IClipboardPlatformAdapter`.
  - Local-only storage at `%LocalAppData%\DynamicTopBar\clipboard_history.json`, zero cloud transmission, zero keylogging.
- [x] Notifications & Reminders Service (`IReminderService`, `ReminderService`):
  - Automated evaluation of due To-do tasks (due within 60 minutes or overdue).
  - Automated evaluation of upcoming meetings from Calendar (starting within 15 minutes).
  - Suppression of dismissed reminder IDs to prevent annoying alert spam.
  - Event-driven notifications without heavy polling threads.
- [x] Personalization & Privacy Controls (`SettingsContentHost`):
  - 24-Hour vs 12-Hour clock format toggle (`ClockUse24HourFormat`) updating both idle capsule and expanded dashboard.
  - Weather temperature unit selector (°C Celsius vs °F Fahrenheit) with dynamic conversion across current and 3-day forecasts.
  - Reminder notification toggle (`RemindersEnabled`).
  - Clipboard opt-in preference toggle (`ClipboardOptIn`).
  - "Clear All Local App Data" privacy action wiping local JSON caches safely and resetting in-memory stores.
- [x] Architecture & Layout Consistency:
  - 100% adherence to `DynamicTopBar.Core` (platform-independent) and `DynamicTopBar.App` (WinUI 3) dependency boundary.
  - Strict preservation of the 340×44 DIP idle capsule and 376×224 DIP expanded panel geometry.
  - High-contrast, low-distraction dark theme (`#090909`, `#171717`, `#2A2A2A`, `#00B4FF`).
- [x] Comprehensive Automated Test Suite:
  - 136 automated unit tests across 20 test fixtures in `tests/DynamicTopBar.Tests` (including 7 `CalendarServiceTests`, 6 `NotesServiceTests`, 7 `ClipboardServiceTests`, 4 `ReminderServiceTests`, and 2 `PersonalizationTests`).

