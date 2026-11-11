# Krav: Layout, platformspakker og afslutning

Dækker **B-09, B-10, B-11 samt samlet G-/X-/B-krav** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- PDF, DOCX og ODT er visuelt kontrollerede og indeholder den aftalte semantik.
- Alle runtime-afhængigheder leveres i private pakker med lovlig distribution og reproducerbar opsætning.
- Ingen platform erklæres understøttet udelukkende ved cross-build eller headless test.
- Trin 12's samlede accept omfatter både graf, billed-/dokumenteksport og det aftalte udskriftsflow.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
