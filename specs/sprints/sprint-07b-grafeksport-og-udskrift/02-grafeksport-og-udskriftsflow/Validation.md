# Validering: Grafeksport og udskriftsflow

Status: **delvist afprøvet**. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: id-/kantlister, gyldig SVG, billede/PDF-signatur, målgrænser, annullering og gamle snapshots.
- Visuel: åbn alle tre formater i eksterne programmer; render PDF og kontrollér tekst, farver og kanter.
- Manuel: start printflow, vis systemdialog og annullér; simulér manglende filassociation og gem PDF i stedet.
- Regression: grafen og alle åbne dokumenter er uændrede efter eksport i begge temaer.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| 2026-11-12 | X-07: udskriftsflow | Brugerens manuelle afprøvning på denne Mac | Brugeren melder flowet OK | Tekstplacering er fortsat utilfredsstillende og udskudt til [issue #9](https://github.com/steentj/SlaegtAssistent/issues/9). Øvrige scenarier er ikke dermed godkendt. |
