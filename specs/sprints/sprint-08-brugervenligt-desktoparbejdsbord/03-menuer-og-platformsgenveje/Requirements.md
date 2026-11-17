# Krav: Indfør menuer og platformens genveje

Status: **implementeret; manuel validering udestår**. Afhængighed: **Ingen; koordiner med 01**.

## Acceptkriterier

- **R3-01:** Menu og knap udfører samme kommando én gang og viser samme aktiveringstilstand.
- **R3-02:** Gem, åbn og indstillinger kan nås med platformens genveje. Genveje vises i menuer.
- **R3-03:** Rediger-handlinger rammer den fokuserede tekstkontrol; lukning kan annulleres ved ugemt tekst.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
