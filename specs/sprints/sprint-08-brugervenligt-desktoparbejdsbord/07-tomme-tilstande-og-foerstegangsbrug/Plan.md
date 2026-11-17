# Plan: Tilføj handlingsrettede tomme tilstande

Status: **implementeret; afventer manuel validering**. Kilde: GUI-anbefaling R7. Alvor: **Minor**. Indsats: **S** (relativ størrelse, ikke et tidsestimat). Afhængighed: **01 og 05**.

## Mål og evidens

Målet er at tilføj handlingsrettede tomme tilstande. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Personlisten har ingen særskilt tom tilstand. Grafen tegner intet uden layout. »Ingen person valgt« giver ingen handlingsvejledning.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/ViewModels/MainWindowViewModel.cs`
- `src/SlaegtsAssistent.App/ViewModels/PersonGraphViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Modellér særskilt opstartsindlæsning, tomt arbejdsområde, ingen søgeresultater og intet personvalg. Undgå at vise tomt arbejdsområde under indlæsning.
2. Vis »Start med at indlæse en GEDCOM-fil« og »Indlæs GEDCOM…« i et tomt arbejdsområde.
3. Vis »Ingen personer matcher søgningen« og »Ryd søgning« ved tomt filterresultat.
4. Vis »Vælg en person for at redigere biografien« i editoren uden personvalg. Genbrug eksisterende kommandoer og fokusér den relevante kontrol efter handling.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Asynkron indlæsning kan vise forkert tomtilstand. Test overgangene; tilføj ikke eksempeldata som skjult sideeffekt. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
