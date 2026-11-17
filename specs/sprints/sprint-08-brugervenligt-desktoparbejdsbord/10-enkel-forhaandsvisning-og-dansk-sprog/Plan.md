# Plan: Forenkling af forhåndsvisning og dansk sprog

Status: **implementeret og manuelt godkendt af brugeren**. Kilde: GUI-anbefaling R10. Alvor: **Minor**. Indsats: **S** (relativ størrelse, ikke et tidsestimat). Afhængighed: **03**.

## Mål og evidens

Målet er at forenkling af forhåndsvisning og dansk sprog. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Forhåndsvisningen viser permanent »Webvisning« og »Rå HTML«. Fanen hedder »Preview«, og søgefeltet bruger »record-id«.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/ViewModels/EditorViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Omdøb fanen til »Forhåndsvisning« og lad renderet indhold være standard.
2. Flyt »Vis rå HTML« til sekundær menu eller sammenklappelig teknisk sektion; tilbyd tydelig tilbagevenden til renderet visning.
3. Brug »Søg navn eller person-id« og normal skrivemåde for »Personer« og »Kontekst«.
4. Bevar sikkerhedsbeskeder synlige i relevant tilstand, og kontrollér at den tekniske visning ikke tilsidesætter previewpolitikken.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Flytning af visningsvalg kan skjule sikkerhedsstatus. Bevar eksisterende bindinger og renderingsservice. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
