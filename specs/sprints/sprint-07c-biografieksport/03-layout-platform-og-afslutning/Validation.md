# Validering: Layout, platformspakker og afslutning

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Kommandoer: dotnet restore; dotnet build; dotnet test; Python unittest discover fra 7B. Kør hver kommando særskilt og registrér resultat.
- Platformmatrix: OS, arkitektur, .NET/Avalonia, Graphviz, Python/Pandoc/WeasyPrint, font og kontorprogramversion.
- Visuel evidens: syntetiske eksempeloutputs, PDF-renderinger og noter om faktiske kontorprogrammer/systemprint.
- Slutaccept: gennemgå alle tre sprinters Validation.md og markér afvigelser som åbne, indtil de er rettet og afprøvet.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | B-09, B-10, B-11 samt samlet G-/X-/B-krav | Ikke registreret | Afventer implementering | Ingen accept endnu |

## Arbejdsjournal 13. november 2026

På macOS arm64 blev en syntetisk flersidet biografi med tabel, billeder, dansk tekst, noter og personlinks eksporteret via en genskabt privat pakke. PDF blev renderet til billeder; DOCX og ODT blev åbnet og konverteret til PDF med LibreOffice. En skjult ODT-fodnote blev fundet og rettet; efterfølgende LibreOffice-konvertering viste teksten. Siderne blev visuelt inspiceret, men den komplette aftalte fixture og kontrol i Microsoft Word er ikke gennemført.

En foreløbig måling på denne Mac med korte syntetiske biografier viste følgende omtrentlige tider for samlet eksport: 1 person PDF/DOCX/ODT 0,7/0,2/0,2 sek.; 10 personer 0,3/0,2/0,2 sek.; 100 personer 0,6/0,2/0,2 sek. Administreret .NET-hukommelse efter de sidste kald var cirka 4 MiB; børneprocessernes maksimale hukommelse er ikke målt. Målingen dækker ikke store billeder eller den fulde fixture.

Offlinearkivet til macOS arm64 indeholder opdateret sidecar, privat Pandoc 3.13, GMP 6.3.0, referencer og indsamlet licensmateriale og kan genskabes med kontrolsum. Pakket sidecar blev brugt i integrationstest; en ren installeret, signeret/notariseret app er ikke prøvet. Windows og Linux har ikke godkendte platformspakker eller native tests. En fuld gennemgang af alle tredjepartskomponenters oprindelse og distributionsforpligtelser mangler. Samlet accept af trin 12 er derfor ikke opnået.

Et Linux x64-værktøjsarkiv blev efterfølgende bygget i Ubuntu 24.04 under emulering. Den kontrolsumvaliderede, genskabte sidecar blev startet i en separat Ubuntu-container uden netværk, system-Python eller system-Pandoc og dannede PDF, DOCX og ODT med dansk tekst og interne links. Dette er ikke en native platformstest eller en test af en fuld apppakke. Arkivet forudsætter mindst det anvendte glibc-miljø, og licensernes fulde distributionsforpligtelser er ikke gennemgået. Projektets CI er udvidet med en pakket sidecar-test for macOS og Linux, men en fremtidig CI-kørsel er ikke et aktuelt testresultat.

En isoleret Ubuntu 24.04 x64-container med .NET SDK 10.0.403 genskabte Linux-arkivet ved build og kørte de målrettede .NET-tests mod den pakkede sidecar: 8 bestået, 1 udviklingssidecar-test sprunget over. Containeren kørte på macOS under x64-emulering, og resultatet erstatter ikke native UI-, print- eller distributionsprøver.

På denne Mac lykkedes en Native AOT-publicering med `dotnet publish` for osx-arm64, men både `--release-smoke-test` og `--release-export-smoke-test` blev standset af macOS med exitkode 137, som tidligere dokumenteret for 7B. Den almindelige .NET-apps nye `--release-biography-export-smoke-test` bestod med PDF, DOCX og ODT fra den medleverede pakke. Den fejlede AOT-start er ikke en godkendelse af en distribuerbar app.

