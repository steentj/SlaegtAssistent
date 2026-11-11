# Dokumentkontrakt: biografier til DOCX, PDF og ODT

Status: planlagt. Normativ sammen med [den fælles eksportkontrakt](../sprint-07b-grafeksport-og-udskrift/Eksportkontrakt.md). Leverancen dækker brugerens DOCX/PDF-ønske og bevarer ODT fra roadmap og mission.

## Hvad eksporteres?

Eksportér det dokument, brugeren faktisk har redigeret, inklusive fri tekst og eksisterende genererede sektioner. Kør aldrig skabelongenerator, GEDCOM-synkronisering eller AI som led i eksport. Frontmatter, interne sektionsmarkører, importdiagnostik, kandidattekst, previewknapper og `↗`-UI-markører er ikke dokumentindhold og skal ikke med.

For hver valgt biografi bruges en åben editors `CreateDocument().Body`, også hvis den er dirty. Ellers læses og parses den gemte fil gennem den eksisterende dokumentparser. I dialogen står **Eksporten medtager dine aktuelle ændringer uden at gemme Markdown-filerne**. Snapshot af alle valgte dokumenter tages ved start. Ingen dirty-status ændres og ingen autosave udføres. En ulæselig, fejlbehæftet eller tvetydig valgt biografi blokerer preflight med en konkret personliste; ingen delvis eksport starter i stilhed.

## Brugerflow og omfang

En handling **Eksportér biografier…** åbner en ikke-modal dialog med native personliste, afkrydsning, vælg alle, søgning og antal valgte. Aktuel person er forvalgt; søgning skjuler ikke allerede valgte personer fra udvalget. Formatvalg: **Word (DOCX)**, **PDF**, **OpenDocument (ODT)**. Resultat: **Ét samlet dokument** (standard) eller **Én fil pr. person**. Samlet dokument følger den viste rækkefølge efter dansk navnesortering og record-id; manuel kapitelrækkefølge og slægtsbogsskabeloner er uden for første version.

Samlet eksport har én titel, **Slægtsbiografier**, og ét kapitel pr. person med sideskift før næste kapitel. Hvert kapitel bruger personens navn som overskrift på niveau 1 og et deterministisk internt bogmærke `person-000001`, … fra ordnet record-id-opslag. Hvis body starter med en niveau-1-overskrift, der præcist er personens normaliserede navn, bruges den som kapiteloverskrift i stedet for at gentage navnet; øvrige overskrifter forskydes så de ligger under kapiteloverskriften, højst niveau 6. Ellers indsættes en kapiteloverskrift og eksisterende bodyoverskrifter forskydes ét niveau. Ingen forside eller indholdsfortegnelse kræves i første version.

I enkeltfiltilstand bruges eksisterende stabil Markdown-basename med den nye formatendelse. Genbrug og udvid kun `BiographyFileNameGenerator`, hvor regler mangler, til ugyldige filnavnstegn og reserverede Windows-navne. Ved sammenfald uden hensyn til store/små bogstaver tilføjes et stabilt kort SHA-256-suffiks af record-id. Preflight viser alle mål; ingen rå record-id'er med `@` eller stiseparatorer bruges direkte som filnavne. Samlet dokument bruger filpicker. Flere filer bruger mappepicker og en samlet gennemgåelig liste over eksisterende mål med valg **Overskriv de viste filer** eller **Annullér**.

PDF har fast A4 stående, 20 mm margen, danske overskrifter, medleveret skrift med æ/ø/å og sidenumre. DOCX og ODT bruger medleverede referencestile med samme sidestørrelse og lignende typografi. Grundskrift 11 pt, linjeafstand omkring 1,35, overskrifter holdes sammen med efterfølgende tekst, billeder skaleres til tekstbredden, tabeloverskrifter gentages over sideskift. Første version lover semantisk ens indhold og læseligt layout, ikke pixelidentiske sideskift mellem Word og LibreOffice. Brugerdefineret referenceskabelon og indlejring af grafen i en slægtsbog ligger uden for scope.

## HTML som fælles mellemformat

Brug Markdig i App med samme understøttede Markdown-udvidelser som preview til parsing af body. Byg en særskilt `BiographyExportHtmlBuilder` og `ExportResourceCollector`: eksportens HTML indeholder semantik og print-stile uden previewets navigation, data-URI-dokument, mørke tema eller brugerinterface. Genbrug rene hjælperfunktioner for sikker lokal stiopløsning/tekstescaping; send ikke `PreviewHtmlDocument` direkte til Pandoc.

Tillad afsnit, lister, overskrifter, fremhævning, blokcitater, kode, tabeller, vandrette linjer, kontrollerede links og lokale rasterbilleder. Rå HTML fra Markdown aktiveres ikke, og kendte interne sektionskommentarer udelades. Øvrig rå HTML vises højst som escaped tekst med en advarsel; ingen scripts/styles/iframes udføres. Diagramkode, LaTeX og vilkårlige filterscripts er ikke særlige eksekverbare eksportfeatures. Bevar deres tekst.

`BiographyExportHtmlBuilder` arbejder på Markdig-AST og en eksplicit whitelist af elementer/attributter. Brug ikke regex til at omskrive Markdown-links, fjerne frontmatter eller skelne kode fra tekst. `BiographyDocumentParser` ejer frontmatter, og parserens eksisterende sektionskontrakt bestemmer interne markører. Linkteksten skrives sikkert som tekst/inlineindhold.

