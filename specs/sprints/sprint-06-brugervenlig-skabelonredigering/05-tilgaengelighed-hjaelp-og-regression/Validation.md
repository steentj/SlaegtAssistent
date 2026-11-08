# Validation: Tilgængelighed, hjælp og regression

- [ ] Hovedforløbet kan gennemføres uden mus med logisk fokusorden.
- [ ] Fokus, valg, fejl og deaktiveret tilstand kan skelnes uden kun farve.
- [x] Gem og annuller er synlige ved minimumsstørrelse og 200 % skalering i headless-layouttesten.
- [x] Editorens relevante kontroller har danske tilgængelighedsnavne; tvetydige filvalg og indtastningsfelter i Indstillinger er navngivet.
- [x] Kontekstuel hjælp forklarer resultatet uden at kræve syntakskendskab.
- [x] Hjælpen beskriver den guidede editor uden at kræve syntakskendskab.
- [ ] Mindst tre uerfarne brugere har gennemført den samme hovedopgave.
- [ ] Mindst 80 % gennemfør på højst fem minutter uden hjælp.
- [ ] Ingen kritiske eller alvorlige brugerbarhedsfund står åbne.
- [x] `dotnet build` og hele `dotnet test` består.

**Godkendelsesdato:** Afventer afsluttende manuel kontrol og brugertest.

**Godkendt af:** Ikke endnu.

**Bemærkninger:**

## Automatisk validering den 8. november 2026

- `dotnet build --no-restore --verbosity minimal`: bestået, 0 advarsler og 0 fejl.
- `dotnet test --no-build --no-restore --verbosity minimal`: 132 Core-tests og 209 App-tests bestået, i alt **341 tests**, ingen sprunget over.
- Rød fase: nye hjælpetests kunne ikke kompileres, før katalogets hjælp og resultateksempler fandtes. De første headless-kørsler fandt derefter manglende navn på en sektionsindikator, en klippet handlingsknap i Indstillinger og mistet feltfokus ved ændring af samlingen.
- Grøn fase: fejlene er rettet ved eksplicitte danske navne, en todelt fast handlingsrække og genbrug af eksisterende blok-/feltobjekter. Omsortering bevarer den valgte blok og det valgte felt; til-/fravalg beholder den visuelle rækkefølge. Tests dækker også gentagne flytninger.
- `TemplateEditorAccessibilityTests` bruger faktiske headless-tastetryk til sektionsflytning, tab-navigation til overskrift/kladde/annuller, feltvalg, skjul/vis, gentagen feltflytning og annullering med Escape. Den eksisterende overskriftstest skriver tegn for tegn og kontrollerer fokus og markør.
- Ved 100 % og 200 % headless-DPI kontrolleres handlingsknappernes faktiske grænser ved editorens minimum på 760 × 560 og Indstillingers minimum på 560 × 480. Valideringsfejl er tekst, som forbliver synlig sammen med editorens handlinger. Native operativsystemskalering og visuel læsbarhed er ikke godkendt af disse tests.
- Tydelig fokus- og valgramme er tilføjet i editoren. Fejl i Indstillinger bruger nu tematilpasset fejlfarve og fortsat forklarende tekst. Kontrast, skærmlæser og native preview kræver stadig manuel kontrol.
- `BiographyTemplateEditorCatalog` er fælles kilde til hjælp og et resultateksempel for alle otte sektionstyper. Editorens hjælp følger valgt sektion. Hjælpemenuen hedder nu **Hjælp til skabeloner** og beskriver kladde, gemning, annullering, eksempelperson, tastatur og **Anvend skabelon** uden tidligere skabelonsyntaks. Søgning bevarer hele hjælpesektionen, så eksempler og kontekst ikke mistes.
- Den samlede regression omfatter fortsat lagring/genåbning, GEDCOM-import, personlinks, preview, kandidatgodkendelse og bevarelse af fri tekst. Trin 4's native macOS-filvælgerkontrol er fortsat særskilt manuel.

## Resterende manuelle kvalitetsporte

- Opgaveark, ens lokalt eksempelgrundlag, resultatark og bedømmelsesregel er klar i `Brugertest.md`.
- Mindst tre uerfarne deltagere skal stadig inviteres og gennemføre opgaven. Der er ikke opfundet invitationer, tider, succesandele eller observationer. Ved tre deltagere kræver 80 %-målet tre af tre succeser.
- Tastaturforløbet skal gennemføres fra Indstillinger til gemning og genåbning i den rigtige app. Kontrollér også lyst/mørkt tema, skærmlæser, native preview og 200 % systemskalering på de understøttede platforme.
- Sprintet er **ikke endeligt manuelt godkendt**. Den tidligere besked »Godkendt, fortsæt« godkendte overgangen fra trin 4, ikke disse endnu ikke gennemførte brugertest.

## Rettelse: Kun formateret skabelonhjælp

- Den rå Markdown-visning er fjernet efter brugerens rettelse. Kun søgefeltet og den formaterede hjælp vises; hjælpen bruger hele indholdsområdets bredde.
- Den nye headless-regression fejlede først på den ekstra tekstboks og består efter rettelsen. Den kontrollerer også, at den formaterede hjælp ikke længere ligger i et todelt layout.
- `dotnet test tests/SlaegtsAssistent.App.Tests --no-restore --filter 'FullyQualifiedName~AvaloniaHeadlessRegressionTests|FullyQualifiedName~TemplateCheatSheetViewModelTests' --verbosity minimal`: alle 11 relevante tests består, herunder hjælpesøgningen.
