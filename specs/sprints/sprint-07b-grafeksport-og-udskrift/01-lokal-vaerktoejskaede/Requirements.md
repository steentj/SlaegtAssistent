# Krav: Lokal værktøjskæde og sidecar

Dækker **X-01, X-02, X-08** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- Sidecaren følger protokol v1 og behandler kun kontrollerede jobfiler lokalt.
- Slutbrugeren behøver ingen system-Python eller separat eksportinstallation.
- Tvetydigt/manglende output betragtes som fejl, selv ved exitkode 0.
- Ingen målfil ændres i denne feature; publicering ejes af eksportservicen i feature 02.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
