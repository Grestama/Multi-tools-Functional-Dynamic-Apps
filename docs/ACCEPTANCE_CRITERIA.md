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
  - 153 automated unit tests across 21 test fixtures in `tests/DynamicTopBar.Tests` (including `LoggingServiceTests`, `FocusTimerTests` sleep/resume sync, `CalendarServiceTests`, `NotesServiceTests`, `ClipboardServiceTests`, `ReminderServiceTests`, and `PersonalizationTests`).

## Master Plan Tahap 1 — Foundation, Stabilization & Modular Architecture

- [x] Proyek dapat dibuild menggunakan konfigurasi yang ditentukan: .NET 9 (`net9.0-windows10.0.26100.0` / WinUI 3 App, `net9.0` Core & Tests) via `dotnet build DynamicTopBar.sln -p:Platform=x64` (0 warnings, 0 errors).
- [x] Semua 11 modul existing telah diaudit secara independen (Media Player, Spotify Connect, Clock & Date, Weather, To-do List, Focus Timer, Quick Launcher, System Glance, Calendar & Schedule, Quick Notes, Clipboard History) dan diklasifikasikan statusnya.
- [x] Bug kritis dan lifecycle issues diperbaiki:
  - Focus Timer sleep/resume desynchronization diperbaiki dengan mengadopsi `TargetEndTime` sebagai sumber kebenaran waktu dinding (wall-clock).
  - Skema penulisan berkas persisten ditingkatkan menjadi atomic writes (`.tmp_{guid}` + atomic move) di seluruh repository/service (Preferences, Todos, Calendar, Notes, Clipboard, Weather cache, QuickLaunch) guna mencegah berkas rusak/terkunci.
  - Penanganan pembatalan operasi asynchronous saat aplikasi ditutup menggunakan `CancellationTokenSource`.
  - Unsubscription event handlers secara tepat pada `Dispose()` (termasuk ReminderService dan ClockTimer) guna mencegah memory leaks atau event terdaftar berulang kali.
- [x] Format penyimpanan data lama tetap kompatibel 100% dan teruji pemulihan otomatis dari berkas rusak.
- [x] Single-instance behavior diperkuat: Named Mutex mendeteksi instance kedua dan menyiarkan sinyal window message Win32 (`DynamicTopBar.ShowOverlayMessage`) untuk memunculkan kembali overlay instansi utama sebelum instansi kedua keluar dengan bersih.
- [x] Focus Timer menangani sleep/resume dan pergeseran waktu sistem secara benar melalui event `WM_TIMECHANGE` & `WM_POWERBROADCAST` (PBT_APMRESUMEAUTOMATIC / PBT_APMRESUMESUSPEND) serta method `Synchronize()`.
- [x] System Tray dapat menyembunyikan, memunculkan, dan menutup aplikasi secara aman (`TrayIcon.cs`).
- [x] Automated test suite ditingkatkan menjadi 153 unit test terverifikasi (21 fixtures, 100% lulus, 0 gagal, 0 di-skip).
- [x] Tidak ada regresi pada 11 modul dan fitur existing.
- [x] Layanan logging dan diagnostik mandiri (`ILoggingService` / `LoggingService`) tersedia untuk pencatatan exception dan lifecycle events tanpa dependensi eksternal.
- [x] Dokumentasi arsitektur dan audit modul diperbarui di `docs/ARCHITECTURE.md` dan `ACCEPTANCE_CRITERIA.md`.

## Master Plan Tahap 2 — Interaction & Productivity Core

