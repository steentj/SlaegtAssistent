# Validation: Global gem og dirty-state

## Definition of Done
- [ ] Dirty-state sættes når bruger redigerer.
- [ ] `Fil -> Gem` gemmer alle dirty filer.
- [ ] Dirty-state nulstilles efter gemning.

## Verifikation
1. Kør relevante tests i `tests/SlaegtsAssistent.App.Tests`.
2. Manuel test: redigér flere personer, brug `Fil -> Gem`, genindlæs og bekræft persistens.
