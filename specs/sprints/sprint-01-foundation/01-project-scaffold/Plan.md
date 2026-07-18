# Plan: Project Scaffold (TDD)

Each numbered step follows Red → Green → Refactor. UI-only steps (scaffolding, XAML layout) have
no meaningful "test first" and are marked accordingly — keep them minimal and mechanical.

1. **Create solution & project skeleton** *(no test — structural setup)*
   - `dotnet new sln -n SlaegtsAssistent`
   - `dotnet new classlib -o src/SlaegtsAssistent.Core`
   - `dotnet new avalonia.mvvm -o src/SlaegtsAssistent.App` (or `avalonia.app` + manual MVVM wiring
     if the template is unavailable)
   - `dotnet new xunit -o tests/SlaegtsAssistent.App.Tests`
   - `dotnet new xunit -o tests/SlaegtsAssistent.Core.Tests`
   - Add all projects to the `.sln`; add project references (`App` → `Core`, both test projects →
     their respective project under test).
   - Add `CommunityToolkit.Mvvm` NuGet package to `SlaegtsAssistent.App`.
   - Add `FluentAssertions` to both test projects.

2. **Write failing test for `PersonListItemViewModel`**
   - Test: constructing a `PersonListItemViewModel` with a display name exposes that name via a
     `DisplayName` property.
   - Run tests → confirm failure (type doesn't exist yet).

3. **Implement `PersonListItemViewModel`**
   - Minimal `ObservableObject`-derived class with a `DisplayName` property.
   - Run tests → confirm pass.

4. **Write failing test for `MainWindowViewModel` default state**
   - Test: a freshly constructed `MainWindowViewModel` exposes an empty `People` collection and a
     `null` `SelectedPerson`.
   - Run tests → confirm failure.

5. **Implement `MainWindowViewModel` default state**
   - `ObservableCollection<PersonListItemViewModel> People` (empty by default).
   - `PersonListItemViewModel? SelectedPerson` property (`[ObservableProperty]`).
   - Run tests → confirm pass.

6. **Write failing test for selection behavior**
   - Test: setting `SelectedPerson` to an item raises `PropertyChanged` for `SelectedPerson`.
   - Run tests → confirm failure (if not already satisfied by `[ObservableProperty]` codegen —
     keep the test regardless as a regression guard).

7. **Implement/confirm selection behavior**
   - Ensure `[ObservableProperty]` generates correct `PropertyChanged` notification.
   - Run tests → confirm pass.

8. **Build the two-pane shell (`MainWindow.axaml`)** *(no unit test — visual layout)*
   - `Grid` with two columns: left = `ListBox` bound to `People` / `SelectedPerson`; right =
     `TabControl` placeholder with an empty "Editor" and "Preview" tab (content added in feature
     04).
   - Set `MainWindowViewModel` as `DataContext` via the composition root.

9. **Wire composition root** *(no unit test — startup wiring)*
   - `App.axaml.cs`: construct `MainWindowViewModel` (directly or via a minimal DI container) and
     assign it to `MainWindow.DataContext` in `OnFrameworkInitializationCompleted`.

10. **Smoke-test the app runs**
    - `dotnet build` the solution.
    - `dotnet run --project src/SlaegtsAssistent.App` and confirm the window opens with the empty
      two-pane layout, no exceptions on startup.

11. **Refactor pass**
    - Review naming, remove template boilerplate/sample content, ensure nullable reference types
      are enabled and warnings-as-errors (if adopted) are clean.
    - Re-run full test suite to confirm no regressions.
