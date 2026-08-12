# Plan: Robust frontmatter, migrering og katalogdiagnostik

1. Skriv fejlende Core-tests for dublerede nøgler, ugyldige værdier, manglende felter og formatversioner.
2. Skriv fejlende katalog- og ViewModel-tests, hvor én defekt fil ikke skjuler gyldige dokumenter.
3. Definér parse-resultat, fejltyper og versionsmigrering uden UI-afhængigheder.
4. Implementér sikker parsing og eksplicit validering af formatversion og record-id.
5. Implementér migrering som kandidat, der ikke skrives før brugerens godkendelse.
6. Vis dokumentfejl og tvetydigheder på dansk i personliste eller særskilt statusvisning.
7. Kør målrettede tests, hele testpakken og build.
8. Gennemfør manuel opstart med gyldig fil, defekt fil, ukendt version og dublet.
9. Dokumentér resultatet, og stop før feature 04.
