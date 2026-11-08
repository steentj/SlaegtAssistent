# Validation: Guidet blokeditor

- [x] En blok kan tilføjes, skjules, vises og flyttes med mus og tastatur.
- [x] Faktafelter kan vælges og ordnes uden tekniske feltnavne.
- [x] Understøttede liste-/tabelvalg kan ændres; ugyldige valg kan ikke foretages.
- [x] Fortryd og gendan virker for alle modelændringer i sessionen.
- [x] Nulstilling kræver bekræftelse og kan annulleres.
- [x] Ingen standardskærm viser `{{`, `#each` eller engelske feltstier.
- [x] Gem, annuller og fejl er synlige ved minimumsstørrelse og 200 % skalering.
- [x] ViewModel-tests og relevante headless UI-tests består.
- [x] Manuel tastaturgennemgang er godkendt.

**Godkendelsesdato:** 2026-11-08
**Godkendt af:** Brugeren
**Bemærkninger:**

- Rød kørsel: den målrettede App-test fejlede som forventet, fordi `TemplateEditorViewModel` og `ITemplateEditorConfirmationService` endnu ikke fandtes.
- Grøn målrettet kørsel: 9 af 9 ViewModel- og headless UI-tests bestået.
- Headless-kontrollen verificerer minimumsstørrelse, primære handlinger, fejlfelt, seks tastaturgenveje og fravær af synlig skabelonsyntaks.
- Samlet build: bestået med 0 warnings og 0 errors.
- Samlet regression: 289 af 289 tests bestået.
- Mus/tastatur ved faktisk kørsel og layout ved 200 % skalering afventer manuel godkendelse.
- Brugeren godkendte trin 2 med beskeden “fortsæt”.
