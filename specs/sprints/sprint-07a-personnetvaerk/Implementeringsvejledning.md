# Implementeringsvejledning: roadmap-trin 12

Dette er startpunktet for en kodningsmodel med begrænset kontekstvindue. Status: **planlagt**. Der er ikke skrevet produktionskode eller kørt funktionelle tests under denne planlægning.

## Læserækkefølge

1. Projektets `AGENTS.md`, `specs/Mission.md` og trin 12 i `specs/Roadmap.md`.
2. Kun den aktuelle sprints `Plan.md`, `Requirements.md`, `Validation.md` og kontraktdokument.
3. Den aktuelle features tre dokumenter og de eksisterende filer, som kodeopgaverne henviser til.

Læs ikke alle historiske sprintplaner for hver opgave. De tre kontrakter er fælles beslutninger; featureplanerne er konkrete arbejdspakker. Ved en nødvendig ændring af kontrakten opdateres både krav, tests og relevante henvisninger. En teknisk begrænsning må ikke skjules ved at fjerne et brugerkrav.

## Arbejdspakker og afhængigheder

| Rækkefølge | Kodeopgave | Primær leverance | Afhænger af |
| --- | --- | --- | --- |
| 1 | [7A.01](01-datagrundlag-og-relationer/Plan.md) | Grafmodel, datakilde, godkendte relationer og Markdown-links | Eksisterende dokumentkontrakter |
| 2 | [7A.02](02-lokalt-layout/Plan.md) | DOT/plain, lokal procesrunner, Graphviz-layout | 7A.01 |
| 3 | [7A.03](03-tegning-og-interaktion/Plan.md) | Tegnekontrol, kamera, hover/naboer og input | 7A.01–02 |
| 4 | [7A.04](04-arbejdsbord-og-regression/Plan.md) | Faner, navigation, probandpersistens og regression | 7A.01–03 |
| 5 | [7B.01](../sprint-07b-grafeksport-og-udskrift/01-lokal-vaerktoejskaede/Plan.md) | Privat sidecar, PDF-prøve og pakningsgrundlag | 7A.02 |
| 6 | [7B.02](../sprint-07b-grafeksport-og-udskrift/02-grafeksport-og-udskriftsflow/Plan.md) | SVG/PNG/PDF og udskriftsflow | 7A og 7B.01 |
| 7 | [7C.01](../sprint-07c-biografieksport/01-dokumentgrundlag-og-konvertering/Plan.md) | Aktuel dokumenttekst til gyldig DOCX/PDF/ODT | 7B.01 |
| 8 | [7C.02](../sprint-07c-biografieksport/02-udvalg-batch-og-udskrift/Plan.md) | Udvalg, filnavne, batch, UI og udskrift | 7C.01 og 7B.02 |
| 9 | [7C.03](../sprint-07c-biografieksport/03-layout-platform-og-afslutning/Plan.md) | Visuel kvalitet, pakkede platforme og samlet accept | Alle foregående |

Hver række kan gives til modellen som selvstændig opgave: **Implementér kun [featuresti] efter dens Plan.md, Requirements.md og Validation.md. Læs sprintens kontrakt. Genbrug eksisterende services. Kør relevante tests, og registrér faktisk validering og resterende afvigelser.** Arbejdspakkerne udføres sekventielt; opdelingen forudsætter ingen parallelle agenter.

## Centrale servicekontrakter

Nedenstående er foreslåede signaturer, som skal konkretiseres med records fra kontrakterne. Bevar den beskrevne ansvarsdeling; tilpas eksisterende navnekonventioner uden at skabe et ekstra lag for hvert metodekald.

```csharp
// Core: ingen I/O eller Avalonia-afhængighed.
PersonGraph Build(IReadOnlyList<GraphPersonInput> people,
    IReadOnlyList<GraphRelation> relations);
GraphDotInput Write(PersonGraph graph);
PersonGraphLayout Parse(string plainOutput, GraphDotInput input);

// App-services: baggrundsarbejde med eksplicit levetid og annullering.
Task<WorkspaceGraphSnapshot> CreateSnapshotAsync(CancellationToken cancellationToken);
Task<PersonGraphLayout> LayoutAsync(PersonGraph graph, CancellationToken cancellationToken);
Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken);
Task<GraphExportResult> ExportAsync(GraphExportRequest request, CancellationToken cancellationToken);
Task<BiographyExportResult> ExportAsync(BiographyExportRequest request, CancellationToken cancellationToken);
```

