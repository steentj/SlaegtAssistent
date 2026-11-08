# Validation: Editormodel og generering

- [x] En standardmodel genererer et semantisk gyldigt dokument.
- [x] Samme model og data giver byte-identisk tekst ved gentagne kørsler.
- [x] En skabelon i det nye format kan indlæses igen med samme model.
- [x] Skjulte og flyttede blokke bevares ved genåbning.
- [x] En ukendt modelversion afvises med dansk forklaring.
- [x] Core-projektet har ingen ny Avalonia-afhængighed.
- [x] Målrettede Core-tests består.
- [x] Den genererede standardskabelon er manuelt gennemgået og godkendt.

**Godkendelsesdato:** 2026-11-08
**Godkendt af:** Brugeren
**Bemærkninger:**

- Rød kørsel: `dotnet test tests/SlaegtsAssistent.Core.Tests/SlaegtsAssistent.Core.Tests.csproj --no-restore --filter FullyQualifiedName~BiographyTemplateEditorModelTests` fejlede som forventet, fordi trin 1-typerne endnu ikke fandtes.
- Grøn målrettet kørsel: 8 af 8 tests bestået.
- Core-regression: 128 af 128 tests bestået.
- Samlet build: bestået med 0 warnings og 0 errors.
- Samlet regression: 280 af 280 tests bestået.
- Standardskabelonen er låst af en uafhængig byte-for-byte snapshot-assertion. Brugeren godkendte trin 1 med beskeden “fortsæt”.
