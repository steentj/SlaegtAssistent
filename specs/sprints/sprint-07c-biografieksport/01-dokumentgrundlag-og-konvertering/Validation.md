# Validering: Dokumentgrundlag og konvertering

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: snapshot-/AST-/ressourcetests med æ/ø/å, nullmetadata, rå HTML og markeringslignende kode.
- Python: faste Pandoc-argumenter og fejlkoder; ingen uautoriseret ressourceadgang.
- Integration: sammenlign udpakket DOCX/ODT-tekst og PDF-tekst med forventede afsnit og links.
- Visuel prøve: gennemse alle sider i PDF og faktisk kontorprogram; journalfør værktøjer og artefakter.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | B-01, B-02, B-03, B-04, B-05 | Ikke registreret | Afventer implementering | Ingen accept endnu |

## Arbejdsjournal 2026-11-12

Dokument-snapshot, et første HTML-grundlag, lokal billedindsamling og sidecarens protokol- og inputkontrol er påbegyndt. Snapshot-, HTML-, billed- og protokoltests er tilføjet. Overskriftsankre får personafhængige id'er; HTML fra dokumenter aktiveres ikke. Ændringerne er endnu ikke tilsluttet et brugerflow.

- `dotnet test --no-restore`: 412 bestået, 2 eksisterende sidecar-integrationstests sprunget over.
- `python3 -m unittest discover -s sidecar/export/tests`: 9 bestået, 1 integrationstest sprunget over.
- Ingen faktisk biografikonvertering eller visuel kontrol er udført. Privat Pandoc, referencefiler og tilslutning fra appen mangler. Fodnoter, lokale ankre, billedhåndtering og semantik skal verificeres i faktiske DOCX-, ODT- og PDF-filer, inden featureen kan accepteres. Platformspakker og kontorprogrammer er ikke afprøvet.

### Fortsat arbejde

Appens sidecar-adapter kan nu oprette et isoleret biografijob og kontrollere svarets identitet og outputformat. Simulerede procestests dækker PDF-jobbet og afvisning af ugyldigt DOCX-output. En lokal udviklingsprøve med systemets Pandoc har dannet DOCX og ODT i sandbox med dansk tekst, indlejret billede og internt bogmærke. Prøven bruger midlertidige standardreferencefiler og beviser ikke den pakkede version eller visuelt layout. Pandoc krævede indlejring af kontrollerede billeder i HTML-input; direkte filopslag blev afvist i sandbox.

`dotnet test --no-restore`: 414 bestået og 2 sprunget over. `python3 -m unittest discover -s sidecar/export/tests`: 10 bestået og 1 sprunget over. Den fulde fixture, PDF-konvertering, private pakker, rigtige referencefiler og appens eksportflow er fortsat ikke verificeret.

### Tabel og fodnoter

En prøve med HTML fra den faktiske Markdig-builder afslørede, at sidecarens kontrol afviste tabellers tekstjustering. Kontrollen tillader nu kun de konkrete justeringsværdier på tabelceller. Fodnoter får særskilte ankre pr. kapitel. Den genererede HTML med dansk tekst, tabel og fodnote blev efterfølgende accepteret af sidecaren og konverteret med lokal system-Pandoc til DOCX og ODT; zip-inspektion viste tekst, tabel og fodnotetekst. Dette er ikke en visuel kontrol eller en test af de pakkede værktøjer.

`dotnet test --no-restore`: 415 bestået og 2 sprunget over. `python3 -m unittest discover -s sidecar/export/tests`: 10 bestået og 1 sprunget over. Den fulde fixture, PDF, referencestile og eksportflowet mangler fortsat.

### PDF-prøve på macOS arm64

Den kontrollerede print-CSS har nu sidenumre. WeasyPrint 66.0 blev afprøvet lokalt med de eksisterende private native biblioteker og den medleverede skrifttype: PDF med dansk søgbar tekst, billede, internt anker og A4-format samt et flersidet dokument med sidenumre bestod. En separat prøvepakke bygget fra den opdaterede sidecar dannede også en biografi-PDF med dansk tekst, fem ankre og sidenummer. Den eksisterende distributionspakke blev ikke ændret, og prøvepakken er ikke en godkendt distribution.

