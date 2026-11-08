# Plan: Lagring og fejlhåndtering

1. Kortlæg projektets atomiske filskrivning og genbrug samme abstraktion og gendannelsespolitik til skabeloner.
2. Skriv integrationstests for ny fil, overskrivning, **Gem som**, annullering, adgangsfejl, afbrudt skrivning, ugyldig genindlæsning og en sti til det tidligere format.
3. Implementér tofaset gemning: skriv sikkert, genindlæs og validér, og opdatér først derefter `AppSettings`.
4. Implementér overgangsbeskeden og oprettelse af en ny standardskabelon, når det tidligere format findes.
5. Integrér den gemte model med hash-, importpreflight- og kandidatflow.
6. Gennemfør manuel fejlindsprøjtning med testfiler og stop for godkendelse.

## Implementeringsbeslutninger – 8. november 2026

- Editorvinduet åbnes fra **Indstillinger → Rediger skabelon…** og modtager alle indlæste personer til preview.
- **Brug kladde** returnerer kun modellen til Indstillinger. Den endelige **Gem** eller **Gem som…** i Indstillinger skriver filen. Dermed kan både editorens og Indstillingers **Annuller** efterlade eksisterende filer og aktive indstillinger uændrede. Ved en ny skabelon åbner **Gem** også filvælgeren.
- `TemplateEditorStorageService` bruger den eksisterende `IAtomicFileWriter` til en midlertidig fil i destinationsmappen. Filen genindlæses, kompileres og sammenlignes med kladden før en atomisk omdøbning erstatter destinationen. Ingen gendannelse af en allerede overskrevet fil er nødvendig ved valideringsfejl.
- Nye filer foreslås med endelsen `.slaegtskabelon` i overensstemmelse med modelkontrakten. Indholdet er versioneret JSON i UTF-8 uden BOM.
- `BiographyTemplateLoader.Load` er produktionsindgangen for modelbaserede filer; `Parse` forbliver indgangen for internt kompileret skabelontekst. Hash beregnes fortsat fra denne deterministiske tekst.
- Det tidligere tekstformat afvises med en dansk overgangsbesked. Editorens standardmodel gemmes som en ny fil, medmindre brugeren udtrykkeligt vælger en anden destination.
- Skabelonlagring ændrer ingen persondokumenter. **Anvend skabelon** eller næste GEDCOM-import genbruger det eksisterende kandidatflow og kræver godkendelse af skabelonændringen.

## Opfølgning på afprøvning

1. Regressionstest stabil blok-/feltidentitet og tastaturfokus ved gentagen overskriftindtastning.
2. Giv GEDCOM-filvælgeren et eksplicit alle-filer-filter, så den ikke overlader filtervalget til en tidligere native dialog; kontrollér fortsat manuel macOS-adfærd efter skabelonlagring.
3. Tilføj en Core-operation til genrendering fra dokumentets godkendte snapshot uden at ændre GEDCOM-baseline eller dokumentfakta.
4. Tilføj **Anvend skabelon** i hovedvinduet med kandidatgennemgang, bevarelse af ugemte editorer og tilbagerulning ved skrivefejl.
5. Test godkendelse, afvisning, annullering, gentagen anvendelse og brug efter genstart uden adgang til GEDCOM-kilden. Kør samlet build og regression.

**Anvend skabelon** læser eksisterende persondokumenter, ikke GEDCOM-kilden. Den genrenderer fra godkendte snapshots og aktuelle dokumentfakta, bevarer metadata bortset fra skabelonhashen og skriver kun godkendte ændringer. Åbne, ugemte editorer forbliver ugemte. Dokumenter uden understøttet snapshot skal først gennemgås via GEDCOM-import; handlingen afviser dem med en dansk besked frem for at gætte på data. Ved skrivefejl gendannes kun de berørte dokumenter atomisk; GEDCOM-snapshotfiler berøres ikke.
