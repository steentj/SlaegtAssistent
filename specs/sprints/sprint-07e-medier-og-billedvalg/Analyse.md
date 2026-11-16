# Analyse: separat mediemappe og manuelt billedvalg

Status: **planlagt, ikke implementeret**. Analysen bygger på repositoryets kode den 16. november 2026. Ingen brugerdata er åbnet, og appen er ikke afprøvet som led i analysen.

## Ønsket og anbefalingen

Brugeren angiver en separat mappe med GEDCOM-filens medier under Indstillinger, på samme måde som input- og outputmappe. Den genererede medieoversigt skal fortsat være en liste eller tabel. Billeder indsættes kun ved et aktivt valg: højst ét billede øverst i hvert persondokument og et frit antal billeder mellem afsnittene i livsforløbet. En fælles vælger med miniaturevisninger bruges begge steder.

Anbefalingen er sprint **7E**, før AI-sprint 8. Løsningen bygger videre på eksisterende Markdown, editorcache, kandidatopdateringer og lokal eksport. Et miniaturegalleri kræver hverken en ny database, en ny dokumenttype eller cloudtjenester.

## Fund i den eksisterende kode

| Område | Faktisk grundlag | Konsekvens for sprintet |
| --- | --- | --- |
| Indstillinger | `AppSettings` har `DefaultGedcomInputFolder` og `DefaultMarkdownOutputFolder`. `JsonApplicationSettingsService` gemmer atomisk via kildegenereret JSON. | Tilføj valgfri `DefaultMediaFolder` og før feltet gennem alle kopier, Gem/Gem som, indlæsning og tilbagerulning ved gemmefejl. |
| Mappevalg | `SettingsWindowViewModel`, `SettingsWindow.axaml` og `IFolderPickerService` har det relevante mønster. | Genbrug mønstret; byg ikke en særskilt mappeadministration. |
| Mediedata | `Media` indeholder record-id, fil, format, titel, type og note. Den kanoniske personsnapshot indeholder medier. | Personens godkendte snapshot kan levere medieoversigten efter genstart uden ny GEDCOM-import. Inline-medier kan mangle record-id. |
| Stiopslag | `BiographyMediaResolver.Resolve(mediaPath, gedcomDirectory, documentDirectory)` bruger GEDCOM-mappen som relativ rod. Filer uden for GEDCOM-/dokumentrod afvises. | En separat mediemappe kræver et eksplicit ekstra begreb; parameteren `mediaBaseDirectory` i skabelonkonteksten er i praksis dokumentets målmappe og må ikke blot omfortolkes. |
| Automatisk rendering | `BiographyTemplateEditorCompiler.AppendMedia` skriver faktisk `![…](…)`, når feltet Lokal fil er valgt. Standardmodellen vælger feltet. Kataloget tillader kun Standard for medieblokken. | Brugerens ønskede liste/tabel er en eksplicit kravændring i forhold til denne kode. Ret compiler, katalog, standard, hjælp og eksempeldata samlet. Ingen eksisterende dokumenter må omskrives i baggrunden. |
| Dokumentopdeling | `BiographyTemplateMarkdownGenerator.CreateDocumentBody` lægger indhold før `## Biografi` i en genereret sektion. `BiographyGeneratedSectionMerger` bevarer indhold udenfor sektionen. | Topbilledet placeres i en selvstændig, brugerejet blok før den genererede sektion. Livsforløbsbilleder lægges i fri tekst efter sektionen. |
| Editor | `EditorViewModel.MarkdownText` er den aktuelle buffer; Gem og eksportsnapshot bruger den. Markør/tekstmarkering er endnu ikke en del af ViewModel-kontrakten. | Tilføj UI-uafhængige positioner og en revisionskontrol; indsættelse må aldrig læse en gammel diskversion. |
| Preview | `SafeMarkdownPreviewService` læser tilladte lokale billeder og indlejrer dem som data. Den kontrollerer fysiske stier, herunder symbolske links. Editorens rødder og previewcache fastlægges i dag ved oprettelse. | Den nye mappe skal bruges som tilladt rod. Åbne editorer skal kunne opdatere rodpolitik og cache uden at miste ugemt tekst. |
| Eksport | `BiographyExportWindow` samler selv rødder. `ExportResourceCollector` understøtter PNG/JPEG, maks. 20 MiB og 40 mio. pixels, samlet 100 MiB. Preview accepterer flere filtyper. | Fælles indsættelsesformater bliver PNG/JPEG, så alt, brugeren kan vælge, kan eksporteres. Saml rodkontrakten frem for at tilføje mappen ét sted. |
| Versionshistorik | Sprint 7D's Git-service omfatter Markdown-filer direkte i arbejdsmappen. | Billedhenvisninger følger dokumenthistorikken; originale billeder og miniaturecache versioneres ikke. Dette skal fremgå af hjælpen. |

De nævnte filnavne findes under `src/SlaegtsAssistent.Core/Biography`, `src/SlaegtsAssistent.App/Services`, `ViewModels` og `Views`. Dette er en målrettet kodeanalyse, ikke en samlet kvalitetsvurdering af appen.