En anden isoleret Linux x64-kørsel afprøvede pakken fra appens faktiske build-output efter automatisk genskabelse: 14 målrettede .NET-tests bestod, og appens `--release-biography-export-smoke-test` dannede alle tre formater. Dette var fortsat Ubuntu 24.04 under emulering og ikke en native Linux-test.

Et byggescript til en privat Windows-prøvepakke er tilføjet. Det kræver et Windows-miljø med eksplicit angivet Python, Pandoc, native DLL'er, skrifttype og licensmateriale og udfører selv en lokal prøve af de tre dokumentformater. Scriptet er syntakskontrolleret, men er ikke kørt på Windows; der er endnu ingen Windows-pakke eller dokumenteret native Windows-accept.

På denne maskine kunne det kørende udviklingsvindue ikke findes via den tilgængelige computerstyring. Derfor er dialogens native filvalg og printervalg ikke påstået visuelt afprøvet. Avalonia headless-test bekræfter menuen, valgdialogens kontroller og afkrydsning, men erstatter ikke den manuelle native prøve.

## Supplerende offlineprøve 15. november 2026

Den almindelige .NET-apps `--release-biography-export-smoke-test` bestod på macOS under `sandbox-exec` med `(deny network*)`; PDF, DOCX og ODT blev dannet af den private sidecar, og netværksforbuddet gjaldt også børneprocesserne. Prøven brugte syntetisk tekst og beviser ikke den signerede AOT-app eller samtlige billed- og UI-scenarier.

## Lang URL og opdaterede værktøjspakker

En udvidet syntetisk fixture med en URL på over 300 tegn viste, at den oprindelige PDF klippede teksten ved højre side. Den kontrollerede print-CSS er ændret til at tillade ombrydning inde i lange ord. Efter rettelsen blev den fulde URL fundet ved tekstudtræk, det oprindelige hyperlink blev bevaret, og den renderede PDF-side blev visuelt kontrolleret. LibreOffice-prøver af den samme fixture viste også den fulde URL i DOCX- og ODT-output.

Sidecarpakkerne til macOS arm64 og Linux x64 blev genopbygget og genskabt fra opdaterede kontrolsumvaliderede arkiver. Den pakkede macOS-sidecar bestod gennemløbstesten med PDF-tekstkontrol. Den genskabte Linux-sidecar dannede en PDF med hele URL'en i en x64-emuleret Ubuntu-container uden netværk. Den almindelige macOS-apps funktionstest for alle tre formater bestod igen under macOS' netværksforbud. Disse prøver ændrer ikke de åbne native platform- og distributionskrav.

En supplerende fixture med et meget langt personnavn og en URL på over 300 tegn blev eksporteret til alle tre formater. PDF-tekstkontrollen fandt hele navnet og hele URL'en. DOCX og ODT blev åbnet i LibreOffice og konverteret til PDF; tekstudtræk viste også her hele navnet og URL'en. Dette er en syntetisk layoutprøve, ikke en Microsoft Word- eller native Windows-test.

## Supplerende Windows-prøve 16. november 2026

En Windows x64-sidecar blev bygget med Python 3.13.1, WeasyPrint 66.0, Pandoc 3.13 og private MSYS2-biblioteker i Wine under x64-emulering. Byggescriptets udvidede prøver dannede PDF, DOCX og ODT uden netværk og kontrollerede dansk tekst, lokalt billede og links. Arkivet kan genskabes med kontrolsum. Et Windows-specifikt problem med fil-URL’er blev rettet, og PDF-konvertering afviser nu ressourcer, der ikke kan indlæses, frem for at udelade dem lydløst. Appen giver Windows-sidecaren en afgrænset DLL-søgesti.

Et forsøg på at køre Windows .NET SDK 10.0.403 i samme emulerede miljø blev standset af miljøfejlen `invalid gdt selector`; det er ikke en bestået Windows-appbuild. Native Windows, native Linux, fuld visuel gennemgang, print, signeret macOS-distribution og samlet licensafklaring er fortsat ikke godkendt. CI er udvidet til at afprøve Windows-pakken ved en fremtidig kørsel, men denne kørsel er ikke foretaget her.
