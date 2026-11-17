# Krav: Gør gemning og versioner forståelige

Status: **implementeret; manuel validering udestår**. Afhængighed: **03**.

## Acceptkriterier

- **R6-01:** Brugeren kan skelne mellem at gemme filer og at oprette en version.
- **R6-02:** Hele brugerflowet bruger danske versionsbetegnelser; der er én gemmehandling i arbejdsfladen.
- **R6-03:** Versionsbesked kan annulleres uden at oprette en version; eksisterende Git-adfærd bevares.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