## Produktbeslutninger for planlægningen

1. **Én global mediemappe**, svarende til de eksisterende globale mapper. Tom værdi bevarer opslag relativt til GEDCOM-mappen. Valgt mappe er tilladt rod; der spørges ikke om godkendelse for hvert billede i den.
2. **Vælgeren starter med personens godkendte GEDCOM-medier.** Brugeren kan skifte til Alle refererede medier i arbejdsområdet og søge i titel/filnavn. Familiefotos kan dermed bruges hos flere personer. Der vises ikke automatisk alle uvedkommende filer fra disken.
3. **Topbillede betyder ét valgfrit billede helt øverst i dokumentets synlige indhold**, før den genererede sektion og dermed normalt før personens titel. Denne konkrete placering undgår at gøre brugerens valg til en del af skabelonens regenererede fakta. Placering under titlen er en mulig senere udvidelse og kræver en anden kompositionskontrakt.
4. **Livsforløbsbilleder indsættes ved markøren**, kun i fri tekst uden for den genererede sektion og uden for topbilledblokken. En markering erstattes kun, når hele markeringen ligger i fri tekst. Ellers gives en besked og ingen ændring.
5. **Almindelig Markdown er den varige repræsentation.** Kun topbilledet får egne markører til udskiftning/fjernelse. Ingen nye frontmatterfelter eller ændring af GEDCOM-baseline er nødvendige.
6. **Ingen automatisk kopiering af originaler.** Henvisninger er relative til dokumentets faktiske mappe. Flyttes kun mediemappen, kan gamle henvisninger bryde; mappeskift omskriver ikke dokumenter. Flyttes dokument- og mediemappe med samme relative struktur, bevares henvisningerne. Forskellige Windows-drev kan ikke forbindes med en relativ sti og skal give en konkret forklaring før indsættelse.
7. **PNG og JPEG i første version**, én fil ad gangen. PDF, TIFF, SVG, HEIC, video og andre medier forbliver i oversigten med format/status, men kan ikke vælges som dokumentbillede. Konvertering, beskæring og billedredigering ligger udenfor sprintet.
8. **Ingen automatisk migration af eksisterende billeder.** Eksisterende medieafsnit kan ændres gennem den sædvanlige kandidatgennemgang. Manuelt indlagte billeder bevares. En gammel Standard-medieblok fortolkes som liste ved fremtidig rendering; skabelonfilen ændres først ved brugerens gemning.

## Risici og afværgning

| Risiko | Planlagt håndtering |
| --- | --- |
| GEDCOM indeholder gamle absolutte Windows-stier på en Mac | Deterministisk ombasering under valgt rod, entydighedskontrol og synlig original/opløst sti; aldrig tavst valg mellem dubletter. Se [Mediekontrakt.md](Mediekontrakt.md). |
| Valg forsvinder ved genrendering eller overskriver livstekst | Brugerejede blokke uden for genereret område; regression af begge kandidatopdateringsveje på aktuelle editorbuffers. |
| Stor mappe eller store billeddimensioner fryser UI | Baggrundsarbejde, begrænset dekodning, sidestørrelse, cachebudget og annullering; målbare grænser i kontrakten. |
| Indstillinger skiftes, mens en vælger eller eksport er åben | Revision af mediekontekst og dokument; gammel vælger kan ikke indsætte. Eksport arbejder på et fast snapshot af rødder og dokumenter. |
| Delvis kobling giver billede i vælger, men ikke i preview/eksport | Fælles sti-/formatpolitik og ende-til-ende-prøver i alle tre eksportformater. |
| Gamle/ødelagte markører eller ændret fri overskrift | Struktur kontrolleres; intet gæt eller global tekstudskiftning. Fri tekst afgrænses ud fra genererede markører, ikke kun overskriftens navn. |
| Godkendte mediedata erstattes af endnu ikke accepteret import | Vælger bruger dokumentets Approved-snapshot; rå import må ikke ændre brugerens mediegrundlag uden kandidatgodkendelse. |

## Afgrænsning og estimat

Omfatter mappeindstilling, robuste lokale stiopslag, liste/tabel, miniaturevælger, topbillede, indsættelse i fri tekst, preview, eksport, hjælp og regression. Omfatter ikke automatisk portrætvalg, ansigtsgenkendelse, ændring af GEDCOM-filen, OCR, billedredigering, træk-og-slip, eksternt filbibliotek, automatisk genbinding af brudte dokumentlinks eller versionering af billedoriginaler.

Estimatet er **15–21 effektive udviklingsdage** for én udvikler med kendskab til koden, inklusive test og fejlretning; manuel accept og ventetid kommer oveni. Det er et samlet funktionssprint, omtrent tre til fire arbejdsuger. Fastlåses kalenderen til to uger, bør leverancen deles efter feature 03, med billedindsættelse i den efterfølgende leverance. Estimatet er usikkert især for platformspecifik billeddekodning og native eksport.
