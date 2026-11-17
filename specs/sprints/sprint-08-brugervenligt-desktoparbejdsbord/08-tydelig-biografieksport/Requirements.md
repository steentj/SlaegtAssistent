# Krav: Gør eksportens tilstand og handlinger tydelige

Status: **implementeret; manuel validering udestår**. Afhængighed: **09 for fælles statusressourcer**.

## Acceptkriterier

- **R8-01:** Knaptilstande svarer til udvalg og aktuel operation; ingen tilsyneladende aktiv startknap ignorerer klik.
- **R8-02:** Brugeren kan se forberedelse, konvertering, afslutning og fejl samt afbryde sikkert.
- **R8-03:** Eksport medtager fortsat ugemte tekster uden at gemme Markdown.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
