# Engineering Rules

## General

1. Build one working vertical slice at a time; do not implement all planned features in one massive change.
2. Keep the idle experience lightweight and visually minimal.
3. Prefer event-driven updates; justify every periodic timer.
4. Keep OS interop inside `App/Platform` or a similarly named adapter layer.
5. Keep UI markup in XAML and UI behavior/state exposure in ViewModels where practical.
6. Do not put Windows types, window handles, WinUI controls, or Dispatcher references into `Core`.
7. Avoid global mutable state and untracked background tasks.
8. Cancel timers/subscriptions and dispose resources when the app or relevant window closes.
9. Do not swallow exceptions silently. Handle expected failures; record enough local diagnostic context without collecting personal data.
10. Never claim a system action succeeded until its result is confirmed.

## C# conventions

- Use nullable reference types when compatible with the generated template.
- Prefer descriptive names and small methods.
- Use `async`/`await` for asynchronous APIs; never block the UI thread on tasks.
- Use cancellation tokens for work that can outlive the current state or view.
- Subscribe/unsubscribe event handlers symmetrically.
- Avoid arbitrary dependency injection frameworks until there is a clear need; basic Microsoft.Extensions.DependencyInjection is sufficient if required by the template.
- Add comments for non-obvious Win32/WinRT interop, not for every line.

## UI conventions

- Define reusable colors, typography, spacing, corner radii, and motion values as resource tokens.
- Avoid hard-coded visual values repeated across views.
- All interactive icon-only buttons need accessible names and reasonable hit targets.
- Do not use emojis as production system icons.
- Keep animation subtle and short; do not animate layout continuously while idle.
- Verify hover and click behavior independently so moving from the header into the expanded panel does not accidentally trigger collapse.

## Testing

- Core state transitions should be unit-testable without running the UI.
- Add a regression test whenever a bug is fixed in Core logic.
- Manually test overlay behavior on a real Windows desktop, including DPI scaling and multiple displays.
- Test no-media, missing-metadata, and unsupported-control cases.
- Include a repeatable build command in the developer guide after the solution exists.

## Git workflow

- Use small commits with one purpose each.
- Avoid committing generated outputs, machine-specific settings, signing certificates, or local secrets.
- Use feature branches for major changes and AI-assisted refactors.
- Update docs when a design decision changes; record significant stack/architecture changes as ADRs.

## AI-assisted coding rules

- Read `PROJECT_BRIEF.md`, `ARCHITECTURE.md`, and `UX_SPEC.md` before generating code.
- Ask for the exact files that will change and the reason for each change.
- Work only on the current phase; do not add deferred features proactively.
- Do not invent API names or claim untested APIs compile. Verify platform APIs against current Microsoft documentation.
- For each code change, provide build/test steps and state what is not yet tested.
- If template-generated project structure conflicts with this scaffold, preserve the intent and explain the adjustment.
