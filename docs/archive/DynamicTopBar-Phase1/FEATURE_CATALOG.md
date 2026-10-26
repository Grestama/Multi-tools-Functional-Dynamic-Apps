# Feature Catalog — Modular Dynamic Top Bar

## Product rule

DynamicTopBar is a **multi-feature contextual bar**, not a music-only widget. The expanded panel displays one selected feature module at a time. Users move between enabled modules with the previous/next buttons, mouse wheel/trackpad swipe where supported, and keyboard arrows while the panel has intentional keyboard focus.

Feature availability depends on Windows APIs, user permissions, and installed integrations. Never show a control as functional until its service exists and the action has been tested. Hide unavailable features or show a clear unavailable state; never use fake live data.

## Skema Pengembangan 3 Tahap

DynamicTopBar dikembangkan dalam tiga tahap bertahap agar arsitektur modular tetap kokoh dan setiap fitur dapat diuji secara independen.

### Tahap 1 — Multifunction Foundation (Selesai)
Fondasi sistem modular terpadu: navigasi multi-fitur, registrasi modul sentral (`IFeatureRegistry`), state management terisolasi (`IFeatureStateManager`), preferensi pengguna (`ISettingsService`), dan host placeholder terpadu (`SharedPlaceholderContentHost`).

| Komponen | Tujuan | Perilaku & Implementasi | Status |
|---|---|---|---|
| Navigasi Fitur | Berpindah antar modul | Tombol Previous/Next, header aktif (ikon + judul), indikator `n / total`, keyboard panah kiri/kanan | **Implemented** |
| Feature Registry | Registrasi terpusat modul | Validasi ID unik, metadata descriptor, default 11 modul terdaftar | **Implemented** |
| State Management | Isolasi state antar modul | Timer/state tidak tereset saat navigasi berpindah; tersimpan per-feature ID | **Implemented** |
| Preferensi Pengguna | Aktifkan/nonaktifkan modul | Grid preferensi 11 modul, penyimpanan lokal JSON `%LocalAppData%\DynamicTopBar\preferences.json` | **Implemented** |
| Shared Placeholder Host | Tampilan modul belum dibangun | Kartu terpadu menampilkan milestone, ruang lingkup, dan dependensi tanpa data tiruan | **Implemented** |

### Tahap 2 — Core Productivity Tools (Fokus Utama)
Modul produktivitas harian yang terintegrasi langsung dengan API lokal Windows atau layanan eksternal.

| ID | Modul | Fitur & Konten | Dependensi & Catatan | Status |
|---|---|---|---|---|
| `media` | Media Player | Metadata lagu real-time, album art, playback controls (GSMTC) | Windows Media Session API | **Implemented** |
| `spotify` | Spotify | OAuth akun, metadata personal, remote playback Connect | Spotify Web API & redirect flow lokal | **Phase 2 Placeholder** |
| `clock` | Clock & Date | Jam digital real-time, tanggal lengkap, zona waktu Windows lokal | Windows `TimeZoneInfo.Local` & clock timer | **Implemented** |
| `weather` | Weather | Suhu terkini, kondisi cuaca, probabilitas hujan, perkiraan cuaca | Weather API provider & input lokasi manual | **Phase 2 Placeholder** |
| `todo` | To-do List | Daftar tugas, checklist, prioritas, deadline | Penyimpanan lokal JSON (tanpa akun cloud) | **Phase 2 Placeholder** |
| `focus` | Focus Timer | Pomodoro timer (25m/5m/50m), countdown latar belakang | Timer lokal independen | **Implemented** |
| `quick-launch` | Quick Launcher | Pintasan cepat Explorer, Browser, Terminal, Kalkulator, Task Manager | Verifikasi skema aman & `Process.Start` | **Implemented** |
| `system-glance` | System Glance | Status baterai via WinRT `PowerManager`, nama PC, system uptime | Windows WinRT & System APIs | **Implemented** |

### Tahap 3 — Smart Companion & Expansion (Ekstra & Masa Depan)
Modul pelengkap yang meningkatkan fleksibilitas dan kenyamanan desktop companion.

| ID | Modul | Fitur & Konten | Dependensi & Catatan | Status |
|---|---|---|---|---|
| `calendar` | Calendar | Kalender bulanan, agenda hari ini, countdown rapat | Integrasi provider kalender atau sinkronisasi .ics | **Phase 3 Placeholder** |
| `notes` | Quick Notes | Scratchpad cepat, auto-save teks/markdown | Penyimpanan file lokal dengan debouncing | **Phase 3 Placeholder** |
| `clipboard` | Clipboard Manager | Riwayat cuplikan teks, pin favorit, filter teks/link | Wajib persetujuan opt-in privasi eksplisit | **Phase 3 Placeholder** |
| `notifications`| Notifications | Glance notifikasi penting, filter prioritas | Dibatasi perizinan Windows Notification Listener | **Direncanakan** |
| `app-usage` | App Usage | Screen time, statistik waktu fokus | Sampling hemat daya tanpa analitik eksternal | **Direncanakan** |
| `personalization`| Personalization | Kustomisasi modul, layout kustom, tema warna | Ekstensi tema WinUI & resource dictionary | **Direncanakan** |

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
