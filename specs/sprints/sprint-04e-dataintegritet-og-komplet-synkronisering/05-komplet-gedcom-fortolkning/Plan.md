# Plan: Komplet GEDCOM 5.5.1-fortolkning

1. Fastlæg og dokumentér mappingtabellen ud fra GEDCOM 5.5.1 og Missionens datakontrakt.
2. Skriv fejlende Core-tests med testdatasæt for fortsættelseslinjer, citationer, strukturreferencer, ukendte hændelser og alle understøttede tegnsæt.
3. Skriv referenceoutputtests for den normaliserede domænemodel og rå segmenter.
4. Refaktorér parsertilstanden, så posttyper, strukturtags, hændelser og citationer ikke deler fejlagtig standardfortolkning.
5. Implementér rekursiv tekstsammensætning og fuld citationmapping.
6. Implementér encodingdetektion og ANSEL-konvertering med tydelige fejlgrænser.
7. Bevar ukendte hændelser uden at gøre alle ukendte tags til hændelser.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel import af en repræsentativ GEDCOM 5.5.1-fil og sammenlign rådata med domæneoutput.
10. Dokumentér resultatet, og stop før feature 06.

## Udvidelse: notattidspunkt

1. Fortolk `CREA` og `CHAN` på delte notatposter og vælg det seneste gyldige dato- og klokkeslæt.
2. Bevar tidspunktet ved henvisninger, genimport og godkendt baseline uden at ændre notatteksten.
3. Vis tidspunktet i kursiv efter hvert notat og test liste, tabel og forhåndsvisning.
4. Formatér tidspunktet efter region, uden at ændre den gemte værdi eller sammenligningen af sekunder.
