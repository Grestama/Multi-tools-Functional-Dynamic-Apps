# Feasibility Evaluation — Optional Modules (Feature Step 3)

## Architectural Principle
DynamicTopBar is a **local, low-distraction Windows desktop component**. We do not add cloud backends, remote servers, accounts, or aggressive polling to implement features. Every optional module must be verified against current supported Windows APIs and target framework capabilities (`net9.0-windows10.0.26100.0`).

---

## Candidate Feasibility Matrix

| Module ID | Feature Name | Feasibility Status | Supported API / Mechanism | Permissions / Privacy | Decision / Fallback |
|---|---|---|---|---|---|
| `stopwatch` | Stopwatch & Laps | **Implemented** | Core High-Resolution Timer (`ITimerService`) | Zero permissions; 100% local | **Active in Step 3**. Supported with high-precision time tracking and lap recording. |
| `capture` | Screen Capture Shortcut | **Supported** | URI `ms-screenclip:` / `SnippingTool.exe` | Standard user action; explicit user-triggered | **Integrated into Quick Launch & verified**. Uses Windows native Snipping Tool. |
| `audio-output` | Audio Device Controls | **Supported (Fallback)** | URI `ms-settings:sound` | Standard user action | Direct device switching uses undocumented COM `IPolicyConfig`; safe release uses `ms-settings:sound`. |
| `clipboard` | Clipboard Snippets | **Deferred** | `Windows.ApplicationModel.DataTransfer.Clipboard` | **High privacy sensitivity**. Requires explicit opt-in, pause toggle, clear all, and local retention policy. | **Deferred to Post-MVP** to guarantee zero background snooping without explicit user opt-in modal. |
| `weather` | Weather Glance | **Deferred** | External Weather REST API | Requires internet, location or city entry, network calls. | **Deferred**. Violates MVP local-only desktop rule. |
| `calendar` | Calendar Glance | **Deferred** | Graph / Outlook / iCal | Requires user account authentication / OAuth. | **Deferred**. Violates MVP local-only rule (no account systems). |
| `notifications` | Notification Glance | **Deferred** | `Windows.UI.Notifications.Management.UserNotificationListener` | Requires Windows capability `userNotificationListener` and explicit user toggle in Windows Settings. | **Deferred**. Prone to permission failures on standard accounts. |
| `system-performance` | CPU/RAM Performance | **Deferred** | `System.Diagnostics.PerformanceCounter` | Continuous polling overhead; sampling CPU counters can increase idle CPU by 1-3%. | **Deferred** to preserve lightweight idle footprint (<0.1% CPU). |
| `transfer-progress` | Download / File Progress | **Deferred** | N/A (no universal Windows API) | Applications (Edge, Chrome, Steam) do not expose a unified public transfer feed. | **Deferred**. Unfeasible without app-specific hooks. |

---

## Mandatory Safeguards Implemented in Step 3

1. **Local Preferences Persistence:**
   - Module order and enabled status are persisted locally to `%LocalAppData%\DynamicTopBar\preferences.json`.
   - Uses stable feature IDs (`media`, `focus`, `quick-launch`, `system-glance`, `stopwatch`).
   - If a saved module is removed, renamed, or corrupted, the service falls back gracefully to defaults.
2. **Graceful Zero/Single Module Handling:**
   - If only 1 module is enabled, Previous/Next navigation controls are automatically hidden.
   - If 0 modules are enabled, the overlay displays a safe diagnostic card rather than crashing.
3. **Accessibility & Focus Discipline:**
   - The overlay preserves `WS_EX_NOACTIVATE` and does not steal keyboard focus during hover.
   - All buttons have `AutomationProperties.Name` and `ToolTipService.ToolTip`.
   - Keyboard navigation via Left/Right arrows allows instant feature switching.
4. **Reduced Motion & Scaling:**
   - Window repositioning uses per-monitor DPI scaling (`GetDpiForWindow`).
   - Animations respect Windows UI easing standards without jarring jumps.
