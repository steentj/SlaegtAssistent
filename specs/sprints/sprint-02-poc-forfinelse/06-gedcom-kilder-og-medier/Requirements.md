# Feature 06: GEDCOM kilder og medier

## Scope
Udvid Core-domænet og parseren til kilder (`SOUR`) og medier (`OBJE`) efter sprintens mapping-tabel.

## In scope
- Domænemodeller for kilde- og mediedata.
- Parsing af `SOUR` og `OBJE` med underfelter i scope.
- Mapping fra parseroutput til domænemodel.
- Testfixtures med kilder/medier.

## Out of scope
- Rendering af billeder eller mediefiler i UI.
- Ikke-aftalte tags udenfor mapping-tabellen.

## Afhængigheder
- Bygger på eksisterende `GedcomLoader`.
