# Plan: Fejltolerant import og synlige diagnostikker

1. Skriv fejlende Core-tests for én defekt post mellem flere gyldige poster og for reelt fatale filfejl.
2. Skriv fejlende App- og rendererløse UI-tests for importoversigt, filtrering og delvis accept.
3. Definér en struktureret importrapport og klare regler for fatal kontra recoverable fejl.
4. Implementér mindst mulig gendannelse i parseren og bevar alle diagnostikker.
5. Integrér rapporten i forhåndskontrollen og kræv godkendelse af delvise importer.
6. Vis diagnostikker på dansk med navigation til person eller rå post, hvor muligt.
7. Opdatér modstridende legacy-tests uden at svække reelle fatalfejl.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel import med gyldige poster omkring en defekt post samt en fatal filfejl.
10. Dokumentér resultatet, og stop før feature 07.
