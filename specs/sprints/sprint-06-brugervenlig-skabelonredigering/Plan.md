# Plan: Sprint 6 – brugervenlig skabelonredigering

## Formål og placering

Sprintet er roadmap-trin 11 og gennemføres efter personlinks og før grafisk slægtstræ og eksport. Målet er at gøre standardskabelonens vigtigste valg tilgængelige uden teknisk syntaks, samtidig med at dataintegritet og lokal behandling bevares. Bagudkompatibilitet med det tidligere skabelonformat er ikke et krav.

Den valgte produktretning og fravalgte alternativer er dokumenteret i [Analyse.md](Analyse.md).

## Feature-rækkefølge

1. [`01-editormodel-og-generering`](01-editormodel-og-generering/Plan.md)
2. [`02-guidet-blokeditor`](02-guidet-blokeditor/Plan.md)
3. [`03-eksempeldata-og-live-preview`](03-eksempeldata-og-live-preview/Plan.md)
4. [`04-lagring-og-fejlhaandtering`](04-lagring-og-fejlhaandtering/Plan.md)
5. [`05-tilgaengelighed-hjaelp-og-regression`](05-tilgaengelighed-hjaelp-og-regression/Plan.md)

Hver feature leverer en afsluttet, testbar vertikal del. Næste feature må først begynde, når automatiske tests er grønne, featureens manuelle scenarier er gennemført, og resultatet er godkendt.

## Arkitektur og ansvar

### Core

- Indfør en UI-uafhængig, versioneret editormodel for blokke, indstillinger og rækkefølge.
- Implementér deterministisk dokumentgenerering fra editormodellen, eventuelt ved at kompilere til den eksisterende interne skabelontekst.
- Implementér versioneret serialisering og genåbning af den nye editormodel.
- Opret deterministiske eksempeldata gennem domænemodeller eller kanoniske snapshots.
- Genbrug relevante dele af `BiographyTemplateRenderer` og skabelonidentiteten, hvis det giver den enkleste løsning; den nye editormodel er den offentlige kontrakt.

### App

- Opret et selvstændigt skabeloneditorvindue eller en tydeligt afgrænset arbejdsflade, som åbnes fra Indstillinger.
- Hold ViewModels fri af Avalonia-typer. Filvalg, dialoger og preview-hosting leveres gennem services.
- Bevar et arbejdseksemplar, indtil brugeren vælger **Gem**. **Annuller** må ikke ændre fil, indstillinger eller aktive dokumentkandidater.
- Brug samme sikre Markdown-til-HTML-service som hovedpreviewet.
- Vis en tydelig overgangsbesked, hvis indstillinger peger på det tidligere format, og tilbyd at starte med standardskabelonen.

## Implementeringsforløb

### Fase 1: kontrakt og testgrundlag

1. Kortlæg standardskabelonens nuværende blokke, alle offentlige felter og flows for indstillinger, importpreflight, preview og genrendering.
2. Fastlæg editormodellens version, bloktyper, tilladte indstillinger og serialiseringsformat i en kort kontraktdokumentation.
3. Skriv fejlende Core-tests for standardmodel, deterministisk output, serialisering, genåbning og ukendt modelversion.
4. Implementér den mindste generator og serialisering uden UI.
5. Kontrollér, at generatorens output er gyldigt, deterministisk og dækker alle understøttede blokke.

### Fase 2: guidet redigering

1. Skriv ViewModel-tests for tilføj, skjul, flyt, nulstil, fortryd og konfigurer blok.
2. Byg master/detail-layout: blokliste til venstre, enkel konfiguration i midten og plads til preview til højre eller under ved smalle vinduer.
3. Brug danske labels og korte forklaringer fra en central katalogmodel, ikke tekniske feltstier i UI'et.
4. Begræns valg til gyldige kombinationer; eksempelvis tilbydes tabel kun for blokke, der har en stabil kolonnemodel.
5. Tilføj tastaturbetjening, tydelig fokusmarkering og bekræftelse før destruktiv nulstilling.

