# Krav: Tilføj handlingsrettede tomme tilstande

Status: **implementeret; afventer manuel validering**. Afhængighed: **01 og 05**.

## Acceptkriterier

- **R7-01:** En førstegangsbruger får én tydelig startaktion.
- **R7-02:** Ingen søgeresultater kan skelnes fra tomt arbejdsområde og fejl.
- **R7-03:** Tilstande under indlæsning blinker ikke fejlagtigt som tomme; fejltekst forbliver tilgængelig.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
