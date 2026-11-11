# Krav: Datagrundlag og relationer

Dækker **G-01, G-02, G-03** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- Input med samme record-id skaber ikke flere noder. En person med manglende/tvetydigt dokument kan indgå, men har ingen navigerbar dokumentsti.
- Alle kendte direkte relationer og reverse Markdown-naboskab er med; familieroller bruges aldrig til at gætte køn.
- En repræsenteret tom dokumentværdi eller afvist relation må ikke erstattes af importreserven.
- Ingen filskrivning, kandidater eller autosave sker ved snapshotopbygning.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
