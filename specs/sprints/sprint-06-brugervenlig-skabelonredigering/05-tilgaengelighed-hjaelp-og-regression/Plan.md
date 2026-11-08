# Plan: Tilgængelighed, hjælp og regression

1. Skriv headless UI-tests for tab-rækkefølge, fokus, tilgængelige navne, fejltekst og synlige handlinger.
2. Gennemgå kontrast, fokus, skalering, tekstombrydning og brug uden mus på de understøttede platforme.
3. Tilføj korte danske hjælpetekster fra et centralt katalog og erstat det syntaksorienterede skabelon-cheat sheet.
4. Forbered ens lokalt testdatasæt og opgaveark til alle brugertestdeltagere uden at indsamle unødige personoplysninger.
5. Gennemfør testen med mindst tre uerfarne it-brugere; registrér tid, behov for hjælp og konkrete observationer.
6. Ret kritiske og alvorlige fund testdrevet og gentag berørte scenarier.
7. Kør regression for standard- og egne skabeloner, importpreflight, preview, personlinks, kandidater, fri tekst og genstart.
8. Kør `dotnet build` og `dotnet test`, udfyld sprintets samlede validering og stop for endelig godkendelse.

## Status den 8. november 2026

- Implementering af hjælp, tilgængelighedsnavne, fokus, fast handlingsområde og automatiske regressionstests er klar.
- Build og alle 341 tests består. Opgaveark og ens eksempelgrundlag er forberedt i `Brugertest.md`.
- Invitationer, brugertest, native kontrol på understøttede platforme og endelig godkendelse afventer faktisk manuel gennemførelse. Resultater registreres i `Validation.md`; automatiske tests erstatter ikke disse porte.
