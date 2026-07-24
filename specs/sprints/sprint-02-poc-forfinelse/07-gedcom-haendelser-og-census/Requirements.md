# Feature 07: GEDCOM hændelser og census

## Scope
Udvid Core-domænet og parseren til hændelser (`EVEN` m.fl.) og census (`CENS`) efter mapping-tabellen.

## In scope
- Domænemodel for generiske hændelser og censusposter.
- Parsing af `EVEN`, `BAPM`, `CHR`, `BURI`, `CENS` med underfelter i scope.
- Tests der dækker både enkeltperson og flere hændelser per person.

## Out of scope
- Avanceret semantisk normalisering af historiske datoformater.
- Yderligere tags udenfor scope-tabellen.

## Afhængigheder
- Kan bygges efter feature 06 for ensartet parsermønster.
