# Validering: Sprint 7C

Status: delvist kørt; samlet accept er ikke opnået. Se featurejournalerne for faktiske resultater og åbne afvigelser. Faste fixturekrav og formater er beskrevet i [Dokumentkontrakt.md](Dokumentkontrakt.md).

1. Redigér fri tekst og genereret tekst uden at gemme. Eksportér alle tre formater. Sammenlign normaliseret dokumenttekst med bufferens body; Markdown-filernes bytes og dirty-status er uændrede. Frontmatter, sektionsmarkører og ↗ fra UI findes ikke i output.
2. Eksportér en faktisk brugerindtastet ↗ som almindelig fri tekst; den bevares. UI-markører må ikke fjernes med generel tegnsletning.
3. Eksportér samlet dokument med links begge veje og to personer med samme navn. Klik bogmærkelinks i Word, LibreOffice og PDF-fremviser. Kontrollér interne ankre med ens overskrift på tværs af kapitler.
4. Test links til personer uden for udvalget, relative billedstier med æ/ø/å, reference-links, kode der ligner links, symlink uden for tilladt mappe, manglende billede og HTTP-billede. Der er advarsler og nul netværksrequests.
5. Åbn DOCX i Word og ODT i LibreOffice. Kontrollér tabeller over sideskift, billedstørrelse, sidenumre, læselighed og redigerbarhed. Render PDF-sider og gennemse alle sider for klipning og tomme ekstrasider. Gem testartefakter uden virkelige persondata.
6. Vælg personer, filtrér søgningen og ændr aktiv person. Udvalget bevares. Test samlet dokument, separate filer, 0/1/100/101 personer og filnavnskollisioner på tværs af store/små bogstaver.
7. Fremprovokér konverteringsfejl før publicering: ingen målfiler ændres. Fremprovokér diskfejl under tredje atomiske publicering: rapporten viser præcis hvilke filer der blev gemt. Test annullering i begge faser.
8. Udskriv samlet PDF via systemfremviser; test manglende fremviser og Gem PDF-reserve. Appen påstår ikke, at en printer har afsluttet jobbet.
9. Afprøv på pakkede osx-arm64-, win-x64- og linux-x64-apps uden udviklingsmiljø og med afbrudt netværk. Registrér værktøjer, skrifttyper og licensmanifest.
10. Kør build, fuld .NET-testpakke, Python-tests og 7A/7B's centrale scenarier. Regression af import, afvist kandidat, genstart, skabelonændring og personlinks dokumenteres.

| Dato | Krav/scenarie | Platform, kontorprogram og værktøjer | Faktisk resultat | Artefakt/evidens |
| --- | --- | --- | --- | --- |
| Ikke kørt | Alle | Ikke registreret | Afventer implementering | Ingen accept endnu |

## Status 13. november 2026

Eksport og udskrift er implementeret med lokale macOS- og emulerede Linux-værktøjsprøver. Udvalgte layoutprøver er gennemført i LibreOffice. Den fulde manuelle gennemgang, Windows, native Linux, pakket signeret macOS-app og samlet accept af 7A–7C mangler fortsat; de kan ikke udledes af de automatiserede prøver. Se [dokumentgrundlag](01-dokumentgrundlag-og-konvertering/Validation.md), [udvalg og batch](02-udvalg-batch-og-udskrift/Validation.md) og [platform og layout](03-layout-platform-og-afslutning/Validation.md).

Seneste samlede automatiske regression: `dotnet test --no-restore` med både udviklingssidecar og genskabt macOS-sidecar aktiveret gav 152 beståede Core-tests og 290 beståede App-tests uden overspring. Python-suiten gav 17 beståede tests med WeasyPrint- og LibreOffice-integration aktiveret. Den almindelige macOS-apps `--release-biography-export-smoke-test` bestod for PDF, DOCX og ODT. Den nye dialog og editorcache er derudover prøvet i Avalonia headless; en fuld manuel interaktion med native filvælgere og printer er ikke registreret.

15. november 2026: Efter kontrollen af ændrede billeder og afvisning af ufuldstændig PDF bestod 152 Core-tests og 293 App-tests med begge macOS-sidecarintegrationer aktiveret. Python-suiten bestod med 17 tests, herunder de lokale WeasyPrint- og LibreOffice-prøver. Dette ændrer ikke de åbne platform- og distributionskrav ovenfor.

15. november 2026, supplerende batchregression: `dotnet test --no-restore` med begge macOS-sidecarintegrationer aktiveret bestod med 152 Core-tests og 296 App-tests uden overspring. De nye tests kontrollerer bevarelse af eksisterende filer ved konverteringsfejl, annullering før publicering og præcis delvis publicering ved en simuleret fejl på tredje atomiske filskrivning.

Supplerende samlet regression 15. november 2026: 152 Core-tests og 297 App-tests bestod uden overspring med udviklingssidecar, den pakkede sidecar og PDF-tekstkontrol aktiveret. Den tilføjede gennemløbstest dækker ugemt fri og genereret tekst fra editor til PDF, DOCX og ODT samt uændrede Markdown-bytes og dirty-status efter hvert output. Platform- og manuelle acceptkrav er fortsat åbne.

Supplerende layoutregression: En lang URL, som tidligere blev klippet i PDF, ombrydes nu og er kontrolleret både som udtrukket tekst og visuelt. Efter genopbygning af værktøjspakkerne bestod 152 Core-tests, 297 App-tests og 18 Python-tests med relevante lokale integrationer aktiveret.

16. november 2026: Den opdaterede macOS-pakke bestod samlet regression med 152 Core-tests, 297 App-tests og 19 Python-tests, herunder lokale PDF- og LibreOffice-integrationer. Appens funktionstest dannede PDF, DOCX og ODT. En Windows x64-pakke er nu bygget og afprøvet uden netværk under Wine og emulering; prøvefilerne indeholder dansk tekst, billede og links. Et forsøg på at køre Windows .NET SDK under emulering blev afbrudt af miljøfejlen `invalid gdt selector`. Native Windows- og Linux-prøver, distribuerbar macOS-app, manuel gennemgang og fuld licensafklaring er fortsat åbne.