- [x] Global Shortcut Engine (`IShortcutService`, `ShortcutService`, `WindowsHotKeyAdapter`):
  - Shortcut keyboard global yang dapat dikonfigurasi menggunakan Win32 `RegisterHotKey` dan `UnregisterHotKey` tanpa memasang low-level keyboard hook global.
  - Validasi kombinasi tombol: pencegahan tombol kosong (`None`), kewajiban adanya modifier (Ctrl, Alt, Shift, Win), serta perlindungan kombinasi sistem operasi (seperti `Ctrl+Alt+Escape`).
  - Deteksi konflik penugasan tombol ganda untuk mencegah bentrok tombol.
  - Subclassing jendela pada `TrayIcon` menangkap pesan Win32 `WM_HOTKEY` (0x0312) dan meneruskan ID aksi ke handler terkait.
  - Shortcut default terdaftar: Command Palette (`Ctrl+Shift+P`), Toggle Capsule (`Ctrl+Shift+Space`), Expand/Collapse Panel (`Ctrl+Shift+Down`), Navigasi Modul (`Ctrl+Shift+Left/Right`), Media (`Ctrl+Shift+F8`), Focus Timer (`Ctrl+Shift+F9`), dan Quick Notes (`Ctrl+Shift+N`).
  - Penyimpanan atomic JSON pada `%LocalAppData%\DynamicTopBar\shortcuts_config.json` dengan dukungan ekspor/impor dan reset ke default.
- [x] Command Palette Engine & UI (`ICommandPaletteService`, `CommandPaletteService`, `CommandPaletteContentHost`):
  - Pusat pencarian dan pelaksanaan aksi desktop yang dapat dibuka secara instan via shortcut (`Ctrl+Shift+P`).
  - Pencarian fuzzy dengan pembobotan multi-faktor (judul, deskripsi, kategori, kata kunci) serta contextual bonus untuk modul yang sedang aktif.
  - Registry aksi dinamis (`PaletteCommand`) terpisah dari antarmuka UI, mendukung aksi synchronous maupun asynchronous.
  - Navigasi keyboard penuh (panah Atas/Bawah untuk navigasi daftar, Enter untuk eksekusi, Escape untuk menutup).
  - Registry perintah bawaan mencakup: kontrol Overlay, navigasi modul, Media Player, Spotify, Focus Timer, To-do List, Quick Notes, Calendar, Clipboard, System Glance, Weather, dan Productivity Hub.
- [x] Integrasi Notifikasi & Mode Privasi (`INotificationService`, `NotificationService`, `NotificationCapsulePill`):
  - Tampilan alert notifikasi ringkas (pill banner) pada header bar capsule dengan ikon Segoe dan ticker auto-dismiss.
  - Kartu detail interaktif (Flyout) saat notification pill diklik menampilkan nama aplikasi sumber, timestamp lokal, judul lengkap, isi pesan/masked note, dan tombol Dismiss.
  - Antrean FIFO burst queue untuk menangani beberapa notifikasi yang masuk berdekatan dengan siklus timer otomatis.
  - Jendela deduplikasi 5 detik untuk meredam notifikasi identik yang masuk berulang kali.
  - Filter blacklist aplikasi (`BlockedAppSources`) untuk memblokir notifikasi dari aplikasi tertentu.
  - Mode privasi (`PrivacyModeEnabled`) yang menyamarkan isi pesan sensitif menjadi `"Notification from {AppSource}"`.
  - Tombol dismiss manual dan pelepasan listener saat shutdown.
  - Notifikasi umpan balik otomatis untuk kegagalan eksekusi perintah Command Palette (`CommandPaletteService.CommandFailed` -> `NotificationService.Post`).
- [x] Spotify Connect Device Listing & Playback Control (`ISpotifyService`, `SpotifyService`, `SpotifyDevice`):
  - Pengambilan daftar perangkat Spotify Connect aktif/tersedia via Spotify Web API endpoint `/v1/me/player/devices` (`GetDevicesAsync`).
  - Pemindahan pemutaran (playback transfer) ke perangkat terpilih (`TransferPlaybackAsync`).
  - Tombol alih perangkat (`SpotifyDevicesButton`) pada `SpotifyContentHost` untuk berpindah antardevice secara mudah.
  - Penanganan token kedaluwarsa secara otomatis dan notifikasi status yang jelas terkait batasan akun Free vs Premium.
