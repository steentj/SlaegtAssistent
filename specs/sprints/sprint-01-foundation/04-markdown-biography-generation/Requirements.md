# Feature: Markdown Biography Generation

## Scope
Convert the domain model produced by GEDCOM parsing (feature 02) into one editable Markdown file
per person, containing the standard structured facts (birth date/place, parents, etc.), as
required by `specs/Roadmap.md` Trin 1: "Generer automatisk en standard Markdown-fil pr. person."

## In Scope
- A pure function/service, `IBiographyMarkdownGenerator`, that takes a `Person` (from feature 02's
  domain model) and returns a Markdown string.
- A fixed, predictable Markdown template: heading with full name, a facts section (birth, death,
  parents), and an empty "Biografi" section/placeholder for the user to write free prose later.
- A file-writing step that saves the generated Markdown to disk, one file per person, using a
  filesystem-safe filename derived from the person's name (with a documented collision strategy).
- Deterministic output: given the same `Person`, the generator always produces byte-identical
  Markdown (required for meaningful snapshot-style tests).

## Out of Scope
- Live preview / rendering Markdown to HTML (covered by feature 06).
- AI enrichment of the prose (Trin 3 — out of this sprint entirely).
- Re-generating/merging Markdown for a person who already has an edited file (out of scope for
  Trin 1; for now, generation only targets people without an existing file, and this feature
  assumes it runs once against a fresh output directory — overwrite behavior is a later concern).

## Decisions & Context
- **Location:** `SlaegtsAssistent.Core/Biography/BiographyMarkdownGenerator.cs`, consuming the
  `Person` type introduced by feature 02. No Avalonia dependency, so it's independently testable.
- **Template shape (fixed for Trin 1):**
  ```markdown
  # {FullName}

  ## Fakta
  - **Født:** {BirthDate} i {BirthPlace}
  - **Død:** {DeathDate} i {DeathPlace}   <!-- omitted entirely if the person is alive/unknown -->
  - **Forældre:** {Parent1FullName}, {Parent2FullName}   <!-- omitted if no parents recorded -->

  ## Biografi
  _Skriv den fulde livshistorie her._
  ```
  Optional facts (death, parents) are omitted as whole bullet lines when the underlying data is
  `null`, rather than printed as "Ukendt", to keep generated files clean.
- **Filename strategy:** `{FullName-slugified}-{ShortId}.md` (e.g. `jens-hansen-a1b2.md`), where
  `ShortId` is a short, stable hash derived from the GEDCOM record's own identifier. This avoids
  collisions between people sharing a name while keeping filenames human-readable. The exact
  hashing approach is an implementation detail covered in `Plan.md`; the important constraint is
  **stability** (same person → same filename across repeated runs).
- **File writing vs. generation split:** `BiographyMarkdownGenerator.Generate(Person) : string` is
  a pure function (easiest to unit test). A separate, thin `BiographyFileWriter.WriteAll(FamilyTree,
  outputDirectory)` handles filesystem I/O and filename resolution, keeping I/O concerns out of
  the string-generation tests.
- **Why isolated:** Depends only on the `Person`/`FamilyTree` domain model from feature 02 (via
  interface, not concrete parsing logic) — it can be developed and tested with hand-built `Person`
  objects in memory, without ever invoking the real GEDCOM parser.

## Dependencies
- Requires the `Person`/`FamilyTree` domain model from feature 02 (GEDCOM Parsing). Does not
  depend on feature 01 (UI shell) or feature 06 (editor/preview) — the UI wires this generator in
  later, but the generator itself needs no UI reference.
