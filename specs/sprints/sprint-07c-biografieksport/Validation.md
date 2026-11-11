# Validering: Sprint 7C

Status: ikke kørt. Faste fixturekrav og formater er beskrevet i [Dokumentkontrakt.md](Dokumentkontrakt.md).

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
