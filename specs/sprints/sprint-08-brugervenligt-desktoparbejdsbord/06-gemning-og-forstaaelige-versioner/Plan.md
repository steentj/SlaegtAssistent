# Plan: Gør gemning og versioner forståelige

Status: **implementeret; manuel validering og godkendelse udestår**. Kilde: GUI-anbefaling R6. Alvor: **Minor**. Indsats: **S** (relativ størrelse, ikke et tidsestimat). Afhængighed: **03**.

## Mål og evidens

Målet er at gør gemning og versioner forståelige. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

»Gem« og »Gem ændringer« bruger begge SaveAllCommand. Versionshandlingerne hedder »Commit« og »Commit alle«.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/Views/VersionHistoryWindow.axaml`
- `src/SlaegtsAssistent.App/Services/AvaloniaVersionDialogService.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Behold én »Gem alle« i hovedarbejdsfladen og den tilsvarende menukommando.
2. Omdøb »Commit« til »Gem version…« og »Commit alle« til »Gem version af alle dokumenter…«.
3. Brug dialogtitel »Beskriv versionen« og bekræftelse »Gem version«. Forklar, at alle dokumentændringer indgår.
4. Opdatér tilgængelige navne, tooltips og relevant hjælp. Bevar tekniske identifikatorer og Git-service.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Nye tekster kan blive for brede. Brug menuer og ombrydning frem for mindre skrifttype. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
