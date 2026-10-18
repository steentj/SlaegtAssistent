# Validation: Sprint 04F – GEDCOM-metadata og GEDCOM-hjælp

- [x] Core- og App-tests er grønne.
- [x] `CREA` giver `Person.CreatedDate` og ingen hændelse (`GedcomCompletenessTests`).
- [x] Private `_`-tags giver hverken hændelse eller advarsel for personer og familier.
- [x] `person.createdDate` kan vises og udelades i skabelon (`BiographyTemplateContractTests`).
- [x] Kanonisk JSON er uændret for personer uden `CREA`.
- [x] Hjælp-menuen åbner GEDCOM-introduktionen (`ShowGedcomHelpCommand`), og vinduet indlæser infografikken.
- [ ] Manuel kontrol: importér `Miscellaenous/TestGedComData/ThraneJacobsen.ged` og kontrollér, at hændelser kun indeholder rigtige hændelser, at "Oprettet dato" vises under Fakta, og at GEDCOM-hjælpen vises korrekt i lyst og mørkt tema.
