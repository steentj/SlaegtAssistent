# Plan: Sprint 04F – GEDCOM-metadata og GEDCOM-hjælp

## Features
1. `CREA` er metadata (oprettelsesdato) og ikke en hændelse. Datoen er det valgfri skabelonfelt `person.createdDate` ("Oprettet dato").
2. Private tags (starter med `_`) udelades som hændelser.
3. Hjælp-menuen får "Introduktion til GEDCOM" med tutorial, fejlkilder, reference og infografik.

## Gennemførelse
- Loaderen (`GedcomLoader`) fortolker `CREA > DATE` på personer og ignorerer `CREA` på familier.
- `CanonicalPersonData.CreatedDate` serialiseres kun, når den har en værdi, så eksisterende fingeraftryk for personer uden `CREA` er uændrede.
- Standardskabelonen viser "Oprettet dato" under Fakta. Egne skabeloner udelader feltet ved ikke at bruge `person.createdDate`.
- GEDCOM-hjælpen er et ikke-modalt vindue med to faner: tekst (privat Markdown-preview) og infografik (indlejret Avalonia-ressource). Ingen netværkskald.

## Bemærkninger
- Ændringen af standardskabelonen giver ét ekstra skabelonskift ved næste import af eksisterende dokumenter.
- Tidligere importerede `CREA`-hændelser og private tags vises som fjernede ved næste synkronisering og skal godkendes i differenceoversigten.
