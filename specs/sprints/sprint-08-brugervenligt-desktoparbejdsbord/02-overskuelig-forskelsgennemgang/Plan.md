# Plan: Forenkling af forskelsgennemgangen

Status: **planlagt; ikke implementeret eller godkendt**. Kilde: GUI-anbefaling R2. Alvor: **Major**. Indsats: **M** (relativ størrelse, ikke et tidsestimat). Afhængighed: **Ingen**.

## Mål og evidens

Målet er at forenkling af forskelsgennemgangen. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Forskelsdialogen har otte kolonner, standardbredde 1.480 og minimum 900; faste kolonner optager 775 før tre værdikolonner, mellemrum og margin. Alle forskelle oprettes i én StackPanel.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Services/AvaloniaGedcomDifferenceDialogService.cs`
- `src/SlaegtsAssistent.App/ViewModels/GedcomDifferenceReviewViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Lever først gruppering pr. person med antal ændringer og kun den valgte persons detaljer. Brug virtualiseret personliste ved mange personer.
2. Vis »Dokument« og »Ny GEDCOM« som hovedværdier. Fold godkendt baseline, teknisk sti og årsag ind under »Flere oplysninger«.
3. Brug »Bevar dokumentværdi« og »Brug ny GEDCOM-værdi« ved hvert felt; brug »Brug ny skabelon« i skabelonflowet. Bevar eksisterende valg også når personen skiftes.
4. Lever derefter fast handlingsområde med »Luk uden ændringer« og »Anvend valgte« samt antal valgte ændringer. Gør kandidatvisningen sammenklappelig og tydeligt knyttet til den valgte person.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Dette er et sikkerhedskritisk flow. Regressioner skal dække beslutningsbevaring og annullering før UI-omlægning; Core-regler må ikke ændres. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
