# ADR-004: Release Packaging and Distribution Strategy

- **Status:** Accepted for Phase 5 implementation.
- **Date:** 2026-10-19

## Context

Phase 5 requires establishing a reproducible packaging, release, and distribution strategy for DynamicTopBar on Windows 10/11.

Key constraints:
1. This is a local Windows desktop utility without a cloud backend, account system, or remote server.
2. The solution must support fast local development iteration (`dotnet run`) and clean standalone distribution without requiring expensive commercial code-signing certificates.
3. Privacy rules require zero telemetry, zero analytics trackers, and zero background network calls.

## Decision

1. **Dual Distribution Model:**
   - **Primary Distribution: Portable Self-Contained Binary (`win-x64`)**
     - Compiled via `dotnet publish src/DynamicTopBar.App/DynamicTopBar.App.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -p:PublishSingleFile=false`.
     - Ships as a portable ZIP containing `DynamicTopBar.App.exe` and its dependencies.
     - Requires no elevated administrator installation or developer certificates.
   - **Secondary Distribution: MSIX Packaged App (`Package.appxmanifest`)**
     - Single-project MSIX enabled via `Microsoft.WindowsAppSDK` and `Microsoft.Windows.SDK.BuildTools.WinApp`.
     - Package identity `DynamicTopBar`, Version `1.0.0.0`, Publisher `CN=DynamicTopBarDev`.
     - Supports clean install, seamless OS startup registration, and 100% clean uninstall via Windows Settings.

2. **Strict Privacy Policy:**
   - Zero network connectivity requested in capabilities (no `internetClient` capability in `Package.appxmanifest` unless explicit remote streaming features are ever introduced).
   - Zero telemetry libraries or crash-reporting servers.
   - Crash diagnostics are strictly local, written to `app_error.log` in the local application folder upon unhandled exceptions.

3. **Single-Instance and Lifecycle Safety:**
   - Named system Mutex (`DynamicTopBar.SingleInstance.Mutex.7B0F33A9`) prevents duplicate overlapping bars.
   - Reliable exit path via system tray icon context menu ("Quit DynamicTopBar"), releasing mutex and disposing all WinRT / GSMTC listeners cleanly.

## Consequences

- Reproducible release builds that any developer with .NET 9 SDK can compile in under 30 seconds.
- Fully private and safe for enterprise and personal desktop environments.
- Completely clean uninstall without orphaned registry keys or background services.