Konverteringskæden er:

1. C# indsamler dokumenter, bygger kontrolleret HTML og kopierer tilladte aktiver til jobmappen.
2. DOCX: sidecar kører privat Pandoc med HTML-input og medleveret `reference.docx`.
3. ODT: samme, med `reference.odt`.
4. PDF: den samme kontrollerede HTML med medleveret print-CSS går til WeasyPrint og dets afgrænsede ressourcefetcher.

[Pandoc understøtter HTML-input, DOCX/ODT, referencefiler og lokale ressourcevalg](https://pandoc.org/MANUAL.html). De præcise argumenter og understøttelse af bogmærker, sidenumre, sideskift og billeder verificeres med faktiske outputfiler i feature 1. Brug kun faste interne argumenter; ingen dokumentdefinerede Pandoc-metadata må ændre skabelon, filtre, program eller ressourceadgang.

## Personlinks, øvrige links og medier

| Indhold | Eksportregel |
| --- | --- |
| Personlink til en anden valgt person i samlet dokument | Internt hyperlink til personens bogmærke, også hvis målkapitlet kommer tidligere |
| Personlink til person uden for samlet udvalg | Bevar synlig linktekst uden lokalt fillink og giv samlet advarsel |
| Personlink i separate filer | Bevar synlig tekst uden link; ingen antagelse om at filer bliver flyttet sammen |
| Eksternt HTTP/HTTPS-tekstlink | Bevar passivt hyperlink; hent aldrig siden |
| `javascript:`, `slaegt-person:`, `file:`, andre aktive skemaer | Fjern destination, bevar tekst, registrér advarsel |
| Dokumentinternt anker | Bevar kun når målet findes; præfiks med kapitel-id i samlet dokument for at undgå sammenfald |
| Lokalt billede | Opløs relativt til kildedokumentets oprindelige mappe før kopiering til jobmappe |
| Manglende, eksternt eller ikke-tilladt billede | Bevar alttekst, indsæt **Billede ikke medtaget**, og vis samlet advarsel uden netværkskald |
| Rå HTML-billede eller SVG fra dokumentet | Ingen indlæsning; samme pladsholderpolitik |

Lokale billeder tillades kun i de samme eksplicitte mapper som dokumentets preview, med fysisk kontrol inklusive symlinks. Første version tillader PNG/JPEG med verificeret filsignatur, højst 20 MiB og 40 megapixel pr. billede, samlet højst 100 MiB jobaktiver. Billeder omdøbes til genererede jobnavne; oprindelige dokumentstier sendes ikke videre som eksterne ressourcer. Der oprettes ingen netværksforbindelse, selv om en Markdown-reference eller metadata indeholder en URL.

Interne bogmærker skal kunne klikkes i DOCX, ODT og PDF. Hvis værktøjsprøven viser manglende understøttelse, ret eksport-HTML/referencefiler eller en afgrænset formatadapter; fjern ikke kravet eller brug private app-URI'er. Fodnoter og tabelindhold skal medtages som understøttet af den valgte Markdown-pipeline og demonstreres i golden-fixturen.

## Job, batch og fejl

Nye rene Core-records under `Core/Export/`: `BiographyExportItem`, `BiographyExportRequest`, `BiographyExportFormat`, `BiographyExportMode`, `BiographyExportDiagnostic`, `BiographyExportResult`. App-services: `IBiographyExportService`, `BiographyDocumentSnapshotService`, `BiographyExportHtmlBuilder`, `ExportResourceCollector`, `BiographyExportService`. `IMarkdownBiographyExportService` er den eksisterende GEDCOM-til-Markdown-generator og skal ikke få ansvar for kontoreksport. Navnesammenfaldet forklares i kode og dokumentation på dansk.

Eksport-ViewModel har `IsExporting`, fase/antal, `CancellationToken`, format, tilstand og valgte id'er; ingen Avalonia-typer. Konverteringer køres sekventielt for at holde hukommelse og værktøjskrav begrænsede. Alle dokumenter, ressourcer og mål valideres før første konvertering. I separat filtilstand konverteres/valideres alle output først, og derefter publiceres de ét ad gangen atomisk. Mængden af filer har ingen fælles filsystemtransaktion. Ved fejl eller annullering under publicering vises præcis **Gemte**, **Ikke gemte** og **Fejl**; allerede gemte filer rulles ikke automatisk tilbage. Der meldes aldrig fuld succes ved delvist resultat.

Ved samlet dokument ændres målet først, når hele filen er valideret. Udskriv biografier bruger samlet PDF og [7B's udskriftsflow](../sprint-07b-grafeksport-og-udskrift/Eksportkontrakt.md). Grænsen er 100 valgte biografier i første version, så midlertidige job og samlet output er håndterbare; UI forklarer grænsen før start. Denne eksportgrænse begrænser ikke grafens personer eller links.

## Layout- og kompatibilitetsaccept

En fast testbiografi indeholder flere sider, æ/ø/å, lange navne/URL'er, niveau-overskrifter, fri tekst, hændelser, noter, kilder, tabel med mindst 60 rækker, billede med billedtekst, manglende billede, kode og personlinks. Eksportér også to personer med ens navn og forskelligt id. Kontrollér tekstindhold, links/bogmærker, billedindhold og sidelayout i PDF-rendering og i faktisk Word eller LibreOffice. Biblioteksinspektion af DOCX/ODT-zip er nødvendig, men beviser ikke alene visuelt korrekt kontorlayout.
