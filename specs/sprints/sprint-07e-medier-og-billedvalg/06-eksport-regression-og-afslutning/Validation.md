# Validering: Eksport, platforme og samlet accept

Status: **ikke kørt; featureen er ikke implementeret**.

## Automatiserede prøver

- Udvid `ExportResourceCollectorTests`, `BiographyExportHtmlBuilderTests`, `BiographyExportServiceTests` og `BiographyExportIntegrationTests` med mediemappe uden for GEDCOM-/dokumentrod.
- Kontroller indlejrede mediafiler i DOCX/ODT og faktisk billedindhold i PDF; markørkommentarer må ikke blive synlige. Afprøv både portræt og landskabsbilleder.
- Manglende fil, ændret fil efter preflight, samlet 100 MiB-budget, symlink udenfor rod og mappeskift under eksport giver kontrolleret adfærd uden at ændre dokumenter.
- Ende-til-ende: genstart uden ny import → vælg billede → ugemt eksport → Gem → Commit → ændr → tilbagerul. Billedhenvisninger følger Markdown-versionen.
- Fuld restore/build/test og native/AOT-matrix som beskrevet i sprintets Validation.md.

Assertions bruger FluentAssertions. Regressioner skal påvise de konkrete krav i [Requirements.md](Requirements.md), inklusive fejlsituationer; undgå tests, der blot gentager implementationen.

## Manuel prøve

1. Gennemfør hele hovedscenariet i sprintets Validation.md på syntetiske data.
2. Åbn PDF i en PDF-fremviser og DOCX/ODT i et egnet lokalt kontorprogram. Kontrollér alle billeder og billedtekster visuelt.
3. Afprøv lyst/mørkt tema, 200 % skalering, tastatur, lukkeadvarsel, Gem som og manglende mediemappe.
4. Registrér en eksplicit samlet vurdering. Ikke-afprøvede native platforme står som manglende validering, aldrig som bestået.

## Resultatjournal

| Dato/platform | Krav/test | Resultat og evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- |
| Ikke kørt | Alle | Afventer implementering | – |
