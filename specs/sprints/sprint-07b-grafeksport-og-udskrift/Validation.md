# Validering: Sprint 7B

Status: ikke kørt. Brug samme graf-fixture som [7A](../sprint-07a-personnetvaerk/Validation.md).

1. Sammenlign SVG-elementer med grafens id-mængder og kantliste; kontrollér escaping af `&`, `<`, citationstegn og danske bogstaver. SVG skal åbne offline i en browser og et billedprogram.
2. Kontrollér PNG-filens signatur, dimensioner, aspektforhold, proband og farver. Vis 2048/4096/8192-eksport ved 100 %; ingen utilsigtet skærmopløsning.
3. Render PDF til billede og inspicér 10-personers- og stor-graf-fixtures. Kontrollér A4/A3 og begge retninger, søgbar dansk tekst, margen og klipning af kanter over udsnittets rand. Brug en testafhængighed til PDF-inspektion uden at tilføje det til produktets runtime unødigt.
4. Eksportér hele grafen efter kraftig zoom og et personfilter; alle personer skal stadig være i output. Eksportér derefter aktuelt udsnit og sammenlign klipning.
5. Start eksport og ændr proband/graf bagefter; output følger den fastfrosne revision. Live kamera og hover er uændrede efter eksport.
6. Overskriv en eksisterende fil; fremprovokér diskfejl, ugyldigt JSON, forkert job-id, exit-0/tom fil, timeout, annullering og for stort output. Målfilen er uændret, indtil publicering lykkes.
7. Test jobmappe med mellemrum, æ/ø/å og shelltegn; der udføres ingen ekstra kommando. Kontrollér oprydning ved fejl og efter nedbrud.
8. Frakobl netværket og udfør hele forløbet. Netværksfælder for HTTP-billede, CSS-import og SVG-ressource må registrere nul requests.
9. Vælg Udskriv på macOS, Windows og Linux. Kontrollér PDF-fremviser, systemets printdialog og annullering; test Gem PDF-reserven. Fysisk prøveudskrift registreres særskilt, hvis printer er til rådighed.
10. Kør `dotnet restore`, `dotnet build`, `dotnet test`, Python-tests via `python -m unittest discover -s sidecar/export/tests` og smoketest af hver faktisk pakket AOT-app uden system-Python/Pandoc/Graphviz.

| Dato | Krav/scenarie | OS/arkitektur og værktøjsversioner | Faktisk resultat | Artefakt/evidens |
| --- | --- | --- | --- | --- |
| Ikke kørt | Alle | Ikke registreret | Afventer implementering | Ingen accept endnu |
