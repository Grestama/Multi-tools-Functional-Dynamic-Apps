# DynamicTopBar — User & Installation Guide

Welcome to **DynamicTopBar**, a modern, lightweight, and extensible desktop control center for Windows 10 and 11 built with WinUI 3 and .NET 9.

---

## 1. System Requirements & Prerequisites

### Supported Operating Systems
- **Windows 11:** 64-bit (all editions).
- **Windows 10:** 64-bit, Version 1809 (Build 17763) or higher.

### Runtime Dependencies
1. **.NET 9 Desktop Runtime:** Ensure the [.NET 9.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/9.0) is installed.
2. **Windows App SDK 1.6+ Runtime:** Standard on updated Windows 11 systems; on Windows 10, install via the Microsoft Windows App SDK installer or `winget install Microsoft.WindowsAppSDK`.
3. **Privileges:** Standard user privileges. DynamicTopBar **never requires administrative elevation**.

---

## 2. Installation & Quick Start

DynamicTopBar is designed as a clean, portable desktop application with zero registry bloat.

### Portable Deployment
1. Download or generate the production release bundle using `publish.bat` or `publish.ps1`.
2. Extract the `publish/win-x64` directory to your preferred application directory (e.g., `C:\Users\<Username>\AppData\Local\Programs\DynamicTopBar`).
3. Run `DynamicTopBar.App.exe` to launch the top bar capsule.

### Development Runner Scripts
- `run.bat`: Closes any existing instances and runs the app.
- `kill.bat`: Gracefully stops all active DynamicTopBar background processes.
- `test.bat`: Executes the automated regression test suite.

---

## 3. Desktop Interaction Model

### The Capsule (Idle State)
- **Dimensions:** 340 DIP wide × 44 DIP high.
- **Placement:** Centered at the very top edge of your primary display.
- **Focus Behavior:** By design, the capsule uses Windows `WS_EX_NOACTIVATE` window styling. Moving your mouse over the capsule **never steals keyboard focus** from your active working application (IDE, browser, word processor).

### The Expanded Panel
- **Expansion:** Hover over the capsule for 500 ms (or click it) to expand the panel down to 376 × 224 DIPs.
- **Auto-Collapse:** The panel remains expanded while interacting. When the mouse leaves, it smoothly collapses after 4000 ms. Re-entering the panel immediately cancels the collapse timer.

---

## 4. Key Modules & Capabilities

### Baseline Productivity & Media Modules
1. **Media Player:** Windows Global System Media Transport Controls (GSMTC) integration displays current track, artist, album thumbnail, and playback controls.
2. **Spotify Connect:** Web API PKCE OAuth 2.0 integration allows listing connected Spotify devices and transferring playback seamlessly.
3. **Clock & Date:** Real-time 1-second system clock with 12/24-hour preference toggle.
4. **Weather:** Local forecast via Open-Meteo with Celsius or Fahrenheit temperature units.
5. **To-Do List:** Task checklist with completed/active filtering and local atomic JSON storage.
6. **Focus Timer:** Pomodoro technique timer with work/break intervals and visual progress ring.
7. **Quick Launcher:** Customizable pinned application shortcuts with sanitized execution.
8. **System Glance:** Real-time CPU usage, RAM utilization, and battery telemetry.
9. **Calendar & Schedule:** Upcoming 24-hour event agenda glance.
10. **Quick Notes:** Scratchpad note editor with debounced auto-save.
11. **Clipboard History:** Multi-item clipboard snippet manager with automatic sensitive data filtering.

