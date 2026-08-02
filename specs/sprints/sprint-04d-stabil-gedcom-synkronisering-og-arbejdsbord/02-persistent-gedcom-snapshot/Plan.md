# Plan: Persistent GEDCOM-snapshot og rå personsegmenter

1. Skriv failing tests for gemning, indlæsning, hashkontrol og manglende snapshot.
2. Definér et lokalt snapshotformat med kildeidentitet, importtidspunkt, formatversion og rå segmenter pr. record-id.
3. Gem den valgte GEDCOM-fil eller en lokal, kontrolleret kopi sammen med et manifest i arbejdsområdet.
4. Indlæs snapshot ved opstart, så personlisten kan vise rå GEDCOM-data før en ny import.
5. Håndtér korrupt eller manglende snapshot med en tydelig status uden at skjule fejl.
6. Sørg for, at en ny GEDCOM-import erstatter snapshot atomisk og ikke efterlader blandede versioner.
7. Stop for manuel validering efter genåbning af appen og arbejdsmappe.
