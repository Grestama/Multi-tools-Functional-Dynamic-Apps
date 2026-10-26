# DynamicTopBar.App

Phase 1 scaffold only. Phase 2 will create the real WinUI 3 project here using the current stable Microsoft template.

Suggested responsibilities:
- `UI/`: XAML views, styles, and view models.
- `Services/`: app-level orchestration and concrete service implementations.
- `Models/`: presentation-only models when not defined in Core.
- `Platform/`: Win32/WinRT/Windows App SDK adapters.
- `Configuration/`: settings defaults and persistence implementation.
- `Assets/`: bundled icons and fallback artwork.

Avoid duplicating types between App and Core. Keep OS handles and WinUI types in this project.
