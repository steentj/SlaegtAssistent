# Validation: GEDCOM Parsing

## Definition of Done
- [ ] `dotnet test tests/SlaegtsAssistent.Core.Tests` passes, including all tests listed in
      `Plan.md` (single person, two-generation relationships, optional death fields, malformed
      file, missing file).
- [ ] `IGedcomLoader` / `GedcomLoader` and the domain types (`Person`, `FamilyTree`) live in
      `SlaegtsAssistent.Core` with **no** dependency on Avalonia or any UI project.
- [ ] Domain types (`Person`, `FamilyTree`) are the only types returned from `GedcomLoader` —
      no `GedcomParser` library types leak into the public API (verified by inspecting the
      loader's public method signatures).
- [ ] Malformed or missing GEDCOM files produce a `GedcomLoadException` with a descriptive
      message, never an unhandled/raw exception from the third-party library.
- [ ] At least 3 fixture `.ged` files exist under
      `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/` and are referenced by tests (not
      inline strings), so future features can add more fixtures without churn.

## How to Verify
1. Run:
   ```
   dotnet test tests/SlaegtsAssistent.Core.Tests
   ```
   All tests pass, exit code 0.
2. Manually inspect `GedcomLoader`'s public signature — confirm it returns `FamilyTree`/`Person`
   types (defined in `SlaegtsAssistent.Core/Domain`), not `GedcomParser` namespace types.
3. Temporarily point the loader at a real, larger GEDCOM export (e.g. from MyHeritage or
   Legacy) and confirm it loads without throwing, as a smoke test beyond the fixtures (not a
   committed test — exploratory check only).
4. Confirm `SlaegtsAssistent.Core.csproj` has no `Avalonia*` package or project reference.

## Merge Criteria
Merge when all Definition of Done items are checked, `dotnet test` is green, and a reviewer
confirms the domain model (`Person`/`FamilyTree`) is stable enough for feature 03 (Markdown
biography generation) to consume without expecting further breaking changes to field names/types.