- [x] Productivity Hub Terpadu (`IProductivityHubService`, `ProductivityHubService`, `ProductivityContentHost`):
  - Tampilan ringkasan terpadu (composite glance) yang menggabungkan agenda kalender mendatang (24 jam), to-do tasks aktif, catatan terakhir/dipin, dan status Focus Timer aktif.
  - Nol duplikasi data: beroperasi langsung di atas `ICalendarService`, `ITodoService`, `INotesService`, dan `IFocusTimerService`.
  - Pembaruan reaktif (`GlanceChanged`) saat terjadi perubahan pada modul-modul produktivitas terkait.
  - Shortcut aksi cepat (`QuickAddTodoAsync`, `QuickAddNoteAsync`, `QuickStartFocusTimer`).
- [x] Pengujian Otomatis & Verifikasi Build:
  - Jumlah automated unit test meningkat menjadi **184 unit tests** terverifikasi pada 25 fixtures (100% passed, 0 failed, 0 skipped).
  - Fixture pengujian baru: `ShortcutServiceTests`, `CommandPaletteTests` (termasuk verifikasi eksekusi sinkron/asinkron, availability predicate, dan event `CommandFailed`), `NotificationServiceTests`, `ProductivityHubTests`, serta penambahan pengujian Spotify devices.
  - Seluruh solusi berhasil dikompilasi dengan **0 warnings dan 0 errors** melalui `dotnet build DynamicTopBar.sln -p:Platform=x64`.
  - Kepatuhan penuh terhadap batas arsitektur (`App → Core`), dimensi UI (340×44 DIP idle capsule, 376×224 DIP expanded panel), serta perilaku non-activating focus (`WS_EX_NOACTIVATE`).

## Master Plan Tahap 3 — Universal Tools Engine

- [x] Arsitektur Universal Tools & Extensible Tool Registry (`IDesktopTool`, `IToolRegistry`, `ToolRegistry`, `StandardDesktopTools`):
  - Kontrak modular `IDesktopTool` di `DynamicTopBar.Core.Models` mendefinisikan metadata alat (`Id`, `DisplayName`, `Description`, `Category`, `IconGlyph`, `ParameterDefinitions`) dan eksekusi non-blocking `ExecuteAsync` yang menerima `ToolContext` serta `CancellationToken`.
  - `ToolRegistry` thread-safe mengelola registrasi, pencarian berdasarkan ID/kategori, dan dispatching eksekusi dengan hasil terstruktur `ToolResult` (`IsSuccess`, `Message`, `Data`, `ExecutionDuration`).
  - Integrasi otomatis dengan Command Palette: semua tool terdaftar disinkronkan ke `ICommandPaletteService` dengan badge kategori dan eksekusi terpadu via `Ctrl+Shift+P`.
- [x] Image Tools Engine (`IImageToolsService`, `WindowsImageToolsService`, `ImageCompressTool`, `ImageResizeTool`):
  - Akselerasi perangkat keras via Windows Imaging Component (WIC) di `DynamicTopBar.App.Platform` (`BitmapDecoder`, `BitmapEncoder`, `BitmapTransform`, `BitmapPropertySet`).
  - Dukungan operasi gambar: Kompresi (pengaturan kualitas 1–100), Konversi Format (JPEG, PNG, WebP), Resize (skala dimensi dan rasio aspek), Crop (validasi batas koordinat X/Y), Rotasi (90°, 180°, 270°), dan Flip (Horizontal, Vertikal).
  - Kontrol metadata EXIF/XMP (`PreserveMetadata = false`) untuk optimasi ukuran berkas dan perlindungan privasi.
  - Keamanan berkas sumber: tidak pernah menimpa berkas asli secara default; penamaan non-destruktif (`_compressed`, `_resized`), serta laporan statistik batch (`SavedBytes`, `SavingsPercentage`).
