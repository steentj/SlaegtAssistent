# Requirements: Sprint 04F – GEDCOM-metadata og GEDCOM-hjælp

- `CREA` på en person må ikke blive til en hændelse eller en importadvarsel.
- Datoen under `CREA` (`CREA > DATE`) er tilgængelig i skabeloner som `person.createdDate` og vises som "Oprettet dato" i standardskabelonen.
- Skabeloner kan fravælge feltet ved ikke at bruge det.
- Hændelser, hvis tag starter med `_` (på personer og familier), må ikke indgå i hændelser i Markdown og giver ingen advarsel.
- `_FSFTID`, `_UID` og øvrige allerede kendte strukturtags er uændrede.
- Hjælp-menuen skal have "Introduktion til GEDCOM", som åbner et ikke-modalt vindue med introduktion, tutorial, posttyper, faldgruber, officielle referencer og infografikken `Guide_til_GEDCOM-filformatet.png`.
- Al tekst er på dansk, og vinduet henter intet fra internettet.
- Skabelon-cheat sheetet skal nævne `person.createdDate`.
