# Plan: Gør grafens information tilgængelig uden mus

Status: **planlagt; ikke implementeret eller godkendt**. Kilde: GUI-anbefaling R4. Alvor: **Major**. Indsats: **M** (relativ størrelse, ikke et tidsestimat). Afhængighed: **Ingen; fælles navigation samles i 05**.

## Mål og evidens

Målet er at gør grafens information tilgængelig uden mus. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

PersonGraphControl er fokusérbar uden egen tastaturhåndtering eller automation peer. Detaljer tegnes ud fra HoveredPerson. Museklik vælger og åbner straks biografien.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/PersonGraphControl.cs`
- `src/SlaegtsAssistent.App/ViewModels/PersonGraphViewModel.cs`
- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Lad valgt person udløse de samme navne og forbindelsesoplysninger som hover. Hold valgt og hoveradfærd tydeligt adskilt.
2. Tilføj en formbaseret valgmarkering og synligt fokus, som fungerer uden farveskelnen.
3. Giv kontrollen det tilgængelige navn »Personnetværk« og en dansk betjeningsbeskrivelse; anvend Avalonia automation peer i visningslaget til meningsfuld status og handlinger.
4. Gør personlisten til en fuld tastaturvej: valg viser oplysninger, Enter åbner biografien, og »Brug som proband« virker på den valgte person.
5. Tilføj piletaster til panorering, +/− til zoom og Home til »Vis alle«, når selve grafen har fokus. Vis betjeningen i hjælpetekst og undgå genveje under tekstindtastning.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Hover, valg og handlingsmål kan komme ud af takt. Bevar eksisterende klik til biografi, kameramatematik og layoutmotor. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
