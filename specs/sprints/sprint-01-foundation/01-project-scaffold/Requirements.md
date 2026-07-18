# Feature: Project Scaffold

## Scope
Establish the base .NET solution and the Avalonia UI application shell described in
`specs/TechStack.md` (Afsnit 1) and `specs/Roadmap.md` (Trin 1, first bullet). This feature
does **not** include GEDCOM parsing, Markdown generation, or the editor/preview logic — it only
provides the empty two-pane window, the MVVM wiring, and the solution/project layout that later
features plug into.

## In Scope
- Solution (`SlaegtsAssistent.sln`) with clear separation between UI and domain logic.
- Avalonia UI application project targeting the newest .NET version, using the MVVM pattern via
  `CommunityToolkit.Mvvm`.
- Main window with a two-pane layout:
  - **Left:** Person list (placeholder, bound to an empty/dummy collection for now).
  - **Right:** Editor/Preview area (placeholder tab control, content wired in a later feature).
- Dependency injection / composition root wiring so later features can register services without
  restructuring the app.
- A test project set up for unit-testing ViewModels (no UI rendering required).

## Out of Scope
- Real person data, GEDCOM loading, Markdown generation, or live preview rendering (covered by
  features 02–04 in this sprint).
- Native AOT publishing/packaging (deferred until the app has real functionality worth shipping).
- Styling/theming polish beyond a usable default layout.

## Decisions & Context
- **Solution layout:**
  - `src/SlaegtsAssistent.Core` — plain class library for domain models and future business logic
    (GEDCOM parsing, Markdown generation land here in features 02–03). No Avalonia dependency.
  - `src/SlaegtsAssistent.App` — Avalonia UI project (Views, ViewModels, App composition root).
  - `tests/SlaegtsAssistent.App.Tests` — unit tests for ViewModels and composition wiring.
  - `tests/SlaegtsAssistent.Core.Tests` — created empty here so feature 02 can start writing tests
    immediately without a setup detour.
- **Test framework:** xUnit, chosen for first-class `dotnet test` support and broad Avalonia
  community usage. Assertions via `FluentAssertions` for readability.
- **MVVM approach:** ViewModels contain zero Avalonia UI types so they are fully unit-testable in
  isolation from the rendering engine (a prerequisite for TDD on later features).
- **Placeholder data:** MainWindowViewModel exposes an `ObservableCollection<PersonListItemViewModel>`
  (empty at this stage) and a `SelectedPerson` property, so feature 02/03 can populate it without
  changing the shell's shape.
- **Why isolated:** This feature has no dependency on GEDCOM or Markdown libraries; it only needs
  the two NuGet packages `Avalonia` and `CommunityToolkit.Mvvm`. It can be fully built and tested
  before any parsing code exists.

## Dependencies
- None (first feature of the sprint). Features 02–04 depend on the solution/project structure
  created here, but this feature does not depend on them.