`SLAEGTSASSISTENT_PDF_INTEGRATION=1 ... python -m unittest discover -s sidecar/export/tests`: 13 bestået. `dotnet test --no-restore`: 415 bestået og 2 eksisterende integrationstests sprunget over. Der mangler fortsat visuel vurdering, samlet fuld fixture, referencestile, platformspakker og brugerflow.

### Kontorformater: kapitelmål og sideskift

Lokal Pandoc 3.13 omsatte kapitel-id på en HTML-sektion til en ODT-sektion uden tilsvarende bogmærke. Personmålet placeres derfor nu i kapiteloverskriften; zip-inspektion af DOCX og ODT bekræfter, at et personlink peger på et eksisterende bogmærke. En separat lokal prøve viste, at Pandoc ikke bevarede CSS-sideskift. Den afgrænsede adapter i dokumentkontrakten indsætter kontrollerede sideskift mellem kapitler; XML-tests bekræfter ét sideskift i hvert af de to formater. Ingen af prøverne beviser visuelt layout eller opførsel i kontorprogrammer.

`SLAEGTSASSISTENT_PDF_INTEGRATION=1 ... python -m unittest discover -s sidecar/export/tests`: 14 bestået. `dotnet test --no-restore`: 415 bestået og 2 eksisterende integrationstests sprunget over. Medleverede referencefiler, privat Pandoc, fuld fixture og brugerflow mangler fortsat.

### Referencestile

DOCX- og ODT-referencer er dannet fra lokale Pandoc 3.13-standardreferencer med et versionskontrolleret script. Scriptet indstiller A4, 20 mm margin, dansk sprog, 11 pt grundskrift, linjeafstand omkring 1,35 og sammenhængende overskrifter. To kørsler i samme miljø gav identiske SHA-256-summer. Prøvepakkescripterne kopierer referencefilerne; de leverer ikke Pandoc.

Lokal Pandoc-konvertering bekræfter A4 og margen i både DOCX- og ODT-output. `SLAEGTSASSISTENT_PDF_INTEGRATION=1 ... python -m unittest discover -s sidecar/export/tests`: 15 bestået. Stilenes visuelle resultat, faktiske kontorprogrammer, privat Pandoc og distributionslicenser er endnu ikke afprøvet eller godkendt.

### Supplerende integration

En faktisk pakkeprøve med appens C#-sidecaradapter dannede PDF, DOCX og ODT med syntetisk dansk tekst, 60 tabelrækker, billede og personlinks. Markdig-tabeller accepteres med begrænset justering, og ODT-fodnotens struktur er rettet efter en LibreOffice-prøve, så noteteksten faktisk vises. En lokal netværksfælde registrerede ingen hentning af et passivt tekstlink; et eksternt billede blev afvist. Den seneste testsuite omfattede også en restaureret macOS-pakke og en emuleret Linux x64-pakke; øvrige platforme og fuld visuel accept er fortsat åbne.

Rå HTML-billeder erstattes nu af en pladsholder med eventuel alternativ tekst, og symbolske billedlinks ud af de tilladte mapper afvises i en målrettet test. Gentagne fodnotereferencer og personlinks med reference-definition og fragment har særskilte regressionstests.

## Editor til faktisk output, 15. november 2026

En integrationstest oprettede et syntetisk arbejdsområde, åbnede en editor og ændrede både en genereret sektion og fri tekst uden at gemme. Snapshot, forhåndskontrol, pakket sidecar, batchtjeneste og atomisk filskriver blev derefter anvendt til samlet PDF, DOCX og ODT. Begge ændrede tekster blev fundet i de tre outputfiler; PDF blev læst med `pypdf`, og kontorformaternes dokument-XML blev inspiceret. Frontmatter og sektionsmarkører blev ikke fundet. Efter hvert format var Markdown-kildefilens bytes uændrede, og editoren var fortsat dirty. Den målrettede test bestod på macOS med den medleverede sidecar; PDF-tekstkontrollen kræver testvariablen `SLAEGTSASSISTENT_TEST_PDF_READER_PYTHON` med `pypdf` installeret.
