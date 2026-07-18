# SlægtsAssistenten

En privatlivsfokuseret desktop-applikation, der transformerer tørre slægtsdata (GEDCOM) til rige, historisk berigede biografier i Markdown-format.

## 🎯 Mission

**SlægtsAssistenten** hjælper slægtsforskere med at opdage og dokumentere historierne, der ligger skjult i slægtstræer. Den læser GEDCOM-filer og kombinerer dem med lokal AI, historiske kilder og handskriftgenkendelse til at skabe levende, detaljerede biografier – alt mens dine data forbliver private på din computer.

### Hovedfunktioner

- **GEDCOM til Markdown**: Konvertér strukturerede slægtsdata til redigerbare, narrative biografier
- **Interaktivt slægtstræ**: Visualisér din slægt med et interaktivt, udforskeligt træ
- **Lokal AI-berigelse**: Brug lokale sprogmodeller (LLM) til at generere historisk kontekst og biografiske detaljer
- **Semantisk søgning (RAG)**: Berig automatisk biografier med relevant indhold fra din lokale samling af historiske PDF-bøger
- **Handskriftgenkendelse (HTR)**: Transskribér og digitalisér scannede historiske dokumenter
- **Multi-format eksport**: Generer trykklare PDF'er, Word-dokumenter (.docx) og OpenOffice-filer (.odt)
- **Privatlivsfokuseret**: Alle data forbliver lokale. Ingen skybaserede tjenester, ingen abonnementer, ingen sporing

## 🏗️ Arkitektur

- **GUI & kerne**: Bygget i **C# med Avalonia UI** (cross-platform: Windows, macOS, Linux)
- **Distribution**: Native AOT-kompilering til enkelt eksekverbar fil per platform
- **Databehandling**: GEDCOM-parsing, Markdown-redigering med live-preview
- **Visualisering**: Graphviz-drevet slægtstræ-rendering med interaktiv SVG-visning
- **AI & processing**: Lokal Ollama-integration til LLM-opgaver
- **PDF & eksport**: Pandoc-baseret dokumentkonvertering via Python-sidecar

## 📋 Projektstruktur

```
SlaegtAssistent/
├── src/
│   ├── SlaegtsAssistent.Core/      # Kernedomain-logik og forretningsregler
│   └── SlaegtsAssistent.App/       # Avalonia UI-applikation
├── tests/
│   ├── SlaegtsAssistent.Core.Tests/
│   └── SlaegtsAssistent.App.Tests/
├── specs/                          # Mission-, vision- og tech stack-dokumentation
└── scripts/                        # Hjælpescripts
```

## 🛠️ Teknologistakken

| Komponent | Teknologi |
|-----------|-----------|
| **GUI-rammeværk** | Avalonia UI (MVVM via CommunityToolkit.Mvvm) |
| **Sprog** | C# (.NET) |
| **GEDCOM-parsing** | GedcomParser NuGet |
| **Markdown** | Markdig til rendering |
| **Grafisk visualisering** | Graphviz → SVG (via Python-sidecar) |
| **Lokal AI** | Ollama + Microsoft.Extensions.AI |
| **PDF-behandling** | PdfPig (C#) + Pandoc (Python) |
| **Dokumenteksport** | python-docx, weasyprint |
| **OCR/HTR** | HuggingFace Transformers (TrOCR, Nougat) |
| **Test** | xUnit |

## 🚀 Kom i gang

### Forudsætninger

- **.NET 10.0** eller nyere
- **Avalonia UI**-afhængigheder (inkluderet via NuGet)
- **Ollama** installeret og kørende lokalt (til AI-funktioner)
- Valgfrit: **Python 3.10+** (til avanceret eksport og handskriftgenkendelse)

### Byg

```bash
dotnet restore
dotnet build
```

### Kør

```bash
cd src/SlaegtsAssistent.App
dotnet run
```

### Test

```bash
dotnet test
```

## 💾 Privatlivs- og databeskyttelse

Al behandling sker lokalt på din computer:
- ✅ GEDCOM-data forlader aldrig din computer
- ✅ AI-modeller kører lokalt via Ollama
- ✅ PDF-søgninger bruger din lokale dokumentsamling
- ✅ Ingen skybaserede tjenester eller API-kald påkrævet
- ✅ Ingen telemetri eller sporing

## 📖 Dokumentation

- **[Mission & Vision](specs/Mission.md)** – Detaljeret vision og UI/UX-designprincipper
- **[Teknologistakken](specs/TechStack.md)** – Arkitektoniske beslutninger og teknologibegrundelse
- **[CHANGELOG](CHANGELOG.md)** – Udgivelsesnoter og projekthistorie

## 📝 Licens

[Tilføj din licens her]

## 👤 Forfatter

Skabt for slægtsforskere, der værdsætter privatlivs- og detaljeret historisk forskning.

---

**Bemærk:** Dette projekt er under aktiv udvikling. Bidrag og feedback er velkomne!
