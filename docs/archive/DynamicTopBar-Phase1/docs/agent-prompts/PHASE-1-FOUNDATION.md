# AI Agent Prompt — Phase 1: Foundation & Documentation

Copy the prompt below into your AI coding agent while the repository root is open.

---

You are the repository architect for **DynamicTopBar**, a native Windows desktop utility with a discreet idle top bar and an expanded contextual panel. Follow the repository-wide rules in `AGENTS.md`.

## Objective

Audit and finish the project foundation so later implementation agents have one consistent source of truth. The repository may already contain a scaffold: inspect it first and adapt it instead of rebuilding it from scratch.

## Read first

- `AGENTS.md`
- `README.md`
- `docs/PROJECT_BRIEF.md`
- `docs/ROADMAP.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/ENGINEERING_RULES.md`
- `docs/SETUP_WINDOWS.md`
- `docs/ACCEPTANCE_CRITERIA.md`
- `docs/decisions/ADR-001-TECH-STACK.md`

## Tasks

1. Inspect the full tree and report what exists, what is empty scaffolding, and what is missing. Preserve user changes and inspect Git status if Git is initialized.
2. Check consistency across scope, architecture, UX rules, setup guidance, technology decision, roadmap, and acceptance criteria. Resolve contradictions directly in the relevant Markdown files.
3. Keep the definition explicit: there is **no remote backend** in the MVP. “Backend” means local Core logic/services and Windows platform adapters in the desktop process.
4. Confirm the MVP and non-goals. Keep notifications, calendar, clipboard, weather, AI features, cloud sync, accounts, and plugin marketplaces out of scope unless the product brief is deliberately amended.
5. Verify links and current platform assumptions against official Microsoft documentation when internet/tool access is available. If a fact cannot be verified, mark it as a validation item rather than guessing.
6. Ensure `AGENTS.md` and phase-specific prompts explain how an agent should inspect, adapt, implement, test, and report without blindly replacing existing work.
7. Update the repository map in `README.md` to reflect files that really exist.
8. Review Phase 1 acceptance criteria and mark an item complete only if you can verify it from the repository.

## Constraints

- Do not create the WinUI solution or implement runtime application code in Phase 1.
- Do not introduce dependencies, databases, cloud services, or new frameworks.
- Do not create speculative documents that repeat another document without adding value.
- Preserve the chosen C# + WinUI 3 + Windows App SDK direction, but make framework versions conditional on the currently supported stable template.

## Deliverables

- An audited, internally consistent foundation.
- Updated documentation and repository map as needed.
- A concise summary of contradictions resolved and any open decisions.

## Done when

A new agent can understand the MVP, layer boundaries, UX state model, setup requirements, and the next phase without needing undocumented context. No app code is claimed to exist.

## Final report format

1. Audit findings.
2. Files created/changed and why.
3. Checks actually performed.
4. Acceptance criteria verified / still open.
5. Explicit handoff instructions for Phase 2.
