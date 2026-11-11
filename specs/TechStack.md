# Applikations-konstitution: SlægtsAssistenten
## Afsnit 2: Teknologisk Stack

Skan- og GUI-laget bygges i C#, mens tunge AI-processer (som lokal OCR) uddegeres til en lille Python-"sidecar".

### 1. Arkitektur & GUI (Cross-Platform)
*   **Sprog:** C# (nyeste .NET version)
*   **GUI Framework:** **Avalonia UI** (MVVM-mønster via `CommunityToolkit.Mvvm`). Dette sikrer ensartet rendering og ægte cross-platform performance på Windows, macOS og Linux.
*   **Designsystem:** Avalonia Fluent-temaet er baseline. FluentAvalonia afprøves kun ved dokumenteret kompatibilitet med projektets Avalonia-version; ellers bruges egne XAML-designressourcer oven på Avalonia Fluent.
*   **Temaer:** Lyst, mørkt og systemstyret tema gemmes i den lokale JSON-indstillingsfil og anvendes ved appstart. ViewModels afhænger ikke af Avalonia-typer.
*   **Distribution:** Kompileres med **Native AOT** til én enkelt eksekverbar fil (`.exe` til Windows, `.app` til Mac) pr. platform. Ingen krav om præinstalleret .NET-runtime hos modtagere af applikationen.

### 2. Data & Tekstbehandling
*   **GEDCOM Parser:** `GedcomParser` (NuGet) til indlæsning og strukturering af slægtstræet.
*   **Markdown Editor & Preview:** `Markdig` (NuGet) til konvertering af Markdown til HTML, som vises i appen via et Avalonia HTML-visningselement (f.eks. AvaloniaWebView) for live-preview.
*   **Persondokumenter:** Markdown-filer bruger versionsstyret YAML-frontmatter med stabilt GEDCOM-record-id og et struktureret faktasnapshot. Den frie biografitekst holdes adskilt fra metadata.
*   **Dokumentsammenligning:** En lokal Core-service normaliserer dokument- og GEDCOM-fakta og producerer deterministiske feltforskelle. Appen viser forskellene og udfører kun ændringer efter brugerens valg.
*   **Eksport-motor (planlagt i trin 12, sprint 7B–7C):** Aktuelle Markdown-biografier, inklusive ugemte editorbuffers, parses til kontrolleret HTML i C#. En lokal Python-sidecar bruger Pandoc til Microsoft Word (.docx / OOXML) og OpenDocument (.odt / ODF), samt WeasyPrint til PDF. Stile, lokale billeder, interne personlinks og fejl kontrolleres efter [dokumentkontrakten](sprints/sprint-07c-biografieksport/Dokumentkontrakt.md). python-docx er ikke en nødvendig runtime-afhængighed i første version.

### 3. Grafisk Visualisering
*   **Layoutmotor (planlagt i trin 12, sprint 7A):** Graphviz `sfdp` kaldes som lokal proces fra C#. Alle kendte personer, godkendte familierelationer og entydige Markdown-personlinks konverteres til DOT. Kraftbaseret netværkslayout læses fra Graphviz plain-format til UI-uafhængige koordinater efter [grafkontrakten](sprints/sprint-07a-personnetvaerk/Grafkontrakt.md).
*   **Interaktiv visning:** En dedikeret Avalonia-tegnekontrol viser prikker og forbindelser med zoom, panorering, hover/fokus, probandmarkering og klik til Markdown-biografi. En statisk SVG og Avalonia.Svg er ikke længere det krævede grundlag for interaktionen. ViewModels og Core forbliver uden Avalonia-typer.
*   **Eksport og udskrift (sprint 7B):** En fælles grafscene eksporteres som selvstændig SVG, PNG eller PDF. PDF bruger lokal WeasyPrint. Udskriv åbner den genererede PDF i systemets fremviser, hvor brugeren vælger printer; appen rapporterer åbning til udskrift, ikke printerfuldførelse. Se [eksportkontrakten](sprints/sprint-07b-grafeksport-og-udskrift/Eksportkontrakt.md).
*   **Værktøjspakning:** Graphviz, Python, Pandoc, WeasyPrint, skrifttyper og native afhængigheder pakkes privat pr. platform. Ingen automatisk download, cloudkald eller krav om systeminstalleret Python/kontorprogram. Native AOT-hovedprogram og platformspakker følger den godkendte [distributions-ADR](sprints/sprint-04e-dataintegritet-og-komplet-synkronisering/11-regression-platform-og-distribution/ADR-001-platformspakker-med-native-biblioteker.md); endelige værktøjsversioner fastlåses efter faktisk kompatibilitetsprøve.

### 4. Lokal AI & RAG Engine
*   **LLM Runtime:** **Ollama** (skal være installeret på brugerens maskine). Ollama kører modeller som *Llama 3 (8B)*, *Mistral* eller dedikerede nordiske sprogmodeller lokalt.
*   **AI Integration i C#:** Microsofts officielle `Microsoft.Extensions.AI` til at kommunikere med den lokale Ollama-instans via lokal REST API (localhost:11434).
*   **PDF Indeksering (RAG):** `PdfPig` (C# NuGet-bibliotek). Appen udtrækker råtekst fra lokale PDF-filer (fra slægtsbibliotek.dk), gemmer et simpelt søgeindeks i hukommelsen (eller en lokal SQLite-database), og vedlægger relevante bogsider som kontekst i AI-prompten.

### 5. Transkribering (HTR / OCR)
*   **OCR-Logik:** Python (kørt som en lokal baggrundsproces/sidecar via C# `Process.Start`).
*   **Modeller:** HuggingFace `Transformers` (f.eks. *TrOCR* eller *Nougat* modeller til historisk håndskrift). Python-scriptet modtager et billede, kører modellen på computerens GPU/NPU, og returnerer teksten til C#-appen.