`GraphDotInput` indeholder DOT og intern-id/record-id-opslag. `WorkspaceGraphSnapshot` indeholder arbejdsområde-id, stigende revision og kopierede data. Eksportrequest indeholder fastfrosne input og validerede mål; filpicker er et UI-ansvar før servicen kaldes. `MainWindowViewModel` ejer eksisterende editorcache; en snapshotadapter tilbydes som smal læseadgang, så nye services ikke får fri adgang til hele ViewModelen. Undgå statiske globale caches og service-locator i nye ViewModels.

Fejl fra processer/filsystem oversættes ved App-grænsen til danske beskeder og konkrete mulige handlinger. Annulering er en særskilt tilstand, ikke en rød fejl. Diagnostik for en enkelt person må ikke slukke resten af grafen. Grafrevision, layoutrevision og eksportøjebliksbillede skal kunne spores i tests uden at logge persontekster.

## Teststrategi

- Rene Core-tests for grafsemantik, DOT/plain, sceneskrivning, stabil sortering og filnavne. Ingen mock af selve algoritmen, der testes.
- App-tests med fakes for proces, filvalg, dialog og systemfremviser. Test observerbar adfærd, buffers og publicerede filer.
- Headless-tests for binding, tastaturinput, hover/klik og dialogtilstande; de erstatter ikke native grafik-/printprøver.
- Faktiske værktøjstests for Graphviz/Pandoc/WeasyPrint med syntetiske fixtures, tydeligt adskilt fra hurtige tests. Afhængighedsmangel registreres som ikke kørt, aldrig som succes.
- Visuel kontrol og offline-smoketest af faktiske platformspakker efter [distributions-ADR](../sprint-04e-dataintegritet-og-komplet-synkronisering/11-regression-platform-og-distribution/ADR-001-platformspakker-med-native-biblioteker.md).

Kør ikke mange gentagne fulde tests uden nye ændringer. Kør målrettet under udvikling, hele berørte projekter ved featureafslutning og den fulde løsning ved sprintafslutning. Ingen publicering, signeringsnøgler eller beskeder til tredjepart indgår i kodeopgaverne.

## Bevidste afgrænsninger

Grafen viser alle eksplicitte relationer, også Markdown-personlinks; den udleder ikke slægtskab fra tekstlig navnelighed. Proband vælges særskilt fra biografinavigation. Kraftbaseret layout er fast efter beregning, med zoom/panorering og hover frem for kontinuerlig fysik. Udskrift sker via genereret PDF i systemfremviser. Biografieksport omfatter DOCX/PDF og også roadmapets ODT, men ingen komplet bogdesigner. Kun eksportudvalg har grænsen 100 personer; grafen må ikke afkortes tilsvarende.

## Kendte risici og beslutningsregler

| Risiko | Konkret håndtering |
| --- | --- |
| Godkendte dokumentrelationer afviger fra ny GEDCOM | Følg facts/Approved-prioritet og test barnets egen relation mod gentagne snapshots |
| Statisk SVG kan ikke levere præcis hover/klik | Native tegnekontrol og eget id-/koordinatopslag; SVG bruges til eksport |
| Graphviz eller WeasyPrint varierer på platforme | Rigtig prøve, privat fastlåst build og native platformjournal før færdigmelding |
| Store grafer eller høj nabograd | Spatialt indeks, samlet tegneflade, nabooversigt og målbare performancekrav |
| Eksportmotor henter et billede fra nettet | Kontrolleret AST/HTML, aktivindsamling, whitelist-fetcher og netværksfælder |
| Flere filer kan ikke committes atomisk som gruppe | Konvertér alt først, publicér atomisk pr. fil, rapportér præcist delvist resultat |
| Printflow forveksles med printerkvittering | UI og journal siger PDF åbnet til udskrift; faktisk print afprøves særskilt |
