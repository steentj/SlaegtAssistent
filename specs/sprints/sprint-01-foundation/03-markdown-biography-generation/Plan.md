# Plan: Markdown Biography Generation (TDD)

1. **Write failing test: minimal person → heading and empty facts**
   - Test: a `Person` with only a `FullName` (no birth/death/parents) generates Markdown starting
     with `# {FullName}` and containing an empty `## Fakta` section (no bullet lines) followed by
     the fixed `## Biografi` placeholder text.
   - Run tests → confirm failure (generator doesn't exist yet).

2. **Implement minimal `BiographyMarkdownGenerator.Generate`**
   - Just enough to satisfy step 1: heading + empty Fakta section + Biografi placeholder.
   - Run tests → confirm pass.

3. **Write failing test: birth date/place rendered**
   - Test: a `Person` with `BirthDate` and `BirthPlace` set produces a `- **Født:** ...` bullet
     with correctly formatted date (agree on a fixed format, e.g. `dd-MM-yyyy`, in the test).
   - Run tests → confirm failure.

4. **Implement birth fact rendering**
   - Run tests → confirm pass.

5. **Write failing test: death fact omitted when absent, present when set**
   - Two test cases: `DeathDate == null` → no "Død" bullet at all; `DeathDate` set → bullet
     rendered with date/place.
   - Run tests → confirm failure.

6. **Implement conditional death fact rendering**
   - Run tests → confirm pass.

7. **Write failing test: parents fact omitted/rendered correctly**
   - Two test cases: no parents → no "Forældre" bullet; one or two parents → names joined with
     `, `.
   - Run tests → confirm failure.

8. **Implement conditional parents fact rendering**
   - Run tests → confirm pass.

9. **Write failing test: output is deterministic**
   - Test: calling `Generate` twice with an equivalent (but separately constructed) `Person`
     produces byte-identical strings.
   - Run tests → confirm failure only if any non-deterministic element (e.g. current timestamp)
     was accidentally introduced; otherwise this test should already pass and serves as a
     regression guard — keep it.

10. **Write failing test: filename slug generation is stable and collision-safe**
    - Test: `BiographyFileWriter` (or a `FileNameGenerator` helper) produces the same filename for
      the same person across two separate calls, and different filenames for two different people
      sharing an identical full name.
    - Run tests → confirm failure (helper doesn't exist yet).

11. **Implement filename slug generator**
    - Slugify full name (lowercase, hyphenate, strip diacritics/special characters) and append a
      short stable id/hash derived from the person's GEDCOM identifier.
    - Run tests → confirm pass.

12. **Write failing test: `BiographyFileWriter.WriteAll` writes one file per person**
    - Test: given a `FamilyTree` with N people and a temp output directory, after `WriteAll` the
      directory contains exactly N `.md` files, and each file's content matches
      `Generate(person)` for the corresponding person.
    - Run tests → confirm failure (writer doesn't exist yet).

13. **Implement `BiographyFileWriter.WriteAll`**
    - Iterate people, resolve filename via the slug generator, write generated Markdown to disk
      (creating the output directory if missing).
    - Run tests → confirm pass.

14. **Refactor pass**
    - Extract a small `MarkdownFactsBuilder` helper if the generator method becomes hard to read.
    - Confirm no Avalonia/UI references anywhere in `SlaegtsAssistent.Core`.
    - Re-run the full test suite to confirm no regressions.
