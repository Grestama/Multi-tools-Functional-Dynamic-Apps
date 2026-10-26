# AI Agent Prompt — Phase 2: WinUI App Shell & Idle Overlay

Copy this prompt into your coding agent with the repository root open on a Windows development machine.

---

You are implementing **Phase 2 only** of DynamicTopBar. Follow `AGENTS.md` and read the active project documents before editing. Inspect the actual workspace and preserve any existing work.

## Objective

Turn the documentation scaffold into a real, buildable, runnable native Windows application. Establish the shell and static idle bar before media integration.

## Read first

- `AGENTS.md`
- `docs/PROJECT_BRIEF.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/ENGINEERING_RULES.md`
- `docs/SETUP_WINDOWS.md`
- `docs/ACCEPTANCE_CRITERIA.md`
- `docs/decisions/ADR-001-TECH-STACK.md`

## Preflight before code

1. Inspect the OS, available .NET SDKs, Visual Studio/WinUI tooling, existing `.sln`/`.csproj` files, and Git status.
2. Use the current stable Microsoft WinUI 3 / Windows App SDK template supported by the installed toolchain. Confirm the target framework compatibility before selecting versions.
3. If this environment is not Windows or lacks the required workload, do not fabricate a successful build. Implement only changes that can be validated, then list the exact setup/build commands the user must run on Windows.
4. Record significant template choices or constraints in an ADR instead of silently changing the architecture.

## Tasks

1. Create the solution and a real WinUI 3 desktop app under `src/DynamicTopBar.App` (or use the smallest template-required layout adjustment and document it). Add `DynamicTopBar.Core` and a compatible test project only if the chosen setup can support them cleanly.
2. Confirm the blank app builds and launches before adding the overlay. Then implement app startup and orderly shutdown.
3. Implement a borderless top-level overlay window for the primary display, centered horizontally with a small top inset. Render only a static idle capsule using tokens from `UX_SPEC.md`.
4. Configure window z-order and activation behavior carefully: the bar must not steal focus merely by appearing. Avoid large invisible hit-test regions and keep the interactive region intentional.
5. Establish a reliable system tray/quit path and basic show/hide behavior. Prevent or gracefully handle duplicate launches; do not introduce a complex singleton mechanism unless needed and verified.
6. Add cleanup for event subscriptions, windows, timers, and tray resources at shutdown.
7. Set up repeatable build/run instructions in `README.md` or `docs/SETUP_WINDOWS.md`. Do not integrate media sessions, album artwork, hover expansion, settings UI, or installer packaging yet.
8. Update Phase 2 acceptance criteria only after observing the corresponding behavior.

## UX baseline

- Idle capsule target height: 44 effective UI units.
- Dark neutral surface, subtle border, no normal title bar.
- Top-center position on primary display as the initial policy.
- No media widget yet; no expanded state yet.
- Native Windows conventions; do not reproduce Apple-only menu items.

## Deliverables

- A real solution/project that can build in the selected Windows toolchain.
- Static idle overlay, startup/shutdown, and reliable tray exit.
- Documented build/run commands and any tested SDK versions.
- Minimal code; no unrelated abstractions or decorative features.

## Done when

A clean checkout builds, the app launches, shows the idle overlay without a standard title bar or unwanted focus stealing, and can be quit reliably from the tray. The same claims must be backed by commands or manual checks that actually ran.

## Final report format

1. App shell architecture and any template-specific adjustments.
2. Files created/changed.
3. Exact build/run/test commands and observed output/status.
4. Manual checks performed, including focus and quit behavior.
5. Known limitations and Phase 3 handoff.
