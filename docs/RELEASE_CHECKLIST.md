# DynamicTopBar — Production Release Checklist & Deployment Guide

> **Target Release:** v1.0.0 (Master Plan Tahap 4: Intelligence, Extensibility & Production Release)  
> **Platform:** Windows 10 (Version 1809+) & Windows 11 (x64)  
> **Target Framework:** .NET 9 (`net9.0-windows10.0.26100.0` for WinUI 3 App, `net9.0` for Core)  
> **Architecture Principle:** In-process desktop modularity (`App → Core`), zero telemetry, strict privacy.

---

## 1. Pre-Release Verification Protocol

Before signing off on any production distribution package, the following automated quality gates must be verified:

| Check | Target / Criterion | Verification Command | Verified Status |
| :--- | :--- | :--- | :--- |
| **Unit Test Suite** | 100% pass (290 tests, 0 skipped, 0 failed) | `dotnet test DynamicTopBar.sln` | [x] PASSED |
| **Core Architecture Boundary** | Zero references to WinUI, Win32, HWND, or Windows App SDK in `DynamicTopBar.Core` | `dotnet build src/DynamicTopBar.Core` | [x] PASSED |
| **Release Build Compilation** | 0 errors, 0 warnings in Release x64 configuration | `dotnet build src/DynamicTopBar.App -c Release -p:Platform=x64` | [x] PASSED |
| **Compiler / Code Analysis** | No unhandled nullability warnings or unused references | `dotnet build -warnaserror` | [x] PASSED |

---

## 2. Target Environment Prerequisites

End-user machines require the following standard Windows desktop prerequisites:

1. **Operating System:** Windows 10 (Build 17763 / Version 1809 or higher) or Windows 11 (all versions), x64 architecture.
2. **.NET Runtime:** [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (x64).
3. **Windows App SDK Runtime:** Windows App SDK 1.6+ runtime redistributable (or installed via MSIX / WinGet).
4. **Permissions:** Standard user privileges (Admin elevation is strictly **not** required).

---

## 3. Production Publish Procedure

DynamicTopBar provides dual automated publish scripts supporting command prompt and PowerShell:

### Option A: Via Command Batch Script
```cmd
publish.bat
```
*Performs test suite execution, cleans output directory `publish\win-x64`, and compiles optimized Release binaries.*

### Option B: Via PowerShell Script
```powershell
.\publish.ps1 -Configuration Release -Platform x64
```

### Option C: Manual CLI Command
```bash
dotnet publish src/DynamicTopBar.App/DynamicTopBar.App.csproj \
    -c Release \
    -p:Platform=x64 \
    --self-contained false \
    -o publish/win-x64
```

---

## 4. Post-Deployment Runtime Quality Checklist

### A. Window Dimensions & Interactivity
- [x] **Idle Capsule Dimensions:** Exact 340 DIP width × 44 DIP height.
- [x] **Expanded Panel Dimensions:** Exact 376 DIP width × 224 DIP height.
- [x] **Topmost & Non-Activating:** Overlay uses `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST` to prevent stealing keyboard focus during hover transitions.
- [x] **Hover Timing:** 500 ms expansion delay, 4000 ms collapse delay; pointer re-entry cancels collapse timer.

### B. Core & Productivity Hub Modules (11 Baseline Modules)
- [x] **Media Player:** Windows GSMTC integration displays current track, artist, thumbnail, and media controls.
- [x] **Spotify Connect:** PKCE OAuth 2.0 Web API authentication and active device playback control.
- [x] **Clock & Date:** Real-time 1-second system clock with 12/24-hour preference toggle.
- [x] **Weather:** Open-Meteo local meteorological forecast with Celsius/Fahrenheit units.
- [x] **To-Do List:** Task completion tracking, prioritization, and local atomic JSON persistence.
- [x] **Focus Timer:** Pomodoro work/break intervals with visual circular progress bar.
- [x] **Quick Launcher:** Process launch adapter with sanitized path execution.
- [x] **System Glance:** Real-time CPU, RAM, and Battery percentages.
- [x] **Calendar & Schedule:** Daily event agenda and schedule glance.
- [x] **Quick Notes:** Scratchpad note editor with debounced auto-save.
- [x] **Clipboard History:** Multi-item clipboard snippet manager with privacy-first sensitive credential masking.

### C. Universal Tools Engine (Tahap 3 & 4)
- [x] **Image Tools:** JPEG/PNG/WebP compression, aspect-ratio preserved resizing, rotation/flip, and metadata stripping.
- [x] **File Tools:** Batch regex renaming, ZIP archive creation/extraction, folder size analysis with reparse-point guard, and file hashing.
- [x] **Developer Utilities:** JSON prettifier/minifier, Base64 encoder/decoder, URL codec, UUID v4 generator, RegEx tester, and Hash calculator.
- [x] **Secondary Tools Window:** Code-only native WinUI 3 Window bypassing XAML compiler bugs (`ToolsWindow.cs`).

### D. Extensibility, Intelligence & Diagnostics (Tahap 4)
- [x] **Plugin System:** In-process modular extensions (`IPlugin`), manifest verification, version compatibility check (`MinAppVersion`), and fault isolation boundary (plugin failure does not crash the host).
- [x] **Automated Workflows:** Multi-step action execution with sequential parameter token substitution (`{prev.output}`), confirmation gates for destructive actions, and atomic JSON persistence (`workflows.json`).
- [x] **AI Assistant:** Deterministic local NLP intent planner mapping queries to registered tools (`IToolRegistry`). Opt-in privacy gate enforced by default.
- [x] **Personalization Engine:** Named profiles ("All-in-One Default", "Deep Focus", "Media Control Studio"), schema version migration (v1 -> v2), and configuration backup export/import.
- [x] **Diagnostics & Health:** Subsystem health checks, resource telemetry (GC memory, process uptime, thread count), and regex-based secret redactor (sanitizing JWT, Bearer tokens, private keys, passwords, and credit card numbers from exported reports).

---

## 5. Security & Privacy Audit Certification

- **Telemetry & Cloud Privacy:** Zero unapproved telemetry or background internet requests. External network calls are strictly confined to user-requested endpoints (Spotify Web API, Open-Meteo Weather) and can be fully disabled in settings.
- **Credential Storage:** Sensitive tokens (e.g. Spotify refresh tokens) use Windows DPAPI (`CryptProtectData`) local encryption.
- **Log Export Safety:** Diagnostic reports automatically redact credentials, API keys, private keys, and tokens prior to clipboard copy or file export.
