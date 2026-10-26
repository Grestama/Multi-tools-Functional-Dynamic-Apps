# AI Coding Prompt Template

Copy this prompt when asking an AI coding assistant to implement one bounded task.

---

You are implementing **DynamicTopBar for Windows**, a native desktop overlay built with C#, WinUI 3, Windows App SDK, and .NET 10 where supported by the chosen stable template.

Before coding, read:
- `docs/PROJECT_BRIEF.md`
- `docs/ARCHITECTURE.md`
- `docs/UX_SPEC.md`
- `docs/ENGINEERING_RULES.md`
- The active phase in `docs/ROADMAP.md`

**Current phase:** [fill in phase number and name]

**Single task:** [describe one narrow outcome]

**Allowed files to create/edit:** [list paths]

**Constraints:**
- Do not implement features assigned to later phases.
- Do not add new packages without explaining why and checking compatibility.
- Keep WinUI/native Windows APIs out of `DynamicTopBar.Core`.
- Verify API usage against current Microsoft documentation; do not invent methods.
- Preserve the documented interaction timing unless the task explicitly changes it.
- Add or update tests for Core behavior.
- Do not refactor unrelated files.

**Before finishing, report:**
1. Files changed and their purpose.
2. Design/architecture decisions made.
3. Exact build/test commands.
4. Results actually observed; separate verified results from assumptions.
5. Remaining risks or limitations.

Do not claim the app builds or tests pass unless you ran the relevant commands in the target development environment.

---
