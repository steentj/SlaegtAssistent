# Krav: Grafeksport og udskriftsflow

Dækker **X-03, X-04, X-05, X-06, X-07 samt samlet X-01–X-08** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- SVG/PNG/PDF indeholder samme aftalte scene og ikke blot skærmens aktuelt tegnede pixels.
- Opløsning, papir og udsnit respekteres; labels og proband kan sammenlignes med kildescenen.
- Udskriv åbner fremviser med instruks og påstår ikke faktisk printerfuldførelse.
- Exportfejl eller annullering før commit efterlader eksisterende mål uændret.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