### Fase 3: preview

1. Skriv tests for eksempelperson, valg af faktisk person, debounce, fejl og annullering.
2. Tilføj et lokalt eksempel, så editoren er nyttig ved første opstart uden GEDCOM.
3. Kompilér, validér, render og sikkerhedsbehandl preview gennem produktionskæden.
4. Vis både “Eksempelperson” og faktiske personer i et tilgængeligt valg, men send aldrig data ud af processen.
5. Bevar sidste gyldige preview ved fejl og knyt fejlbeskeden til den blok eller indstilling, som skal rettes.

### Fase 4: sikker lagring og overgang

1. Skriv integrationstests for ny skabelon, overskrivning, **Gem som**, annullering, skrivefejl, genstart og fund af det tidligere format.
2. Gem atomisk via projektets filsystemabstraktion og genbrug eksisterende politik for backup/gendannelse.
3. Aktivér først den nye skabelonsti, når filskrivning, genlæsning og validering er lykkedes.
4. Hvis den gemte sti peger på det tidligere format, vises en overgangsbesked med valg om at oprette en ny standardskabelon; konvertering er ikke påkrævet.
5. Sørg for, at skabelonændringer fortsat bliver kandidater og ikke overskriver eksisterende persondokumenter automatisk.

### Fase 5: hjælp, brugerbarhed og regression

1. Tilføj kontekstuel hjælp ved hvert valg og erstat det syntaksorienterede skabelon-cheat sheet med hjælp til den guidede editor.
2. Kør headless UI-tests for navigation, fokus, labels, valideringsfejl og vinduets minimumsstørrelse.
3. Gennemfør den målbare brugertest fra `Analyse.md` og ret alvorlige fund, før sprintet godkendes.
4. Regressionstest standard- og egne skabeloner, personlinks, importpreflight, kandidatgodkendelse, genåbning og privat preview.
5. Kør `dotnet build` og `dotnet test`; registrér kommandoer og resultater i `Validation.md`.

## TDD- og godkendelsesporte

For hver feature anvendes rød-grøn-refaktorér:

1. Skriv sporbare fejlende tests før produktionskode.
2. Implementér den mindste komplette adfærd.
3. Refaktorér med grønne målrettede tests.
4. Kør berørte Core- og App-tests.
5. Gennemfør featureens manuelle validering.
6. Dokumentér resultatet og stop for godkendelse.

Sprintet afsluttes kun, når hele testpakken er grøn, en skabelon i det nye format kan genåbnes med samme model, fri biografitekst ikke påvirkes, og den manuelle brugerbarhedstærskel er opfyldt.

## Risici og modtræk

| Risiko | Modtræk |
| --- | --- |
| Brugeren forventer, at det gamle format kan åbnes | Tydelig overgangsbesked og enkel oprettelse af en ny standardskabelon |
| Dokumentgenereringen afviger fra preview | Samme generator og sikre previewkæde anvendes begge steder |
| UI bliver en ny teknisk formular | Danske opgaveorienterede valg, progressive detaljer og brugertest |
| Preview afviger fra import | Samme loader, validering, renderer og sikre previewservice anvendes |
| Gemning efterlader aktiv sti til defekt fil | Atomisk skrivning, genlæsning og validering før aktivering |
| Skabelonændring overskriver brugerens tekst | Eksisterende markører og kandidatgodkendelse bevares |
| Store eksempeldata gør preview langsomt | Fast lille datasæt, cancellation og debounce |

## Uden for sprintet

- En generel WYSIWYG-tekstbehandler.
- Vilkårlig HTML, scripts eller plugins i skabeloner.
- Bagudkompatibilitet, import eller konvertering af det tidligere skabelonformat.
- Eksportspecifik typografi og sidelayout.
- Cloudlagring, telemetri eller ekstern brugeranalyse.
