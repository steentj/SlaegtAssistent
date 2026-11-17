# Krav: sprint 8 – brugervenligt desktoparbejdsbord

Status: **planlagt**.

## Samlede krav

- **S8-01:** Hovedvinduet kan betjenes ved 960 × 600 og udnytter standard 1.220 × 760 og større vinduer uden afskæring af nødvendige handlinger.
- **S8-02:** Forskelsgennemgangen er læsbar ved 900 pixels bredde og bevarer beslutninger og dokumentindhold ved annullering.
- **S8-03:** Menuer, genveje og fokus følger platformen; tastaturbrugere kan gennemføre alle sprintets primære flows.
- **S8-04:** Én fælles personsøgning og tilgængelige personoplysninger fungerer uden hover; eksplicit åbning bevarer ugemte tekster.
- **S8-05:** Gemning, versioner, tomme tilstande og eksport bruger tydelige danske handlinger og korrekt status.
- **S8-06:** Semantiske temaer, kontrast og ikke-farvebaserede markeringer virker i begge temaer og understøttede tilgængelighedsindstillinger.
- **S8-07:** Forhåndsvisning viser renderet indhold som standard; rå HTML er sekundært tilgængelig med uændret sikkerhed.
- **S8-08:** Ingen regression i fri tekst, godkendte fakta, editorbuffers, billeder, eksport, versioner og annullering; ingen nye cloudkald eller afhængigheder.

## Detaljekrav

- [R1: Tilpas arbejdsbordet til minimumsstørrelsen](01-arbejdsbord-og-vinduesstoerrelser/Requirements.md)
- [R2: Forenkling af forskelsgennemgangen](02-overskuelig-forskelsgennemgang/Requirements.md)
- [R3: Indfør menuer og platformens genveje](03-menuer-og-platformsgenveje/Requirements.md)
- [R4: Gør grafens information tilgængelig uden mus](04-tilgaengeligt-personnetvaerk/Requirements.md)
- [R5: Saml navigation og skjul grafeksportens detaljer](05-faelles-personnavigation-og-grafeksport/Requirements.md)
- [R6: Gør gemning og versioner forståelige](06-gemning-og-forstaaelige-versioner/Requirements.md)
- [R7: Tilføj handlingsrettede tomme tilstande](07-tomme-tilstande-og-foerstegangsbrug/Requirements.md)
- [R8: Gør eksportens tilstand og handlinger tydelige](08-tydelig-biografieksport/Requirements.md)
- [R9: Ret temaressourcer og statusflader](09-semantiske-temaer-og-statusflader/Requirements.md)
- [R10: Forenkling af forhåndsvisning og dansk sprog](10-enkel-forhaandsvisning-og-dansk-sprog/Requirements.md)

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.
