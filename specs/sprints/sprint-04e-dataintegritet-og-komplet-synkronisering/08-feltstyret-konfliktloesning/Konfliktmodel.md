# Feltstyret konfliktmodel

## Tre sammenlignede tilstande

Hver kandidat viser tre adskilte værdier:

- **Dokument** er den aktuelt synlige, strukturerede værdi i Markdown-dokumentet.
- **Godkendt** er værdien fra den senest godkendte kanoniske baseline.
- **Ny GEDCOM** er værdien fra den aktuelle import.

Et fravalg bevarer dokumentværdien for de felter, dokumentparseren repræsenterer. For strukturer, som ikke kan udledes tabsfrit af Markdown, bevares den senest godkendte kanoniske værdi. Fri tekst uden for de genererede markører kopieres direkte og indgår aldrig i et struktureret feltvalg.

## Stabile feltstier og handlinger

Skalare felter bruger stier som `person.birthDate`. Gentagne data bruger den stabile identitet fra den kanoniske baseline, eksempelvis `person.events[77056F6738DBCD3E1AC9FE57]`, `person.sources[@S1@]` og `person.media[@M1@]`. Identiske gentagelser beholder baselinekontraktens entydige suffiks.

Hver forskel klassificeres som:

- **Tilføj**, når kun den nye import har værdien eller elementet;
- **Ændr**, når begge tilstande har feltet eller elementet med forskelligt indhold;
- **Fjern**, når kun den godkendte baseline har værdien eller elementet.

Listen sorteres ordinalt efter feltsti. Betydende rækkefølge for hændelser, census, noter og medier bevares i den valgte kandidat. Ikke-betydende kildesamlinger kanoniseres igen før fingerprint beregnes.

## Årsager og beslutning

En kandidatrække angiver en eller flere årsager: **GEDCOM**, **Skabelon** og **Migrering**. Manglende markører, manglende baseline og ukendte baselineversioner vises som en særskilt migreringshandling og accepteres aldrig automatisk.

Nye GEDCOM-værdier er valgt som standard. Brugeren kan ændre hvert valg, bevare alle dokumentværdier eller vælge alle nye GEDCOM-værdier. Previewet genberegnes uden fil-, editor- eller snapshotændringer. Først knappen **Anvend valgte** afleverer den samlede beslutning til importens atomiske commit.

En lukning, et fravalg af alle kandidater eller en afvist migrering giver ingen planlagt dokumentændring. En godkendt kandidat til en åben editor anvendes kun i hukommelsen og markerer editoren som ugemt. Et lukket dokument skrives først sammen med resten af importtransaktionen.
