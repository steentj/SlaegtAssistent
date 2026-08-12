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