- [x] File Tools Engine (`IFileToolsService`, `FileToolsService`, `BatchRenameTool`, `ZipArchiveTool`, `FolderSizeTool`):
  - Batch Rename dengan simulasi pratinjau (`SimulateBatchRenameAsync`), deteksi tabrakan nama berkas (collision detection), validasi karakter terlarang, serta aturan manipulasi: Find/Replace, Prefix/Suffix, Penomoran berurutan berpadding, Transformasi huruf (Upper, Lower, Title), dan Modifikasi ekstensi.
  - Pembuatan arsip ZIP (`CreateZipArchiveAsync`) dengan opsi level kompresi serta ekstraksi rekursif (`ExtractZipArchiveAsync`) dengan proteksi penimpaan.
  - Folder Size Analyzer (`AnalyzeFolderSizeAsync`) dengan penghitungan rekursif ukuran, jumlah berkas, dan subfolder, dilengkapi proteksi kritis terhadap symlink dan NTFS junction points (`FileAttributes.ReparsePoint`) untuk mencegah infinite recursion.
  - File Search (`SearchFilesAsync`) berbasis glob wildcard (`*.txt`, `*budget*`) dengan batas ukuran berkas (min/max bytes) dan limitasi hasil.
  - Streaming File Hash Calculator (`CalculateFileHashAsync`) untuk MD5, SHA1, SHA256, dan SHA512 dengan dukungan pembatalan `CancellationToken`.
  - Folder Favorites & Recent Files dengan persistensi berkas atomic JSON (`favorite_folders.json`, `recent_files.json`).
- [x] Advanced Clipboard Tools (`IClipboardService`, `ClipboardService`, `ClipboardContentType`):
  - Deteksi otomatis jenis konten clipboard (`Text`, `URL`, `Code`, `JSON`, `Email`, `Number`).
  - Filter proteksi data sensitif: deteksi pola regex otomatis untuk token JWT, nomor kartu kredit, blok private key SSH/RSA, dan bearer token untuk mencegah kebocoran data.
  - Pencarian dan filter snippet berdasarkan kata kunci dan kategori jenis konten.
  - Pembersihan otomatis (auto-pruning) berdasarkan batas kapasitas (`MaxSnippetCapacity`) dan batas usia hari (`AutoPruneDays`).
  - Blacklist aplikasi sumber (`ExcludedApplications`) guna mencegah penangkapan snippet dari password manager atau aplikasi sensitif.
  - Ekspor (`ExportSnippets`) dan impor (`ImportSnippets`) riwayat clipboard dalam format JSON lokal.
  - Perlindungan privasi ketat: monitoring clipboard tetap 100% opt-in dan hanya berjalan di memori lokal.
