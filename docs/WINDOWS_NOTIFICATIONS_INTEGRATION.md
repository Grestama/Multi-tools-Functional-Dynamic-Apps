# Windows Notification Integration & Architectural Audit

## 1. Executive Summary

DynamicTopBar integrates notification capabilities into the desktop capsule and expanded panel. In accordance with strict Windows engineering principles, this document provides an honest, empirical audit of the Windows Notification platform, what is natively supported, and the limitations of accessing third-party Windows notifications.

---

## 2. Windows Notification API Audit

Windows 10 and Windows 11 expose multiple notification layers:

### A. Windows App Notifications (Toast Notifications)
- **APIs:** `Microsoft.Windows.AppNotifications` (Windows App SDK) / `Windows.UI.Notifications.ToastNotificationManager`.
- **Capability:** Generates notifications created by DynamicTopBar (e.g. reminders, timer completions, tool progress).
- **Permissions:** Permitted for unpackaged and packaged apps.
- **Privacy:** Fully local; content stays in the Windows Action Center and local process memory.

### B. Universal Windows Notification Listener (`UserNotificationListener`)
- **API:** `Windows.UI.Notifications.Management.UserNotificationListener`.
- **Intended Purpose:** Created for wearable devices and companion devices (e.g. smartwatches) to sync notifications across devices.
- **Platform Constraints & Security Gates:**
  1. **Restricted Capability:** Requires `userNotificationListener` capability declared in `Package.appxmanifest`.
  2. **Explicit User Consent:** Calling `UserNotificationListener.Current.RequestAccessAsync()` requires the user to grant notification access in Windows Settings (`Settings > Privacy & Security > Notifications`).
  3. **Unpackaged Limitations:** In non-packaged Win32 executions, calling `UserNotificationListener.Current` returns `UserNotificationListenerAccessStatus.DeniedBySystem` or throws an `UnauthorizedAccessException`.
  4. **Enterprise / Group Policy:** Often disabled on managed organizational devices.

---

## 3. DynamicTopBar Notification Architecture

To guarantee reliability, avoid false claims, and safeguard user privacy, DynamicTopBar implements a **multi-tiered notification engine**:

```
┌─────────────────────────────────────────────────────────────────┐
│                 Incoming Notification Sources                   │
│                                                                 │
│  [Reminders]      [Focus Timer]    [Background Tools] [External]│
└───────┬─────────────────┬───────────────────┬─────────────┬─────┘
        │                 │                   │             │
        ▼                 ▼                   ▼             ▼
┌─────────────────────────────────────────────────────────────────┐
│           DynamicTopBar INotificationService (Core)             │
│                                                                 │
│  • FIFO Burst Queue (Smooth sequential presentation)            │
│  • 5-Second Sliding Deduplication Window                        │
│  • Application Source Blacklist Filter                          │
│  • Privacy Masking ("Notification from {AppSource}")           │
│  • Configurable Duration & In-Memory Lifecycle                  │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                 WinUI 3 Presentation Tier                       │
│                                                                 │
│  • Capsule Pill Banner (340×44 DIP Header)                      │
│  • Interactive Detail Flyout Card (Full Message & Timestamp)    │
│  • Manual & Auto-Dismiss Lifecycle                              │
└─────────────────────────────────────────────────────────────────┘
```

### Key Architectural Invariants:
1. **No Phantom Interceptions:** DynamicTopBar never claims universal access to every third-party Windows notification without an active, user-granted `UserNotificationListener` permission.
2. **Ephemeral In-Memory History:** Notifications are held in memory during the active session. Sensitive message bodies are **never written to permanent disk storage or telemetry databases** without explicit consent.
3. **Privacy Mode:** When Privacy Mode is toggled on, capsule pills and summary previews mask message bodies into generic notices (`"Notification from {SourceApp}"`), preventing over-the-shoulder snooping in public or office environments.
4. **Burst Queueing & Deduplication:** When multiple notifications arrive in rapid succession, a FIFO queue displays them consecutively with automatic timer cycling, while identical notifications within 5 seconds are silently collapsed.
