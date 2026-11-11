# Plan: Sprint 7A – interaktivt personnetværk

## Leverance og rækkefølge

Første del af roadmap-trin 12. Efter sprintet kan brugeren navigere fra en komplet graf til sine Markdown-biografier, vælge proband og undersøge en persons direkte forbindelser. Ingen eksportmotor er nødvendig for at bruge grafen. Status: planlagt, ikke implementeret eller valideret.

Læs [Grafkontrakt.md](Grafkontrakt.md) før kodning. Den fastlægger datakilder, konflikter, koordinater og interaktion. Gennemfør i denne rækkefølge:

1. [Datagrundlag og relationer](01-datagrundlag-og-relationer/Plan.md).
2. [Lokalt layout](02-lokalt-layout/Plan.md).
3. [Tegning og interaktion](03-tegning-og-interaktion/Plan.md).
4. [Arbejdsbord og regression](04-arbejdsbord-og-regression/Plan.md).

Hver opgave er afgrænset til én feature. Læs dens tre dokumenter, skriv relevante fejlende tests, implementér, og opdatér valideringsjournalen med faktisk evidens. Implementér ikke senere sprints på forskud. De første to features kan demonstreres med testdata og teknisk testvært; slutbrugerflowet afsluttes i feature 4. Dokumentér manuel afprøvning uden at markere uafprøvet adfærd som godkendt. Ingen ny godkendelsesport indføres mellem kodeopgaver; ejerens samlede accept registreres ved sprintafslutning.

## Eksisterende kode, der skal genbruges

- `Core/Domain/{Person,Family,FamilyTree}.cs`: GEDCOM-personer og eksplicitte familier.
- `Core/Biography/{BiographyDocumentMetadata,BiographyFactsSnapshot,CanonicalBiographySnapshot,BiographySyncBaseline}.cs`: dokumentfakta og godkendte relationer.
- `App/Services/PersonDocumentNavigation.cs`: sikker linkopløsning. `MainWindowViewModel.OpenPersonDocument` og `_editors`: genbrug og bevar åbne tekster.
- `App/Services/{IMarkdownDocumentCatalog,IGedcomSnapshotStore,FileSystemGedcomSnapshotStore,IAtomicFileWriter,AppJsonContext}.cs`: katalog, lokal GEDCOM-kopi og atomisk persistens.
- `App/ViewModels/MainWindowViewModel.cs`: `_familyTree` er kun sat efter import; opstart loader katalog/snapshot, ikke et fuldt træ. `_allPeople` omfatter det komplette katalog, mens `People` er filtreret. Disse forskelle skal håndteres eksplicit.
- `App/Views/MainWindow.axaml` og `App.axaml.cs`: arbejdsbord og komposition. Tilføj en partialfil til koordineringen frem for at udvide den store hovedfil unødigt.
- `tests/.../PersonDocumentNavigationTests.cs`, `PersonFilterUiRegressionTests.cs`, `AvaloniaHeadlessRegressionTests.cs`: eksisterende regressionsmønstre.

Alle nye ViewModels bruger almindelige .NET-typer. Avalonia-geometri, brushes og pointerevents findes kun i view/kontrol. Core har ingen filadgang, processtart eller App-reference.

## Færdigkriterium

Alle krav i [Requirements.md](Requirements.md) og scenarier i [Validation.md](Validation.md) er dokumenteret. `dotnet restore`, `dotnet build` og `dotnet test` består. Manglende Graphviz giver en forklaring, mens editoren stadig fungerer. Native AOT-pakker prøves på osx-arm64, win-x64 og linux-x64 efter projektets distributions-ADR. Der hævdes ikke platformsgodkendelse på baggrund af headless tests alene.
