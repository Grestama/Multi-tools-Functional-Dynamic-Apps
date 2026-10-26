# AI Agent Prompt — Phase 5: QA, Performance & Release

Copy this prompt into your coding agent when core features and the UI are implemented.

---

You are implementing **Phase 5 only** of DynamicTopBar. Follow `AGENTS.md`. First audit the real repository, build configuration, test suite, packaging approach, and existing known issues. Do not infer release readiness from feature presence alone.

## Objective

Make the MVP reproducibly buildable, testable, installable, and supportable. Fix high-priority release blockers before adding any new features.

## Read first

- `AGENTS.md`
- `README.md`
- `docs/PROJECT_BRIEF.md`
- `docs/ROADMAP.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/ENGINEERING_RULES.md`
- `docs/ACCEPTANCE_CRITERIA.md`
- all test projects and release/build configuration

## Tasks

1. Run a clean build and all automated tests. Capture actual errors and fix root causes; do not merely suppress warnings or alter tests to make them pass.
2. Add/complete unit tests for state transitions, cancellation/re-entry, media mapping, feature navigation boundaries, unavailable modules, timer preservation while switching pages, missing-session fallbacks, and lifecycle cleanup. Add UI/manual regression steps for focus, tray quit, duplicate launch, long metadata, missing artwork, and repeated Previous/Next clicks.
3. Validate startup/shutdown, timer cancellation, event unsubscription, background task cancellation, exception handling, and repeated media-session changes. Fix leaks or stale UI updates discovered.
4. Measure idle CPU and memory on a real Windows target where possible. Describe the test environment and idle scenario. Avoid making performance guarantees without measurements.
5. Verify behavior at common display scales/resolutions and with multiple displays where available. Test normal windowed use and common fullscreen applications, documenting any limitations honestly.
6. Choose and verify a reproducible packaging method compatible with the project's real template and target OS. Test install, first launch, normal launch, startup behavior if enabled, upgrade path if supported, and uninstall on a clean or isolated environment when possible.
7. Ensure application identity, icon/assets, version metadata, and user-facing names are consistent. Do not bundle signing credentials or machine-specific secrets.
8. Add concise troubleshooting documentation, known limitations, build/package commands, and a release checklist. Record significant packaging decisions in an ADR.
9. Review privacy: no telemetry, account creation, network calls, or data collection should be introduced without explicit product approval. Local diagnostic logs, if present, should be minimal and documented.
10. Audit every item in `docs/ACCEPTANCE_CRITERIA.md`. Mark only items actually verified; label environment-dependent items as not verified rather than passing them by assumption.

## Constraints

- Do not add new feature categories during stabilization. Verify that every module marked “available” in the UI has a real working implementation; label prototypes clearly and do not simulate successful OS actions.
- Do not claim clean-install, performance, or fullscreen validation unless performed.
- If packaging cannot be tested in the current environment, prepare the configuration and give exact user-side validation steps.

## Deliverables

- Passing automated tests and a clean supported build, or a precise report of unresolved blockers.
- Repeatable packaging/release steps.
- Troubleshooting guide, known limitations, and completed acceptance checklist with evidence/notes.
- A version/release summary and reproducible command list.

## Done when

A developer can follow the documented steps to build the app; supported tests pass; install/start/quit behavior is repeatable; release limitations are documented honestly; and the package can be reproduced in the validated environment.

## Final report format

1. Release readiness summary.
2. Build/test results with commands and environment.
3. Manual and performance checks actually performed.
4. Package artifact/location and install/uninstall results if produced.
5. Remaining release blockers and a prioritized known-issues list.
