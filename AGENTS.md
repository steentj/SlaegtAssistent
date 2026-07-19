# Project Guidelines

## Language
- **All documentation, UI text, code comments, and other written text must be in Danish** (see [specs/Mission.md](specs/Mission.md)). Identifiers (class/method/variable names) stay in English per normal .NET convention.

## Architecture
- `src/SlaegtsAssistent.Core` — plain class library for domain models and business logic (GEDCOM parsing, Markdown generation). No Avalonia dependency, no UI types.
- `src/SlaegtsAssistent.App` — Avalonia UI app (Views, ViewModels, composition root). MVVM via `CommunityToolkit.Mvvm`; ViewModels must stay free of Avalonia UI types so they're unit-testable without a renderer.
- `tests/SlaegtsAssistent.Core.Tests` and `tests/SlaegtsAssistent.App.Tests` — xUnit test projects mirroring the two src projects.
- See [specs/TechStack.md](specs/TechStack.md) for the full stack (Ollama for local AI, PdfPig/Pandoc for docs, Graphviz for tree rendering, Python sidecar for OCR/export) and [specs/Mission.md](specs/Mission.md) for product intent and UX principles.

## Build and Test
```bash
dotnet restore
dotnet build
dotnet test
```
Run the app: `cd src/SlaegtsAssistent.App && dotnet run`

## Conventions
- Privacy-first: no cloud calls, no telemetry. All processing stays local.
- New features are specified under `specs/sprints/<sprint>/<feature>/` with `Plan.md`, `Requirements.md`, `Validation.md` before implementation — check for an existing spec folder before starting non-trivial work.
- Assertions in tests use `FluentAssertions`.
