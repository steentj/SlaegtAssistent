# Validering: Sprint 7A

Status: ikke kørt; dette er en testplan. Brug xUnit og FluentAssertions. Rene Core-tests kræver ingen Graphviz-installation. App-tests bruger falsk procesrunner, og egentlige værktøjstests markeres særskilt og køres i platformskontrollen.

## Fælles testdata

Opret en lille GEDCOM-fixture med A (mand/proband), B (kvinde/partner), C og D (børn), E (forælder til A), F (forælder til B), G (anden partner til A), H (isoleret/ukendt køn), I–J (separat familie). Tilføj Markdown-link C → H og retur H → C, samt A → B, som allerede har familieforbindelse. Forvent 10 noder og 11 unikke kanter: AB, AC, AD, BC, BD, EA, FB, AG, IJ, CH, EF (EF tilføjes som eksplicit Markdown-link). Forvent A's direkte naboer B,C,D,E,G; F er ikke direkte nabo til A. Test fixturebyggeren med den eksplicitte kantliste før andre forventninger genbruges.

Varianter: manglende dødsdato, `ABT`-dato, ukendt køn, navnesammenfald, ens partnere i flere familier, forældreløkke, selvlink, reference-link, kodet filnavn, omdøbt dokument, dubleret record-id og manglende fil. Tilføj en konflikt, hvor en importeret forælder afvises; barnets dokument skal vinde over forældresidens gentagne snapshot.

## Samlet manuel accept

1. Åbn arbejdsområdet og **Graf**. Tæl noder/kanter mod fixturelisten, inklusive H og I–J. Zoom må ikke skjule relationer i modellen.
2. Hover A: navn/datoer for A,B,C,D,E,G vises samtidig; F får ingen label. Gentag med høj nabograd, manglende datoer og labels ved vindueskanten. Nabooversigten indeholder hele mængden.
3. Vælg H som proband; kun størrelse/ring og kameraets eventuelle centrering ændres. Klik derefter C: C's Markdown-dokument åbnes, og H forbliver proband.
4. Redigér A uden at gemme, åbn C fra grafen, og vend tilbage til A. Buffer og dirty-status er bevaret. Tilføj et link i fri tekst og se ny kant uden at gemme.
5. Aktivér et filter i personlisten; grafens søgning finder stadig alle. Brug kun tastatur til at vælge person, læse naboer, åbne biografi og vælge proband.
6. Genstart efter at original GEDCOM er flyttet. Godkendte data, proband og relationer er bevaret. Prøv også et arbejdsområde med kun dokumenter og ingen baseline.
7. Gentag i lyst/mørkt/systemtema, 100/200 % skærmskalering og minimumsvindue 960×600. Kontrollér rød/blå/grå og synligt fokus.
8. Fjern Graphviz, fremprovokér timeout og skift arbejdsområde under layout. Ingen fastlåst UI, gammel graf i nyt område eller tabt tekst.
9. Kør kontraktens målinger med 1.000/10.000 noder. Registrér første layout, gentaget cachevisning, zoom/panorering og hover med høj grad.
10. Kør samlet build/test samt faktiske platformspakkers graf- og editorflow; notér særskilt, hvis en platform ikke er til rådighed.

## Journal

| Dato | Krav/scenarie | Platform og versioner | Kommando/måling | Faktisk resultat | Evidens/afvigelse |
| --- | --- | --- | --- | --- | --- |
| Ikke kørt | Alle | Ikke registreret | Ikke kørt | Afventer implementering | Ingen accept endnu |
