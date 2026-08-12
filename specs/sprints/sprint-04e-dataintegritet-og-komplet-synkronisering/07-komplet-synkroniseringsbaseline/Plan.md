# Plan: Komplet kanonisk synkroniseringsbaseline

1. Skriv fejlende Core-tests for hver struktureret datatype og for permutationer af ikke-betydende rækkefølge.
2. Skriv fejlende tests for skjulte skabelonfelter, manglende baseline og migrationsversioner.
3. Definér det versionsstyrede kanoniske snapshot og stabile elementidentiteter.
4. Implementér kanonisering og fingerprint uden UI- eller filsystemafhængigheder.
5. Adskil importeret, godkendt og dokumentafledt tilstand i metadata og afstemning.
6. Implementér baseline-migrering eller tydelig manuel gennemgang for ældre dokumenter.
7. Kør mutationsegnet grænsetest for den lille, højrisikofyldte sammenligningskerne.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel uændret genimport og ændringer i event, kilde, census, medie og submitter.
10. Dokumentér resultatet, og stop før feature 08.