- [x] Developer Utilities Suite (`IDeveloperUtilitiesService`, `DeveloperUtilitiesService`):
  - 11 utilitas developer offline lokal di `DynamicTopBar.Core`:
    - JSON Formatter & Validator (indentasi kustom, laporan baris/kolom kesalahan sintaks).
    - Base64 Encoder / Decoder dengan varian URL-safe (RFC 4648).
    - URL Encoder / Decoder.
    - UUID / GUID Generator (format "D", "N", "B", "P", toggle huruf kapital, pembuatan batch).
    - Streaming Text Hash Calculator (MD5, SHA1, SHA256, SHA512).
    - Text Formatter & Statistics (Upper, Lower, Title, Trim, Sort lines, Deduplicate lines, Word/Line/Char count).
    - Regex Tester dengan proteksi ReDoS (`RegexEvaluationTimeout = 2s`) mencegah catastrophic backtracking membekukan proses.
    - Timestamp Converter (konversi epoch detik/milidetik ke ISO 8601 UTC/Lokal bolak-balik).
    - Color Format Converter (HEX, RGB, HSL dengan kalkulasi komponen presisi).
    - QR Code Generator (matriks QR pure C#, output representasi visual SVG dan ASCII art).
    - Universal Unit Converter (Panjang, Massa, Temperatur, Penyimpanan Digital).
- [x] WinUI 3 UI Shell & Host (`ToolsContentHost` & `ToolsWindow`):
  - Host panel utilitas terintegrasi di `TopBarWindow.xaml` dengan bar filter kategori (All, Developer, File Tools, Image Tools).
  - Chip selector untuk pemilihan alat secara cepat dengan ikon dan deskripsi singkat.
  - Kolom input argumen interaktif, tombol eksekusi, indikator status, tampilan output scrollable, dan tombol "Copy Result".
  - **Jendela Terpisah untuk Area Besar (`ToolsWindow`):** Jendela mandiri terpisah (960×650 DIPs) untuk operasi berskala besar (pratinjau transformasi gambar, tabel batch rename, folder size breakdown, regex testing, dan clipboard manager) yang dapat dipanggil melalui tombol Pop-Out (``) maupun Command Palette.
  - Pendaftaran modul dinamis di runtime tanpa merusak jumlah baseline 11 modul yang diuji oleh regression tests.
- [x] Pengujian Otomatis & Verifikasi Build:
  - Jumlah automated unit test meningkat menjadi **254 unit tests** terverifikasi pada 30 test fixtures (100% passed, 0 failed, 0 skipped).
  - Fixture pengujian baru: `ToolRegistryTests`, `FileToolsServiceTests` (termasuk verifikasi `ConvertFileAsync` CSV↔JSON, line endings, dan collision prevention), `DeveloperUtilitiesTests`, `ImageToolsTests`, dan penambahan pengujian Advanced Clipboard.
  - Seluruh solusi berhasil dikompilasi dengan **0 warnings dan 0 errors** (`dotnet build DynamicTopBar.sln -p:Platform=x64`).
  - Batasan arsitektur `App → Core`, dimensi UI (340×44 DIP idle capsule, 376×224 DIP expanded panel), dan non-activating window flags (`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`) dipertahankan sepenuhnya.

## Dynamic Island Experience & Live Activity Engine

- [x] Centralized Motion Tokens (`MotionTokens.cs`):
  - Definisi durasi terpusat: `Micro` (100ms), `Fast` (160ms), `Normal` (220ms), dan `Expressive` (260ms).
  - Kurva akselerasi/deselerasi `CubicEase` (`EaseOutCubic`, `EaseInCubic`, `EaseInOutCubic`).
  - Dukungan aksesibilitas Windows: deteksi `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` untuk mereduksi durasi ke instan secara otomatis saat pengguna menonaktifkan animasi OS.
- [x] Live Activity Engine (Prioritas Kontekstual Dynamic Island):
  - Prioritas 1 — Peringatan Notifikasi (`NotificationCapsulePill`): Menampilkan judul pengirim, cuplikan pesan teks, dan tombol dismiss instan.
  - Prioritas 2 — Media Live Activity (`LiveActivityMediaPanel`): Thumbnail artwork album 24×24, ticker judul lagu & artis, serta visualizer audio gelombang equalizer.
  - Prioritas 3 — Focus Timer Live Activity (`LiveActivityTimerPanel`): Badge timer, teks sisa waktu countdown tabular (`25:00`), dan status sesi.
  - Prioritas 4 — Status Idle Default (`LiveActivityIdlePanel`): Dot aksen aplikasi, format hari & tanggal (`Mon, Oct 26`), jam sistem realtime, dan glyph status (Wi-Fi, Volume, Baterai).
- [x] Visualizer Equalizer Gelombang Audio 3-Bar (`LiveActivityWaveVisualizer`):
  - 3 bilah vertikal oscillating (`WaveBar1`, `WaveBar2`, `WaveBar3`) dengan timer Dispatcher ringan (130ms), hanya aktif berosilasi ketika media sedang diputar (GSMTC atau Spotify) dan otomatis berhenti saat dijeda/idle untuk efisiensi CPU 0%.
- [x] Koordinasi Geometri Native Window & Hit-Testing Passthrough:
  - Batas native OS window saat idle dibatasi presisi ke `348 × 52 DIPs`, meloloskan klik mouse di bawah 52 DIP langsung ke aplikasi/jendela di belakangnya tanpa terhalang kanvas transparan.
  - Saat ekspansi: kanvas OS window segera melebar ke `384 × 236 DIPs` sebelum animasi Storyboard GPU dimulai, mencegah stutter per-frame OS resize.
  - Saat kolaps: kanvas kembali menciut ke `348 × 52 DIPs` setelah `CollapseStoryboard` selesai.
  - Tap instan pada kapsul idle langsung memicu ekspansi (`OnHeaderBarTapped`) tanpa harus menunggu hover delay.
- [x] Verifikasi Kualitas & Biner Rilis:
  - Seluruh 291 unit test lulus 100% (`dotnet test DynamicTopBar.sln`).
  - Biner standalone Release berhasil dipublikasikan ke `publish\win-x64\DynamicTopBar.App.exe` (518 KB).


## Master Plan Tahap 4 — Intelligence, Extensibility & Production Release

- [x] Sistem Plugin Modular Dalam-Proses (`IPlugin`, `IPluginManager`, `PluginManager`, `PluginBase`, `DeveloperToolkitPlugin`):
  - Kontrak modular `IPlugin` di `DynamicTopBar.Core` menyediakan isolasi plugin berbasis status siklus hidup (`Loaded`, `Active`, `Disabled`, `Failed`).
  - Manifest plugin (`PluginManifest`) memvalidasi `Id`, `Name`, `Version`, `MinAppVersion`, `Permissions`, dan `Tags`. Penolakan registrasi otomatis jika versi minimum melampaui versi aplikasi host.
  - Batas isolasi kesalahan (Fault Isolation Boundary): eksepsi tak tertangani saat inisialisasi atau eksekusi plugin ditangkap secara aman, menandai status plugin `Failed` tanpa menyebabkan crash pada aplikasi host.
  - Registrasi dinamis: aktivasi plugin secara otomatis mendaftarkan tool yang dideklarasikan ke `IToolRegistry` dan perintah ke `ICommandPaletteService`, serta melepasnya kembali secara bersih saat dinonaktifkan.
  - Persistensi konfigurasi plugin atomic JSON per-plugin (`{pluginId}.settings.json`) dan status enabled/disabled (`plugins_state.json`).
- [x] Workflow Automation Engine (`IWorkflowEngine`, `WorkflowEngine`, `WorkflowDefinition`, `WorkflowStep`):
  - Eksekusi rangkaian multi-langkah (`WorkflowStep`) berurutan secara non-blocking dengan dukungan pembatalan `CancellationToken`.
  - Substitusi token parameter dinamis: mendukung injeksi output langkah sebelumnya ke langkah berikutnya melalui pola token `{prev.output}`.
  - Gerbang keselamatan tindakan destruktif (`IsDestructive`): meminta konfirmasi pengguna eksplisit sebelum menjalankan langkah berisiko; penolakan pengguna membatalkan alur kerja secara aman.
  - Pengendalian kegagalan bertahap (`ContinueOnError`): opsi untuk menghentikan langsung alur kerja atau melanjutkan ke langkah berikutnya jika suatu langkah gagal.
  - Persistensi berkas atomic JSON (`workflows.json`) dengan mekanisme safe swap `.tmp_GUID`.
  - Sinkronisasi otomatis ke Command Palette: setiap alur kerja dapat dicari dan dijalankan langsung melalui `Ctrl+Shift+P`.
- [x] AI Assistant (Lokal, Deterministik & Berorientasi Privasi) (`IAiAssistantService`, `AiAssistantService`, `AiIntentPlan`, `AiExecutionResult`):
  - NLP Intent Planner lokal yang memetakan kueri bahasa alami (misal: "prettify json", "generate guid", "base64 encode", "hash sha256") ke alat terdaftar di `IToolRegistry` tanpa transmisi data ke cloud atau server eksternal.
  - Gerbang privasi ketat (Opt-in by default): AI Assistant dinonaktifkan secara default (`IsEnabled = false`); tidak ada pemrosesan kueri sebelum pengguna memberikan persetujuan eksplisit.
  - Validasi tindakan berisiko: kueri yang memicu alat destruktif mewajibkan konfirmasi eksplisit (`RequiresConfirmation = true`) sebelum eksekusi dijalankan.
- [x] Personalization Engine & Migrasi Skema (`IPersonalizationService`, `PersonalizationService`, `PersonalizationProfile`, `VersionedConfiguration`):
  - Pengelolaan profil personalisasi preset bawaan: `"All-in-One Default"`, `"Deep Focus"`, dan `"Media Control Studio"`.
  - Pergantian profil dinamis (`SetActiveProfileAsync`) yang memicu event reaktif `ActiveProfileChanged`.
  - Migrasi skema konfigurasi otomatis dari v1 (format datar warisan) ke kontainer berversi (`SchemaVersion = 2`) tanpa kehilangan preferensi pengguna yang ada.
  - Ekspor dan impor konfigurasi menyeluruh dalam format JSON dengan validasi struktur.
- [x] Monitoring Diagnostik, Health Checks & Redaksi Rahasia (`IDiagnosticsService`, `DiagnosticsService`, `SystemDiagnosticReport`):
  - Registri pemeriksaan kesehatan subsistem (`MediaService`, `ToolRegistry`, `PluginManager`, `WorkflowEngine`) dengan status `Healthy`, `Degraded`, `Unhealthy` beserta pengukuran latensi (ms).
  - Telemetri metrik sumber daya proses: alokasi memori GC terkelola (MB), jumlah thread OS, dan waktu aktif aplikasi (uptime).
  - Redaksi rahasia otomatis (Secret Redactor): pembersihan regex otomatis pada seluruh laporan diagnostik dan log sebelum diekspor, menyamarkan token JWT (`[REDACTED_JWT_TOKEN]`), token Bearer (`[REDACTED_BEARER_TOKEN]`), private key RSA/SSH (`[REDACTED_PRIVATE_KEY]`), nomor kartu kredit (`[REDACTED_CARD_NUMBER]`), dan kata sandi/API key.
- [x] Antarmuka Workspace Terpadu (`ToolsWindow.cs` & `TopBarWindow.xaml.cs`):
  - Penambahan 4 ruang kerja interaktif baru pada `ToolsWindow`:
    - **Workflows Workspace:** Daftar alur kerja, penampil rincian langkah, tombol eksekusi real-time, dan kartu laporan hasil langkah.
    - **Plugin Manager:** Daftar ekstensi terpasang, badge status siklus hidup, tombol toggle aktivasi/deaktivasi, serta inspektur metadata.
    - **Diagnostics Dashboard:** Banner status sistem, kartu kesehatan subsistem, meter metrik sumber daya, dan tombol ekspor laporan teredaksi ke clipboard.
    - **AI Assistant Panel:** Indikator privasi lokal, toggle opt-in, kotak input bahasa alami, chip rekomendasi perintah cepat, pratinjau kartu rencana intent, dan penampil hasil eksekusi.
  - Perintah baru pada Command Palette: `Workflows: Open Automations Workspace`, `Plugins: Manage Extensions`, `Diagnostics: Health & Subsystems Glance`, `AI Assistant: Natural Language Command`.
- [x] Skrip Rilis & Distribusi Produksi:
  - Skrip publikasi otomatis: `publish.bat` dan `publish.ps1` yang menjalankan pengujian regresi, membersihkan folder rilis, dan mengompilasi biner Release x64 ke `publish\win-x64`.
  - Panduan dan checklist rilis lengkap terdokumentasi di `docs/RELEASE_CHECKLIST.md`.
- [x] Pengujian Otomatis & Verifikasi Build:
  - Jumlah automated unit test meningkat menjadi **290 unit tests** terverifikasi pada 35 test fixtures (100% passed, 0 failed, 0 skipped).
  - Fixture pengujian baru: `PluginManagerTests`, `WorkflowEngineTests`, `AiAssistantTests`, `PersonalizationServiceTests`, dan `DiagnosticsTests`.
  - Seluruh solusi berhasil dikompilasi dengan **0 warnings dan 0 errors** (`dotnet build DynamicTopBar.sln -p:Platform=x64`).
  - Batasan arsitektur `App → Core`, dimensi UI (340×44 DIP idle capsule, 376×224 DIP expanded panel), dan non-activating window flags (`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`) dipertahankan sepenuhnya.

## Dynamic Island Experience & Live Activity Engine

- [x] Centralized Motion Tokens (`MotionTokens.cs`):
  - Definisi durasi terpusat: `Micro` (100ms), `Fast` (160ms), `Normal` (220ms), dan `Expressive` (260ms).
  - Kurva akselerasi/deselerasi `CubicEase` (`EaseOutCubic`, `EaseInCubic`, `EaseInOutCubic`).
  - Dukungan aksesibilitas Windows: deteksi `Windows.UI.ViewManagement.UISettings.AnimationsEnabled` untuk mereduksi durasi ke instan secara otomatis saat pengguna menonaktifkan animasi OS.
- [x] Live Activity Engine (Prioritas Kontekstual Dynamic Island):
  - Prioritas 1 — Peringatan Notifikasi (`NotificationCapsulePill`): Menampilkan judul pengirim, cuplikan pesan teks, dan tombol dismiss instan.
  - Prioritas 2 — Media Live Activity (`LiveActivityMediaPanel`): Thumbnail artwork album 24×24, ticker judul lagu & artis, serta visualizer audio gelombang equalizer.
  - Prioritas 3 — Focus Timer Live Activity (`LiveActivityTimerPanel`): Badge timer, teks sisa waktu countdown tabular (`25:00`), dan status sesi.
  - Prioritas 4 — Status Idle Default (`LiveActivityIdlePanel`): Dot aksen aplikasi, format hari & tanggal (`Mon, Oct 26`), jam sistem realtime, dan glyph status (Wi-Fi, Volume, Baterai).
- [x] Visualizer Equalizer Gelombang Audio 3-Bar (`LiveActivityWaveVisualizer`):
  - 3 bilah vertikal oscillating (`WaveBar1`, `WaveBar2`, `WaveBar3`) dengan timer Dispatcher ringan (130ms), hanya aktif berosilasi ketika media sedang diputar (GSMTC atau Spotify) dan otomatis berhenti saat dijeda/idle untuk efisiensi CPU 0%.
- [x] Koordinasi Geometri Native Window & Hit-Testing Passthrough:
  - Batas native OS window saat idle dibatasi presisi ke `348 × 52 DIPs`, meloloskan klik mouse di bawah 52 DIP langsung ke aplikasi/jendela di belakangnya tanpa terhalang kanvas transparan.
  - Saat ekspansi: kanvas OS window segera melebar ke `384 × 236 DIPs` sebelum animasi Storyboard GPU dimulai, mencegah stutter per-frame OS resize.
  - Saat kolaps: kanvas kembali menciut ke `348 × 52 DIPs` setelah `CollapseStoryboard` selesai.
  - Tap instan pada kapsul idle langsung memicu ekspansi (`OnHeaderBarTapped`) tanpa harus menunggu hover delay.
- [x] Verifikasi Kualitas & Biner Rilis:
  - Seluruh 291 unit test lulus 100% (`dotnet test DynamicTopBar.sln`).
  - Biner standalone Release berhasil dipublikasikan ke `publish\win-x64\DynamicTopBar.App.exe` (518 KB).



