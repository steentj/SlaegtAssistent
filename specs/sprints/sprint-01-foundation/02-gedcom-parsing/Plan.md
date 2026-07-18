# Plan: GEDCOM Parsing (TDD)

Each step follows Red → Green → Refactor: write the failing test against fixture data first, then
implement just enough to pass.

1. **Author minimal GEDCOM fixture: `single-person.ged`**
   - One `INDI` record with name, sex, birth date and place. No families.
   - Add to `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/single-person.ged`.

2. **Write failing test: load a single person**
   - Test: `GedcomLoader.Load("single-person.ged")` returns a `FamilyTree` with exactly one
     `Person`, with `FullName`, `Sex`, `BirthDate`, and `BirthPlace` matching the fixture.
   - Run tests → confirm failure (types/loader don't exist yet).

3. **Implement domain types (`Person`, `FamilyTree`) and `GedcomLoader` happy path**
   - Add `GedcomParser` NuGet reference to `SlaegtsAssistent.Core`.
   - Implement just enough mapping logic to satisfy step 2's test.
   - Run tests → confirm pass.

4. **Author fixture: `two-generations.ged`**
   - Two `INDI` records (parent, child) linked via a `FAM` record.

5. **Write failing test: parent-child relationship resolved**
   - Test: loading `two-generations.ged` produces a child `Person` whose `Parents` collection
     contains the parent `Person` (matched by name/id).
   - Run tests → confirm failure.

6. **Implement family/relationship resolution**
   - Parse `FAM` records and wire `Parents`/`Children` references on the domain `Person` objects.
   - Run tests → confirm pass.

7. **Write failing test: death date/place is optional**
   - Test: a person fixture with no `DEAT` tag yields `DeathDate == null` and `DeathPlace == null`
     without throwing.
   - Run tests → confirm failure if current implementation assumes death data is always present.

8. **Implement/confirm optional-field handling**
   - Ensure mapping code treats missing tags as `null`, not exceptions or default structs.
   - Run tests → confirm pass.

9. **Author fixture: `malformed.ged`**
   - Deliberately broken structure (e.g., unterminated record or invalid tag hierarchy).

10. **Write failing test: malformed file raises `GedcomLoadException`**
    - Test: loading `malformed.ged` throws `GedcomLoadException` with a non-empty, descriptive
      `Message` (not the raw parser exception type leaking out).
    - Run tests → confirm failure.

11. **Implement error translation**
    - Catch underlying `GedcomParser` exceptions (or detect invalid structure) and rethrow as
      `GedcomLoadException`.
    - Run tests → confirm pass.

12. **Write failing test: missing file path**
    - Test: `Load()` with a non-existent path throws `GedcomLoadException` (not an unhandled
      `FileNotFoundException`).
    - Run tests → confirm failure, then implement, then confirm pass.

13. **Refactor pass**
    - Extract mapping helpers if the loader method has grown too large.
    - Confirm `SlaegtsAssistent.Core` still has zero Avalonia references.
    - Re-run the full `SlaegtsAssistent.Core.Tests` suite to confirm no regressions.
