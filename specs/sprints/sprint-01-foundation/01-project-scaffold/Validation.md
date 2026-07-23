# Validation: Project Scaffold

## Definition of Done
- [ ] Solution builds with `dotnet build` with zero errors and zero new warnings.
- [ ] `dotnet test` passes for both `SlaegtsAssistent.App.Tests` and `SlaegtsAssistent.Core.Tests`
      (the latter may contain zero tests at this stage — it must still build and report success).
- [ ] `dotnet run --project src/SlaegtsAssistent.App` launches a window without throwing, showing:
  - A left pane with an (empty) person list.
  - A right pane with placeholder Editor/Preview tabs.
- [ ] `MainWindowViewModel` and `PersonListItemViewModel` contain **no** reference to any Avalonia
      UI/rendering type (verified by inspecting `using` statements / project reference direction:
      ViewModels project may reference `Avalonia.Base`/`CommunityToolkit.Mvvm` but must not require
      a rendering context to be constructed and tested).
- [ ] All tests written in `Plan.md` exist, are meaningful (fail without the implementation,
      pass with it), and are committed alongside the implementation.
- [ ] `SlaegtsAssistent.Core` has no project or package reference to Avalonia (confirms the
      domain layer stays UI-agnostic for future features).

## How to Verify
1. Clone/checkout the branch and run:
   ```
   dotnet build SlaegtsAssistent.sln
   dotnet test SlaegtsAssistent.sln
   ```
   Both commands must exit with code 0.
2. Run `dotnet run --project src/SlaegtsAssistent.App` and visually confirm the two-pane shell
   appears and the app does not crash on launch or close.
3. Inspect `src/SlaegtsAssistent.Core.csproj` to confirm no `Avalonia*` package reference exists.
4. Review test output to confirm the tests from `Plan.md` steps 2, 4, and 6 are present and pass.

## Merge Criteria
This feature can be merged when all Definition of Done items are checked, CI (or local
`dotnet test`) is green, and a reviewer confirms the ViewModel layer has no Avalonia rendering
dependency (so features 02–05 can unit-test their ViewModels the same way).
