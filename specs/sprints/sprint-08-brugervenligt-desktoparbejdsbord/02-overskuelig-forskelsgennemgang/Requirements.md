# Krav: Forenkling af forskelsgennemgangen

Status: **planlagt**. Afhængighed: **Ingen**.

## Acceptkriterier

- **R2-01:** Ved 900 pixels vinduesbredde kan værdier og valg læses uden vandret rulning.
- **R2-02:** Personskift, foldning og filtrering ændrer ingen beslutninger. Eksisterende standardvalg for nye personer og konflikter bevares.
- **R2-03:** Annullering skriver ingen dokumentændringer; godkendelse anvender præcis de valgte ændringer.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
