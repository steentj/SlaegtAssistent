# Requirements: Persistent GEDCOM-snapshot og rå personsegmenter

- Den senest indlæste GEDCOM-identitet skal gemmes lokalt.
- Rå GEDCOM-segmenter skal gemmes pr. personens record-id.
- Snapshotdata må ikke sendes til cloud eller telemetri.
- Rå GEDCOM-data skal være tilgængelige ved næste opstart uden ny filindlæsning.
- Snapshotformatet skal have formatversion og integritetskontrol.
- Manglende, korrupt eller uforenelig snapshotdata skal vises som en tydelig fejl/status.
- En ny import må ikke blande personsegmenter fra to GEDCOM-versioner.
