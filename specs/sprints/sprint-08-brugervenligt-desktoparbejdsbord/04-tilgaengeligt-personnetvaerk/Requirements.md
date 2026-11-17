# Krav: Gør grafens information tilgængelig uden mus

Status: **implementeret; manuel validering udestår**. Afhængighed: **Ingen; fælles navigation samles i 05**.

## Acceptkriterier

- **R4-01:** En person kan findes, identificeres, bruges som proband og åbnes uden mus eller hover.
- **R4-02:** Grafens fokus og valgte person kan skelnes i begge temaer og ved nedsat farvesyn.
- **R4-03:** Skærmlæser kan få personens navn og forbindelser via den tilgængelige liste og status.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
