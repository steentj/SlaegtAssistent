# Plan: Atomisk dokumentlagring og gendannelse

1. Skriv fejlende tests, der afbryder skrivning før tempfil, efter tempfil, under flush og før erstatning.
2. Skriv fejlende App-tests for bevaret dirty-state og dansk fejl ved skrivefejl.
3. Definér den fælles lagringskontrakt og et lagringsresultat uden Avalonia-typer.
4. Implementér tempfil, flush, atomisk erstatning og sikker oprydning pr. platform.
5. Før Markdown, settings og relevante manifester gennem den sikre lagringsgrænse.
6. Refaktorér dubleret skrive- og fejlhåndtering væk.
7. Kør målrettede tests, hele testpakken og build.
8. Gennemfør manuel test med en kopi af et dokument og en kontrolleret skrivefejl.
9. Dokumentér resultatet i `Validation.md`, og stop før feature 02.
