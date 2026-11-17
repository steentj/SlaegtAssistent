# Krav: Ret temaressourcer og statusflader

Status: **planlagt**. Afhængighed: **Ingen; udføres tidligt som grundlag for 08**.

## Acceptkriterier

- **R9-01:** Alle refererede ressourcer findes; statusflader vises kun med indhold.
- **R9-02:** Normal tekst opnår mindst 4,5:1 og væsentlige kontrol-/fokusmarkeringer mindst 3:1 i de understøttede udseender.
- **R9-03:** Diffens betydning kan aflæses uden farve; ViewModels indeholder ingen Avalonia-typer eller visningsfarver.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
