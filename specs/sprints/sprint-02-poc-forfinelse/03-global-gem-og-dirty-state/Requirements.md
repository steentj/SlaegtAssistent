# Feature 03: Global gem og dirty-state

## Scope
Tilføj central dirty-state og global `Fil -> Gem`, som gemmer alle ændrede filer i sessionen.

## In scope
- Central registrering af ændrede filer.
- `Fil -> Gem` i menuen.
- Gemmekommando gemmer alle dirty filer.
- Dirty-state nulstilles efter succesfuld gemning.

## Out of scope
- Auto-save.
- Versionshistorik/undo-stack på tværs af filer.

## Afhængigheder
- Bygger ovenpå editorens eksisterende save-funktion.
