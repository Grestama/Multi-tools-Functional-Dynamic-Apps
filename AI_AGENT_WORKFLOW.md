# AI Agent Workflow

## Which prompt should I use?

Use one phase prompt at a time. These prompts are designed to be given to a coding agent that can inspect and modify the repository, not a chat-only model that cannot access project files.

| Phase | Prompt file | Intended result |
|---|---|---|
| 1 — Foundation | `docs/agent-prompts/PHASE-1-FOUNDATION.md` | Audit and complete requirements/docs only; no app code. |
| 2 — App Shell | `docs/agent-prompts/PHASE-2-APP-SHELL.md` | Real WinUI solution, runnable idle overlay, tray, lifecycle. |
| 3 — Core Services | `docs/agent-prompts/PHASE-3-CORE-SERVICES.md` | Tested state machine and Windows media adapter. |
| 4 — UI Detailing | `docs/agent-prompts/PHASE-4-UI-DETAILING.md` | Polished idle/expanded UI, motion, accessibility, display behavior. |
| 5 — QA & Release | `docs/agent-prompts/PHASE-5-QA-RELEASE.md` | Regression tests, performance validation, packaging, release docs. |

## Recommended usage loop

1. Open the repository root in the AI coding agent so it can inspect and edit files.
2. Ensure the agent reads `AGENTS.md` (most coding agents also discover this file automatically; do not assume discovery—mention it in the request).
3. Start a new task with only the prompt for the current phase. Do not give all five prompts to the agent at once.
4. Let it inspect current files and produce a short plan before changing code. It should then implement the task rather than only giving advice.
5. Review its final report, especially exact commands run and tests not run. Check `git diff` and `git status` before accepting changes.
6. Commit or checkpoint a known-good state before moving to the next phase.
7. Update `docs/ACCEPTANCE_CRITERIA.md` based on observed results. Do not advance merely because the agent says a phase is done.

## If the agent loses context

Ask it to re-read `AGENTS.md`, `README.md`, `docs/ROADMAP.md`, and the prompt for the active phase; inspect current source and tests; then summarize the actual state before continuing. Never ask it to regenerate the entire repository as a shortcut.

## Handoff checklist between phases

- The previous phase's exit criteria are demonstrably satisfied or remaining gaps are explicitly accepted.
- The working tree contains no accidental deletions or unrelated refactors.
- The solution builds/tests as expected in the available environment.
- Architecture or UX changes are documented.
- The agent's output separates verified behavior from assumptions.
