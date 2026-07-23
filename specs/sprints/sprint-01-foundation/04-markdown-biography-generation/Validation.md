# Validation: Markdown Biography Generation

## Definition of Done
- [ ] `dotnet test tests/SlaegtsAssistent.Core.Tests` passes, including all generator and file
      writer tests listed in `Plan.md`.
- [ ] `BiographyMarkdownGenerator.Generate(Person)` is a pure function: no filesystem or Avalonia
      access, verified by code review (method signature takes/returns only in-memory types).
- [ ] Generated Markdown for a person with full data (birth, death, parents) matches the template
      shape defined in `Requirements.md`, confirmed via an approval/snapshot-style test with a
      fixed expected string.
- [ ] Optional facts (death, parents) are correctly omitted when data is absent — covered by
      dedicated tests, not just visually inspected.
- [ ] `BiographyFileWriter.WriteAll` produces exactly one `.md` file per person in the input
      `FamilyTree`, with filenames that are stable across repeated runs and unique even for
      people sharing a full name.
- [ ] No generated filename contains characters invalid on Windows, macOS, or Linux filesystems
      (spaces are fine; slashes, colons, and other reserved characters are not).

## How to Verify
1. Run:
   ```
   dotnet test tests/SlaegtsAssistent.Core.Tests
   ```
   All tests pass, exit code 0.
2. Use the fixture `.ged` files from feature 02 (`single-person.ged`, `two-generations.ged`) as
   input: load via `GedcomLoader`, run `BiographyFileWriter.WriteAll` into a temp directory, and
   manually inspect the generated `.md` files for readability and correctness against the
   template.
3. Confirm re-running generation against the same fixture data produces identical filenames and
   file content byte-for-byte (diff the two runs' output directories).

## Merge Criteria
Merge when all Definition of Done items are checked, `dotnet test` is green, and a reviewer
confirms the Markdown template output is readable/sensible Danish prose scaffolding (not just
technically correct) — since this file is what the user will read and edit directly in feature 05.
