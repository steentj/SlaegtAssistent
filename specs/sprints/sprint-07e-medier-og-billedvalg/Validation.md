# Samlet validering: sprint 7E

Status: **ikke kørt – kun planlægning er leveret**. Featuremappernes Validation.md beskriver de konkrete automatiske og manuelle prøver.

## Baseline og automatiske kørsler

Før implementering registreres operativsystem, .NET SDK, commit/arbejdstræ, eksportværktøjer og kendte fejl. Kør:

```bash
dotnet restore
dotnet build
dotnet test
```

Under implementering køres de berørte testklasser efter hver feature. Ved samlet accept køres hele løsningen samt eksisterende lokale eksportintegrationer og projektets platformsspecifikke Native AOT-publishprøver. Journalen skal skelne mellem bestået, fejlet, sprunget over og ikke kørt; eksporter, der kræver private værktøjspakker, kan ikke godkendes alene med mocks.

Genbrug blandt andet `SettingsWindowViewModelTests`, `EditorViewModelTests`, `SafeMarkdownPreviewServiceTests`, skabeloncompiler-/modeltests, `MainWindowViewModelTemplateUpdateTests`, `ExportResourceCollectorTests` og eksportintegrationstests. Nye tekstoperationer og stiopslag får isolerede Core-tests med FluentAssertions.

## Syntetisk prøvegrundlag

Opret en GEDCOM-fil med tre personer, hvoraf to deler et familiefoto. Person A har både record-id-medier og et inline-medie uden id. Gem godkendte snapshots og luk appen, så næste prøve starter uden ny import.

Mappestruktur: `Prøve/GEDCOM`, `Prøve/Medier` med to undermapper og `Prøve/Dokumenter`. Medier omfatter portræt-JPEG med EXIF, landskabs-PNG, samme filnavn i to mapper, en gammel Windows-reference, en unik ombaserbar fil, en manglende fil, et korrupt JPEG, en SVG, en TIFF og en JPEG over grænsen. Tilføj stier med mellemrum, æøå, parenteser, `#` og `%`. Symlinkprøver bruger kun midlertidige lokale data; intet privat materiale.

## Hovedscenario

1. Start fra eksisterende dokumenter, åbn Indstillinger, vælg mediemappen og Gem. Kontrollér at ingen Markdown-fil ændres, og at medieoversigten fortsat er liste/tabel.
2. Åbn person A, vælg topbillede fra miniaturegalleriet og redigér alternativ tekst og billedtekst. Kontrollér billede før titlen i preview og præcis én topblok i Markdown.
3. Skriv tre livsforløbsafsnit og indsæt to billeder mellem afsnittene. Brug både personfilter og Alle refererede medier. Kontrollér at fejlposter ikke kan bekræftes.
4. Fortryd og gentag seneste indsættelse; kontrollér markør, fokus og dirty-status. Prøv indsættelse i faktaområdet og annullering af vælgeren; dokumentet skal forblive uændret ved disse forsøg.
5. Eksportér før Gem til PDF, DOCX og ODT. Alle tre valgte billeder og brugerens ugemte livstekst skal være med. Åbn udskrifts-PDF. Kontrollér faktisk billedindlejring, ingen synlige markører og passende skalering/sideskift.
6. Gem og genstart uden at importere GEDCOM. Billeder og personens medieudvalg skal stadig kunne bruges. Gem som for indstillinger skal også bevare mediemappen.
7. Tilbyd ny GEDCOM og ny skabelon. Afvis først, accepter derefter. Topbillede og hele frie biografitekst inklusive billeder skal bestå tegn for tegn, bortset fra eksplicit brugerredigering.
8. Skift mediemappe med ugemt tekst. Kontrollér at tidligere rod fjernes fra previewtilladelserne, at teksten bevares, og at ingen gamle Markdown-links omskrives automatisk. Ryd indstillingen og verificér GEDCOM-relative fallback.
9. Fjern en originalfil og genindlæs preview/vælger. Kontrollér forståelig advarsel, bevaret henvisning og eksisterende kontrolleret eksportflow. Genopret filen uden dokumentændring.
10. Aktivér lokal versionshistorik, Commit, skift topbillede og indsæt endnu et livsforløbsbillede, Commit igen og rul tilbage. Markdown-billedvalgene skal følge versionen; originale billeder skal være urørte.

## Responsivitet og budget

På en dokumenteret lokal SSD-maskine med mindst 16 GB RAM måles en varm og kold kørsel med 250 referencer og 10.000 filer under mediemappen. Målet er synlig dialog og indlæsningsstatus inden 1 sekund, første side med miniaturer inden 3 sekunder og søge-/markeringrespons inden 200 ms efter katalogindlæsning. Indeksering må gerne fortsætte med status; UI skal hele tiden kunne annulleres. Tider er acceptmål for dette prøvegrundlag, ikke garantier for alle drev.

Registrér forløb og målinger. Instrumentér højst to samtidige dekodninger, højst 60 kort pr. side og højst 64 MiB miniaturecache. Luk/genåbn og skift side 20 gange; cache og bitmapantal må ikke vokse ubegrænset. Afprøv 20 MiB-/40 mio. pixels-grænser, eksportens samlede 100 MiB-budget og indeksgrænsen på 100.000 filer. Store billeder må ikke kortvarigt dekodes ukontrolleret for at lave en lille miniature.

## Platforme og visuel kontrol

| Miljø | Skal afprøves | Status ved planlægning |
| --- | --- | --- |
| macOS Apple Silicon | Native vælger/miniaturer, mappevalg, EXIF, Fortryd/Gentag, eksport, AOT | Ikke kørt |
| Windows x64 | Drevbogstaver, lange stier, relative stier på samme/forskellige drev, case, native UI/eksport/AOT | Ikke kørt |
| Linux x64, hvis distribueret | Casefølsomhed, symlinks, native UI/eksport/AOT | Ikke kørt |
| Alle målplatforme | Lyst/mørkt tema, 200 % skalering, tastatur, læserækkefølge og tilgængelige navne | Ikke kørt |

DOCX og ODT kontrolleres både som ZIP-struktur med indlejrede billeder og visuelt i et lokalt kontorprogram. PDF kontrolleres for faktiske billeder og visuelt renderede sider. Headless-tests erstatter ikke native billed-/tekstfeltprøver.

## Accept og journal

| Dato | Feature/krav | Miljø og test | Resultat/evidens | Afvigelse/opfølgning |
| --- | --- | --- | --- | --- |
| 16-11-2026 | Planlægning | Målrettet læsning af eksisterende kode | Specifikation udarbejdet; produktionskode uændret | Ingen implementeringsprøver kørt |

Ved accept forbindes K01–K18 med featurejournalerne og konkrete testresultater. Blokerende fejl om teksttab, forkert person, automatisk billedindsættelse eller filer uden for de tilladte rødder skal være løst. Sprintet er ikke samlet accepteret, før den aftalte platformmatrix er gennemført eller produktejeren eksplicit har indsnævret leverancens målplatforme.
