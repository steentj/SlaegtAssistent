# Validering: Links mellem persondokumenter

## Automatiske acceptscenarier
- Forælder, barn og partner linker til de rigtige dokumenter, også når målet oprettes senere i samme batch.
- Ens navne og forskellige record-id'er giver forskellige mål; dubleret dokumentidentitet eller manglende mål giver almindelig tekst.
- Faktisk eksisterende sti vinder over et filnavn beregnet fra et ændret navn.
- Relative stier og linktekst er gyldige ved undermapper, danske bogstaver, mellemrum, parenteser, kantede parenteser, procent- og nummertegn.
- Standardskabelon og egne skabelonfelter renderer korrekt uden dobbelt escaping.
- Snapshotrendering bruger godkendte relationer; afvist ny relation indføres ikke gennem links.
- Kandidataccept opdaterer kun genererede sektioner; afvisning og fri tekst bevares.
- Gentagen import/rendering er deterministisk og giver ingen gentagen identisk kandidat.
- Eksisterende previewbeskyttelse mod eksterne ressourcer består.

## Manuel afprøvning
1. Importér en lokal GEDCOM med mindst to generationer, børn og partnere. Gem dokumenterne og kontrollér links i Markdown-filerne med en lokal Markdown-læser.
2. Genåbn arbejdsområdet og kontrollér dokumentstierne uden ny import.
3. Opdatér en eksisterende biografi med fri tekst gennem kandidatflowet; prøv både accept og afvisning.
4. Afprøv en egen skabelon med de dokumenterede referencefelter samt filnavne med mellemrum og danske bogstaver.
5. Kontrollér previewets opførsel og dokumentér, om lokal personnavigation understøttes eller links alene er portable dokumentlinks.

## Udført validering
### Automatiske resultater – 6. november 2026
- `dotnet build /Users/steen/Projects/SlaegtAssistent/SlaegtsAssistent.sln --no-restore`: bestået, 0 advarsler og 0 fejl.
- `dotnet test /Users/steen/Projects/SlaegtAssistent/SlaegtsAssistent.sln --no-build --no-restore`: 260 tests bestået (120 Core og 140 App), ingen fejl eller oversprungne tests.
- `git diff --check`: bestået.
- Roadmap kontrolleret maskinelt: trin 1–14 i rækkefølge, alle specifikationslinks findes, og gamle decimalhenvisninger er fjernet fra roadmap.

Kommandoerne blev kørt fra `/private/tmp` med installeret SDK 10.0.403. Projektets `global.json` kræver 10.0.402 med deaktiveret roll-forward; den præcise SDK-version er ikke installeret. Konfigurationen er bevaret. Validering med præcis 10.0.402 er derfor ikke udført.

### Dækning
- `BiographyPersonLinksTests`: relative URI'er fra kildedokumentets mappe, særlige tegn, tvetydige og manglende mål, navnesammenfald, escaping, egne skabeloner, tomme navnelisters betingelser, determinisme samt godkendte forældre-/børne-/partnerrelationer.
- `BiographyPersonLinkTests`: fremad- og bagudlinks i en batch, eksisterende brugerdefineret filnavn efter navneændring, dubleret dokumentidentitet, optaget ugyldigt mål og annullering.
- `MainWindowViewModelTests.PersonLinks_RequireCandidateApprovalAndPreserveFreeText`: accept og afvisning af linkopdatering gennem det faktiske importflow; fri tekst bevares, og genimport efter accept giver status Uændret.
- Den eksisterende suite, herunder tests af sikker preview, snapshotpersistens og skabelonkontrakten, består fortsat.

### Afgrænsning og manuel status
- Manuel GUI-afprøvning er ikke udført; ovenstående manuelle scenarier afventer.
- Links er portable Markdown-dokumentlinks. Den eksisterende private preview fjerner lokale href-mål og understøtter derfor ikke intern personnavigation. Previewpolitikken er ikke ændret.
- Strukturerede personreferencer linkes efter record-id. Navne i fri prosa og noter omskrives ikke ved navnesøgning.
- Arbejdsområdets eksisterende katalog søger fortsat i den valgte mappe, ikke rekursivt. Core-opslaget understøtter relative stier mellem undermapper, men dette indfører ikke et nyt rekursivt dokumentkatalog.
- Eksisterende egne skabeloners relationsfelter `fullName` og navnelister får links automatisk. Skabeloner, der selv bygger Markdown-links, skal bruge `plainName` som linktekst for at undgå indlejrede links.
