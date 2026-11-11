# Plan: Dokumentgrundlag og konvertering

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

7B værktøjskæde og ../Dokumentkontrakt.md.

Kravsporbarhed: **B-01, B-02, B-03, B-04, B-05**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Opret rene eksport-records i Core og `BiographyDocumentSnapshotService` i App. Lad snapshot hente fra editorcache før disk og medtage original kildeplacering til ressourceopløsning.
2. Skriv `BiographyExportSnapshotTests`: dirty body eksporteres, original Markdown/dirty-status er uændret, frontmatter er udeladt, og generator/import aldrig kaldes.
3. Implementér `BiographyExportHtmlBuilder` over Markdig-AST og `ExportResourceCollector` med fysisk stikontrol og begrænsede rasterformater. Udtræk kun delbare eksisterende sikkerhedshjælpere; ændr ikke previewets kontrakt.
4. Implementér kapitelnavne, deterministiske bogmærker, interne ankre, personlinks, pladsholdere og advarsler. Skriv `BiographyExportHtmlTests` for links i kode, reference-links, ens navne og stiudbrud.
5. Udvid sidecarens operationer med Pandoc HTML → DOCX/ODT og kontrolleret HTML → PDF. Medlever `reference.docx`, `reference.odt`, print-CSS og lovligt distribueret font. Ingen python-docx-efterbehandling medmindre en dokumenteret, testet begrænsning kræver en afgrænset adapter.
6. Kør en rigtig prøve med Dokumentkontraktens flersidede fixture i alle tre formater. Verificér sider, bogmærker og billeder, før UI bygges; reparér referencefiler/adaptere med konkrete fejlende tests.
7. Tilføj formatuafhængige indholdstests og formatinspektion. Registrér alle nye værktøjsversioner og licenser i 7B's pakningsmanifest.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
