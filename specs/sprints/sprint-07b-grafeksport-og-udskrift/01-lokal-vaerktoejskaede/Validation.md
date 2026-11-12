# Validering: Lokal værktøjskæde og sidecar

Status: **påbegyndt, ikke godkendt**. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: process/error/schema/stivalidering og ressourcefælder i både C# og Python.
- Integration: rigtig graph-pdf kan renderes og indeholder dansk tekst, vektorcirkler og forbindelser.
- Manuel: frakobl netværk og fjern værktøjer fra PATH; privat pakke skal fungere eller præcist forklare en manglende komponent.
- Pakning: journalfør version, licensmanifest, platform og kommando; en manglende platform markeres ikke afprøvet.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| 2026-11-11 | Dele af X-02: protokol, stier og ressourcer | `python3 -m unittest discover -s sidecar/export/tests -v`, macOS arm64 | Automatiske test af skema, stier, symbolske links og aktive ressourcer bestået | Ikke en prøve af PDF-rendering eller netværksisolering i den pakkede app |
| 2026-11-11 | Dele af X-02: C#-adapter | `dotnet test`, macOS arm64 | 147 Core-test og 235 App-test bestået, herunder adapterens falske procesrunner | Kontrol af job-id, sti, signatur, annullering og oprydning er testet med falsk runner |
| 2026-11-11 | Indledende værktøjsprøve | Python 3.13.1 og WeasyPrint 66.0 i midlertidigt virtuelt miljø | Import fejlede: native GObject-biblioteker mangler | Ingen PDF produceret; dette er ikke en privat programpakke |
| 2026-11-11 | Graf-PDF på macOS arm64 | Python 3.13.1, WeasyPrint 66.0 og lokalt udpakkede Homebrew-biblioteker i midlertidigt udviklingsmiljø; `SLAEGTSASSISTENT_PDF_INTEGRATION=1` | Rigtig PDF fra tre personer. A4/A3 og begge retninger bestået; dansk tekst kan udtrækkes, og grafen indeholder vektorer | A4 liggende er visuelt inspiceret. Kræver `pypdf` som testafhængighed. Udviklingsmiljøet er ikke en distribuerbar pakke og beviser ikke offline-drift uden systemafhængigheder |
| 2026-11-11 | Oprydning efter nedbrud og fejl | Målrettede App-tests, macOS arm64 | Gamle markerede job slettes; aktive, nye og uvedkommende job bevares. Fejl i svar, overgrænse og manglende værktøj er testet | Windows- og Linux-afprøvning mangler |
| 2026-11-11 | C# til rigtig PDF-sidecar | `LocalExportIntegrationTests` med midlertidig Python-startindpakning og private forsøgsbiblioteker | PDF-signatur accepteret af den rigtige procesrunner og C#-adapter | Startindpakningen er kun til udviklingsprøven; en rigtig programpakke er ikke testet |
| 2026-11-11 | Foreløbig privat macOS arm64-sidecar | `scripts/proeve-pdfvaerktoej-macos.py` med Python 3.13.1, WeasyPrint 66.0, PyInstaller 6.22.3, private forsøgsbiblioteker og DejaVu Sans med licens | Pakken blev bygget og dannede PDF med privat Fontconfig; den blev også startet gennem C#-procesrunneren | Prøvepakken er midlertidig og indgår endnu ikke i en færdig AOT-app. Alle afhængigheders versioner og licenser mangler at blive låst og dokumenteret |
| 2026-11-11 | Linux x64, emuleret container | Python 3.13, WeasyPrint 66.0; `python:3.13-slim` på Docker med `--platform linux/amd64` | Syv Python-tests bestået, herunder A3/A4 og begge retninger | Emuleret miljø, ikke en test af den distribuerede app på en fysisk Linux-maskine |
| 2026-11-11 | Foreløbig pakket sidecar, Linux x64 | `scripts/proeve-pdfvaerktoej-linux.py`, PyInstaller 6.22.3, afprøvet i netværksfrakoblet `python:3.13-slim`-container med PATH uden Python og privat DejaVu Sans/Fontconfig | Graf-PDF dannet med søgbar dansk tekst | Uden privat skrifttype og Fontconfig fejlede forsøget; skrifttype og konfiguration er derfor nødvendige dele af platformspakken. Dette er ikke en AOT-app eller en færdig pakke |
| 2026-11-11 | Opslag og versionskontrol | App-tests og rigtig macOS-prøvepakke | Privat pakke prioriteres; manglende skrifttype, fejlagtig protokol og ugyldig version afvises; gyldig version caches | Afhængighedsversioner og samlede licenser er endnu ikke fastlåst |
| 2026-11-11 | Appens PDF-smoketest på macOS arm64 | Almindelig Debug-build med prøvepakken kopieret til `tools/export/slaegt-export`; `--release-export-smoke-test` | Appen fandt den private sidecar og dannede en PDF | Ikke en distribueret app |
| 2026-11-11 | Native AOT-smoketest på macOS arm64 | `dotnet publish -c Release -r osx-arm64 -p:DistributionBuild=true`, efterfulgt af PDF-smoketest | Bygning lykkedes, men operativsystemet stoppede appen ved start med exitkode 137. macOS-loggen oplyste, at signaturen ikke var betroet | AOT-smoketesten er ikke bestået. Ingen Developer ID Application-identitet fundet lokalt; signering og notarisering mangler |

Der mangler låste og licensdokumenterede værktøjer, privat pakning, integration i appen samt afprøvning af de pakkede apps på macOS, Windows og Linux. Feature 02 og sprintet er ikke færdige.
