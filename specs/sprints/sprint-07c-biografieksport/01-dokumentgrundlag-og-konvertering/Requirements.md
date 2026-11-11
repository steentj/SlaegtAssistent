# Krav: Dokumentgrundlag og konvertering

Dækker **B-01, B-02, B-03, B-04, B-05** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- Brugerens aktuelle tekst er kilden, ikke nygenererede GEDCOM-biografier.
- Samlet eksport har stabile kapitler og fungerende interne personlinks uden app-URI'er.
- Alle aktive/eksterne ressourcer afvises eller erstattes efter kontrakten uden fetch.
- DOCX/ODT er semantiske redigerbare dokumenter; PDF har dansk søgbar tekst.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
