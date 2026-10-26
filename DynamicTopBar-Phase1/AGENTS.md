# Instructions for AI Coding Agents

This file is the repository-wide operating contract for any AI coding assistant. Phase-specific instructions live in `docs/agent-prompts/`.

## First actions for every task

1. Inspect the actual repository tree and `git status` before editing. Preserve existing user changes.
2. Read `README.md`, `docs/PROJECT_BRIEF.md`, `docs/ROADMAP.md`, `docs/ARCHITECTURE.md`, `docs/UX_SPEC.md`, `docs/ENGINEERING_RULES.md`, and `docs/ACCEPTANCE_CRITERIA.md`. Read the active phase prompt as well.
3. Determine the current implementation state from files and command output; do not assume a prior agent completed work just because a document says it should exist.
4. Give a short implementation plan, then carry out the bounded task in the active phase unless a genuine blocker requires user input.
5. Prefer small, coherent changes. Reuse existing components and patterns; do not regenerate or replace the repository without a concrete reason.

## Architecture rules

- This is a **local Windows desktop application**, not a web app and not a client-server system. “Backend” means in-process Core logic, local services, and Windows OS adapters. Do not add an HTTP API, cloud service, account system, telemetry backend, or database to the MVP.
- The intended stack is C# + WinUI 3 + Windows App SDK, targeting .NET 10 only if the selected stable template officially supports it. Verify the current supported template and SDK before pinning versions.
- `DynamicTopBar.App` owns XAML/WinUI, view models, app lifecycle, window behavior, tray integration, and concrete OS adapters.
- `DynamicTopBar.Core` owns platform-independent models, state transitions, and use-case rules. Core must not reference WinUI, HWND/Win32 types, Windows App SDK classes, or the App project.
- Dependency direction is `App → Core`; never introduce `Core → App`.
- Keep OS-specific implementation behind small, purposeful interfaces. Do not create abstractions or projects without a clear need.

## Product and UX constraints

- Idle state is a discreet capsule centered near the top of the selected display; the expanded panel grows down from the same capsule.
- Default starting values: hover delay 500 ms, collapse delay 4000 ms, idle height 44 effective UI units. Centralize values in resources/configuration rather than duplicating magic numbers.
- Pointer re-entry cancels collapse. Hover alone must not steal keyboard focus. A user must always have a reliable way to quit the app.
- Support missing media metadata/artwork and unsupported media actions gracefully.
- Do not guarantee visibility above exclusive fullscreen or secure desktop surfaces. Test and document observed behavior.
- Keep the idle app lightweight; prefer event-driven updates and justify any polling/timers.

## Engineering rules

- Work only on the current phase prompt. Do not prematurely implement later-phase features.
- Before adding a package, verify that it is needed and compatible with the actual target framework; explain the tradeoff.
- Verify Windows/.NET API usage against current Microsoft documentation. Never invent APIs or imply an untested API works.
- Do not block the UI thread. Cancel timers and asynchronous work when no longer needed; unsubscribe events on shutdown.
- Add/update tests for Core logic and regression fixes. Prefer deterministic state tests over timing-sensitive sleeps.
- Use accessible names for icon-only controls, sensible keyboard focus, and subtle motion.
- Update relevant Markdown when a decision or behavior changes. Record material architecture changes in a new ADR under `docs/decisions/`.
- Never delete or overwrite user work, secrets, screenshots, certificates, or machine-specific configuration without explicit permission.

## Definition of done for each task

1. The requested change is implemented in the actual repository (not just described).
2. Relevant build, test, and formatting/static-analysis commands are run when available.
3. The final report lists changed files, commands actually run, observed results, remaining issues, and unverified items.
4. Never claim a build/test passed if it was not run, or if it ran in an environment that cannot validate the Windows target.
5. Update `docs/ACCEPTANCE_CRITERIA.md` only for criteria actually verified; do not mark items complete based solely on code presence.
