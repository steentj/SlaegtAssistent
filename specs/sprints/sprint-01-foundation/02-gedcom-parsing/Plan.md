# Plan: GEDCOM Parsing (TDD)

Each step follows Red → Green → Refactor: write the failing test against fixture data first, then
implement just enough to pass.

1. **Author minimal GEDCOM fixture: `single-person.ged`**
   - One `INDI` record with name, sex, birth date and place. No families.
   - Add to `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/single-person.ged`.

2. **Write failing test: load a single person**
   - Test: `GedcomLoader.Load("single-person.ged")` returns a `FamilyTree` with exactly one
     `Person`, with GEDCOM `RecordId`, `FullName`, `Sex`, `BirthDate`, and `BirthPlace` matching
     the fixture.
   - Run tests → confirm failure (types/loader don't exist yet).

3. **Implement domain types (`Person`, `FamilyTree`) and `GedcomLoader` happy path**
   - Add `Gedcom.Net.SDK` NuGet reference to `SlaegtsAssistent.Core`.
   - Implement just enough mapping logic to satisfy step 2's test, including preserving each GEDCOM
     person record id (`@I...@`) on domain `Person`.
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

7. **Write failing test: repeated import merges by `RecordId`**
   - Create a second fixture (e.g. `two-generations-updated.ged`) where at least one existing
     person keeps the same GEDCOM id but has changed field values, and optionally one brand-new id.
   - Test: loading the updated file into an existing `FamilyTree` updates the existing person
     (matched by `RecordId`) instead of creating a duplicate, and still adds any truly new id.
   - Run tests → confirm failure.

8. **Implement merge behavior**
   - Add/confirm loader API that can merge into an existing tree
     (e.g. `Load(filePath, existingTree)` or equivalent merge method).
   - Use `RecordId` as the stable key for de-duplication and updates.
   - Run tests → confirm pass.

9. **Write failing test: death date/place is optional**
   - Test: a person fixture with no `DEAT` tag yields `DeathDate == null` and `DeathPlace == null`
     without throwing.
   - Run tests → confirm failure if current implementation assumes death data is always present.

10. **Implement/confirm optional-field handling**
    - Ensure mapping code treats missing tags as `null`, not exceptions or default structs.
    - Run tests → confirm pass.

11. **Author fixture: `malformed.ged`**
    - Deliberately broken structure (e.g., unterminated record or invalid tag hierarchy).

12. **Write failing test: malformed file raises `GedcomLoadException`**
    - Test: loading `malformed.ged` throws `GedcomLoadException` with a non-empty, descriptive
      `Message` (not the raw parser exception type leaking out).
    - Run tests → confirm failure.

13. **Implement error translation**
    - Catch underlying parser exceptions (or detect invalid structure) and rethrow as
      `GedcomLoadException`.
    - Run tests → confirm pass.

14. **Write failing test: missing file path**
    - Test: `Load()` with a non-existent path throws `GedcomLoadException` (not an unhandled
      `FileNotFoundException`).
    - Run tests → confirm failure, then implement, then confirm pass.

15. **Refactor pass**
    - Extract mapping helpers if the loader method has grown too large.
    - Confirm `SlaegtsAssistent.Core` still has zero Avalonia references.
    - Re-run the full `SlaegtsAssistent.Core.Tests` suite to confirm no regressions.