### Universal Tools Workspace (`ToolsWindow`)
Press `Ctrl+Shift+T` or select "Pop Out" (``) from the Tools hub to open the dedicated expansive desktop workspace (1040 × 700 DIPs) featuring 8 categories:
- **Image Tools:** Batch compress JPEG/PNG/WebP, resize with aspect-ratio lock, crop, rotate, flip, and strip EXIF metadata.
- **File Tools:** Batch regex renaming with live collision simulation, ZIP archive creator/extractor, folder size analyzer with symlink recursion guards, and file hash calculators.
- **Developer Utilities:** JSON prettifier/minifier, Base64 encoder/decoder, URL codec, UUID v4 generator, RegEx tester with ReDoS timeout protection, text case formatters, color pickers, QR code generator, and unit converter.
- **Clipboard Manager:** Searchable snippet history with category badges and one-click copy.
- **Workflows:** Sequential multi-step automation chains with parameter substitution (`{prev.output}`).
- **Plugins:** Modular extensions manager with lifecycle state badges (Active/Disabled/Failed) and per-plugin settings.
- **Diagnostics:** Subsystem health checks, process memory/thread telemetry, and sanitized JSON report export.
- **AI Assistant:** Local intent planner with strict opt-in privacy, prompt chips, and safe tool execution.

---

## 5. Keyboard Shortcuts Reference Table

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| `Ctrl+Shift+Space` | Toggle TopBar | Show or hide the top bar capsule overlay. |
| `Ctrl+Shift+P` | Command Palette | Open fuzzy-search command palette for all tools and modules. |
| `Ctrl+Shift+T` | Tools Workspace | Open expansive secondary Universal Tools window. |
| `Ctrl+Shift+M` | Media Glance | Expand top bar directly to Media Player / Spotify. |
| `Ctrl+Shift+F` | Focus Timer | Start or pause the Pomodoro focus timer. |
| `Ctrl+Shift+V` | Clipboard History | Open clipboard snippet history manager. |

---

## 6. Privacy & Security Guarantees

DynamicTopBar is engineered with a **local-first, privacy-by-default** architecture:
- **Zero Telemetry:** No user analytics, error telemetry, or tracking pings are sent to any external server.
- **Strict Opt-In AI:** The AI Assistant is disabled by default (`IsEnabled = false`). All natural language intent parsing runs 100% locally on your CPU; no prompts are ever uploaded to cloud AI providers.
- **DPAPI Credential Protection:** Spotify authentication tokens are encrypted using Windows DPAPI (`CryptProtectData`) tied to your local Windows user account.
- **Secret Redactor:** Exporting diagnostic reports automatically redacts passwords, tokens, API keys, and credit cards before copying to clipboard.

---

## 7. Configuration & File Locations

All user data is stored locally in standard Windows application data directories:
- **Application Preferences:** `%LOCALAPPDATA%\DynamicTopBar\preferences.json`
- **Personalization Profiles:** `%LOCALAPPDATA%\DynamicTopBar\personalization.json`
- **Automated Workflows:** `%LOCALAPPDATA%\DynamicTopBar\workflows.json`
- **Plugin Settings:** `%LOCALAPPDATA%\DynamicTopBar\plugins\{pluginId}.settings.json`
- **To-Do Items:** `%LOCALAPPDATA%\DynamicTopBar\todos.json`
- **Notes Scratchpad:** `%LOCALAPPDATA%\DynamicTopBar\notes.json`
- **Calendar Events:** `%LOCALAPPDATA%\DynamicTopBar\calendar.json`
- **Diagnostic Logs:** `%LOCALAPPDATA%\DynamicTopBar\logs\`

---

## 8. Troubleshooting & FAQ

**Q: Why does the top bar not take focus when I click or type into it?**  
A: By design, the capsule runs in non-activating mode (`WS_EX_NOACTIVATE`) so it never disrupts your active typing in Word, VS Code, or games. When you click into an input box (such as Notes or Search), Windows App SDK handles focused text input specifically within that control.

**Q: How do I completely quit the application?**  
A: Right-click the DynamicTopBar icon in the Windows System Tray (near the taskbar clock) and select **Exit**, or press `Ctrl+Shift+P` and execute `"Quit Application"`, or run `kill.bat`.

**Q: How do I back up or transfer my settings to another computer?**  
A: Open the Universal Tools Workspace (`Ctrl+Shift+T`), navigate to **Diagnostics**, or copy the `%LOCALAPPDATA%\DynamicTopBar\` folder to your new computer.
