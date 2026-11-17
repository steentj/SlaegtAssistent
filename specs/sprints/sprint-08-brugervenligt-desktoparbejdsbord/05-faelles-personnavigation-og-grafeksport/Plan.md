# Plan: Saml navigation og skjul grafeksportens detaljer

Status: **planlagt; ikke implementeret eller godkendt**. Kilde: GUI-anbefaling R5. Alvor: **Major**. Indsats: **M** (relativ størrelse, ikke et tidsestimat). Afhængighed: **01, 03 og 04**.

## Mål og evidens

Målet er at saml navigation og skjul grafeksportens detaljer. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Personlisten og grafens søgning/resultatliste konkurrerer om plads. Grafens eksportvalg og outputknapper vises permanent over grafen.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/Views/MainWindow.axaml.cs`
- `src/SlaegtsAssistent.App/ViewModels/MainWindowViewModel.cs`
- `src/SlaegtsAssistent.App/ViewModels/PersonGraphViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Brug personlistens søgning som fælles indgang til alle personer. Bevar søgning i hele datasættet og entydig identitet ved navnesammenfald.
2. Lad listevalg centrere og markere personen i grafen uden faneskift; Enter eller »Åbn biografi« åbner dokumentet. Bevar klik på grafnode som direkte åbning.
3. Saml forbindelsesoplysninger i ét område for valgt person, også tilgængeligt uden mus. Fjern grafens dublerede søgefelt og resultatliste.
4. Erstat permanente eksportvalg med »Eksportér graf…«. Vis format først: PNG viser opløsning, PDF viser papirformat, SVG viser kun relevante valg. Bevar hel graf/udsnit og etiketter.
5. Bevar zoom, »Vis alle« og »Centrér proband« ved grafen. Lad »Udskriv…« bruge eksisterende PDF-flow og relevant papirvalg.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Samling af personvalg kan skabe rekursive opdateringer eller utilsigtede faneskift. Genbrug eksportmetoder og stabile person-id’er. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
