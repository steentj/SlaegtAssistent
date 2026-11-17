# Krav: Saml navigation og skjul grafeksportens detaljer

Status: **planlagt**. Afhængighed: **01, 03 og 04**.

## Acceptkriterier

- **R5-01:** Der er én fælles personsøgning, der omfatter hele datasættet.
- **R5-02:** Listevalg kan inspicere grafen uden at skifte fane; eksplicit åbning bevarer ugemte buffers.
- **R5-03:** SVG, PNG, PDF og udskrift bevarer eksisterende indhold og sikkerhedskontrol. Kun relevante formatvalg vises.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Afgrænsning

Sektionen ændrer den beskrevne brugeroplevelse og dens nødvendige UI-tilstande. Eksisterende datakontrakter, eksportformater og sikkerhedsregler bevares. Se de konkrete trin og risici i [Plan.md](Plan.md).
