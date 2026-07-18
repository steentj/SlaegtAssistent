# Feature: GEDCOM Parsing

## Scope
Read a GEDCOM file from disk and turn it into an in-memory domain model (`Person`, `Family`,
relationships) that later features (Markdown generation, tree visualization) can consume. Maps to
`specs/Roadmap.md` Trin 1, bullet "Implementer GEDCOM-indlæsning" and `specs/TechStack.md`
Afsnit 2 (`GedcomParser` NuGet).

## In Scope
- Loading a `.ged` file (given a file path or stream) and producing a strongly-typed domain model
  living in `SlaegtsAssistent.Core`.
- Domain model covering the minimum fields needed for Trin 1's Markdown output: full name, birth
  date/place, death date/place (if present), parents, and sex.
- Basic structural validation: a malformed/unreadable file surfaces a clear error rather than a
  cryptic exception or silent partial data.
- Mapping from the third-party `GedcomParser` library's types into our own domain types, so the
  rest of the app never depends directly on the third-party library's object model.

## Out of Scope
- Writing/exporting GEDCOM files (not required by any Trin 1 task).
- Spouse/marriage/family-event modeling beyond what's needed to resolve parent-child relationships
  (deferred to the tree-visualization sprint, Trin 2).
- UI for selecting/loading files (a simple "Open File" flow can be stubbed later; this feature
  exposes a pure `IGedcomLoader` service consumed by the UI).
- Performance tuning for very large GEDCOM files (optimize only if a real bottleneck appears).

## Decisions & Context
- **Library:** `GedcomParser` NuGet package (per `TechStack.md`) is used for the low-level GEDCOM
  tokenizing/parsing. Its types are wrapped, not exposed, so we can swap parsers later without
  touching consumers.
- **Domain model location:** `SlaegtsAssistent.Core/Domain/Person.cs` and
  `SlaegtsAssistent.Core/Domain/FamilyTree.cs` (or similar) — plain C# records/classes with no
  dependency on Avalonia or the parser library's types.
- **Anti-corruption layer:** `SlaegtsAssistent.Core/Gedcom/GedcomLoader.cs` implements
  `IGedcomLoader.Load(string filePath) : FamilyTree`, translating parser output to domain types.
  This isolates the rest of the app from third-party API changes and keeps this feature testable
  via sample `.ged` fixture files rather than mocking the parser.
- **Error handling:** Invalid/missing files throw a domain-specific `GedcomLoadException` with a
  human-readable message (surfaced later by the UI); we do not swallow errors.
- **Test fixtures:** A handful of small, hand-authored `.ged` sample files (e.g.
  `single-person.ged`, `two-generations.ged`, `malformed.ged`) checked into
  `tests/SlaegtsAssistent.Core.Tests/Fixtures/Gedcom/` and used as TDD inputs.
- **Why isolated:** This feature only depends on the `Core` project created in feature 01's
  scaffold (no UI). It can be developed and fully tested against fixture files with zero relation
  to the Avalonia shell or Markdown generation.

## Dependencies
- Requires `SlaegtsAssistent.Core` project to exist (created in feature 01). Feature 03 (Markdown
  biography generation) depends on the `Person`/`FamilyTree` domain model produced here.
