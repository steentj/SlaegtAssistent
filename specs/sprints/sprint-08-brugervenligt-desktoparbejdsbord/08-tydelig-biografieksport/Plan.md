# Plan: Gør eksportens tilstand og handlinger tydelige

Status: **implementeret; manuel validering og godkendelse udestår**. Kilde: GUI-anbefaling R8. Alvor: **Minor**. Indsats: **S** (relativ størrelse, ikke et tidsestimat). Afhængighed: **09 for fælles statusressourcer**.

## Mål og evidens

Målet er at gør eksportens tilstand og handlinger tydelige. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Starthandlinger er synlige under eksport, men gentagne starter afvises internt. Format og samling har tilgængelige navne uden synlige etiketter.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/BiographyExportWindow.axaml`
- `src/SlaegtsAssistent.App/Views/BiographyExportWindow.axaml.cs`
- `src/SlaegtsAssistent.App/ViewModels/BiographyExportViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Tilføj synlige etiketter »Format« og »Samling« med korrekt tilgængelig tilknytning.
2. Gør »Eksportér…« primær og »Udskriv…« sekundær. Deaktivér begge ved tomt udvalg og under kørsel.
3. Vis ubestemt fremdrift sammen med operationens eksisterende fasetekst. Vis »Annullér handling« under kørsel.
4. Lad Escape annullere igangværende arbejde og ellers lukke dialogen. Lukning under kørsel skal vise annulleringsstatus og vente på sikker afslutning.
5. Lås valg, som ikke kan påvirke det igangværende snapshot, så visning og operation ikke modsiger hinanden.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Ændret lukning kan frigive ressourcer før jobafslutning. Genbrug cancellation og snapshots; ændr ikke eksportkontrakter. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
