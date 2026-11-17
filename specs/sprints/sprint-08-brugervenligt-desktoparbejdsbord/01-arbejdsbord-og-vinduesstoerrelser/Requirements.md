# Krav: Tilpas arbejdsbordet til minimumsstørrelsen

Status: **planlagt**. Afhængighed: **Ingen**.

## Acceptkriterier

- **R1-01:** Alle nødvendige handlinger kan nås ved 960 × 600 uden vandret rulning i hovedvinduet.
- **R1-02:** Skjulning og genåbning af paneler mister ikke personvalg, editorbuffer eller fornuftige panelbredder.
- **R1-03:** Standardstørrelsen 1.220 × 760 og et stort vindue bruger den ekstra plads til arbejdsindhold.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
