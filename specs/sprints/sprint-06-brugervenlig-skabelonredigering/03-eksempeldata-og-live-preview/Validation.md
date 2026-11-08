# Validation: Eksempeldata og live preview

- [x] Et fyldestgørende preview vises uden indlæst GEDCOM.
- [x] En faktisk indlæst person kan vælges og fravælges igen.
- [x] Flytning, skjul og liste-/tabelvalg ses automatisk i preview.
- [x] Hurtige ændringer viser kun det seneste renderingsresultat.
- [x] En renderingsfejl bevarer sidste gyldige preview og viser en dansk fejl.
- [x] Samme model og person giver samme Markdown i editor- og produktionsflow.
- [x] Eksterne links, billeder og rå HTML medfører ingen netværkskald eller scriptkørsel.
- [x] Målrettede Core-, App- og sikkerhedstests består.
- [ ] Manuel preview-sammenligning er godkendt.

**Godkendelsesdato:**
**Godkendt af:**
**Bemærkninger:**

- Rød Core-kørsel: testen fejlede som forventet, fordi `BiographyTemplateExampleData` endnu ikke fandtes.
- Grøn målrettet kørsel: 1 af 1 Core-test og 6 af 6 previewtests bestået; samlet editorsuite 15 af 15.
- Latest-result-wins er testet med en previewservice, som bevidst ignorerer cancellation og afslutter en ældre rendering sidst.
- Sikkerhedskontrollen genbruger `SafeMarkdownPreviewService`; CSP blokerer scripts og netværksforsøg er 0.
- Samlet build: bestået med 0 warnings og 0 errors.
- Samlet regression: 296 af 296 tests bestået.
- Manuel sammenligning af webpreview og hovedvinduets preview afventer brugerens godkendelse.
- Kontrol før trin 4 den 8. november 2026: 35 målrettede Core-, editor- og sikkerhedstests bestod. Den manuelle godkendelse er fortsat åben. Åbning fra Indstillinger og overførsel af alle indlæste personer er efterfølgende koblet på i trin 4.
