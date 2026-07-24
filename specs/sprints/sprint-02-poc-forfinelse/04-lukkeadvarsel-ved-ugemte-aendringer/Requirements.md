# Feature 04: Lukkeadvarsel ved ugemte ændringer

## Scope
Ved lukning med ugemte ændringer skal appen vise dialog med valgene Gem / Kassér / Annullér.

## In scope
- Intercept af lukning når dirty-state er aktiv.
- Tre eksplicitte valg: Gem, Kassér, Annullér.
- `Gem` udfører global gem.
- `Kassér` lukker uden gemning.
- `Annullér` afbryder lukning.

## Out of scope
- Delvis gemning per fil i lukkedialog.

## Afhængigheder
- Kræver feature 03 (global gem og dirty-state).
