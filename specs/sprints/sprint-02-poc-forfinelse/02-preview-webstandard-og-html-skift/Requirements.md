# Feature 02: Preview med webstandard og HTML-skift

## Scope
Preview skal som standard vise renderet webvisning af Markdown, med mulighed for at skifte til rå HTML.

## In scope
- Ny preview-tilstand i ViewModel (`Web`/`Html`).
- Standardtilstand: `Web`.
- UI-kontrol (radio/toggle) til at skifte tilstand.
- Rå HTML-visning bevares som alternativ.

## Out of scope
- Flere preview-modes.
- Ekstern browserintegration.

## Afhængigheder
- Bygger ovenpå editor/preview fra sprint 1.
