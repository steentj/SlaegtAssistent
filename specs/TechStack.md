# Applikations-konstitution: SlægtsAssistenten
## Afsnit 2: Teknologisk Stack

Skan- og GUI-laget bygges i C#, mens tunge AI-processer (som lokal OCR) uddegeres til en lille Python-"sidecar".

### 1. Arkitektur & GUI (Cross-Platform)
*   **Sprog:** C# (nyeste .NET version)
*   **GUI Framework:** **Avalonia UI** (MVVM-mønster via `CommunityToolkit.Mvvm`). Dette sikrer ensartet rendering og ægte cross-platform performance på Windows, macOS og Linux.
*   **Distribution:** Kompileres med **Native AOT** til én enkelt eksekverbar fil (`.exe` til Windows, `.app` til Mac) pr. platform. Ingen krav om præinstalleret .NET-runtime hos modtagere af applikationen.

### 2. Data & Tekstbehandling
*   **GEDCOM Parser:** `GedcomParser` (NuGet) til indlæsning og strukturering af slægtstræet.
*   **Markdown Editor & Preview:** `Markdig` (NuGet) til konvertering af Markdown til HTML, som vises i appen via et Avalonia HTML-visningselement (f.eks. AvaloniaWebView) for live-preview.
*   **Eksport-motor (PDF, OOXML, ODF):** Markdown-filerne sendes til Python-sidecar'en, som anvender Pandoc samt python-docx og weasyprint. Dette muliggør fejlfri eksport til professionel PDF, Microsoft Word (.docx / OOXML) og LibreOffice/OpenOffice (.odt / ODF) uden tung C#-kodning.

### 3. Grafisk Visualisering
*   **Layout Engine:** Graphviz (afvikles lokalt via Python-sidecar eller C#-wrapper). GEDCOM-data konverteres i appen til en standard .dot-fil. Graphviz beregner automatisk det komplekse, hierarkiske layout og genererer en SVG (Scalable Vector Graphics).
*   **Visning og Udskrift:** SVG-filen indlæses i Avalonia via Avalonia.Svg, hvilket tillader tabsfri zoom, panorering og direkte systemudskrift. Eksport understøtter SVG, PNG og PDF.

### 4. Lokal AI & RAG Engine
*   **LLM Runtime:** **Ollama** (skal være installeret på brugerens maskine). Ollama kører modeller som *Llama 3 (8B)*, *Mistral* eller dedikerede nordiske sprogmodeller lokalt.
*   **AI Integration i C#:** Microsofts officielle `Microsoft.Extensions.AI` til at kommunikere med den lokale Ollama-instans via lokal REST API (localhost:11434).
*   **PDF Indeksering (RAG):** `PdfPig` (C# NuGet-bibliotek). Appen udtrækker råtekst fra lokale PDF-filer (fra slægtsbibliotek.dk), gemmer et simpelt søgeindeks i hukommelsen (eller en lokal SQLite-database), og vedlægger relevante bogsider som kontekst i AI-prompten.

### 5. Transkribering (HTR / OCR)
*   **OCR-Logik:** Python (kørt som en lokal baggrundsproces/sidecar via C# `Process.Start`).
*   **Modeller:** HuggingFace `Transformers` (f.eks. *TrOCR* eller *Nougat* modeller til historisk håndskrift). Python-scriptet modtager et billede, kører modellen på computerens GPU/NPU, og returnerer teksten til C#-appen.