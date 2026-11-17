# Plan: Tilpas arbejdsbordet til minimumsstørrelsen

Status: **planlagt; ikke implementeret eller godkendt**. Kilde: GUI-anbefaling R1. Alvor: **Major**. Indsats: **M** (relativ størrelse, ikke et tidsestimat). Afhængighed: **Ingen**.

## Mål og evidens

Målet er at tilpas arbejdsbordet til minimumsstørrelsen. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Hovedvinduet tillader 960 × 600, mens kolonnernes minimumsbredder, mellemrum og marginer kræver mindst 1.048 logiske enheder. Topbjælken bruger en ubrudt knaprække.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/ViewModels/MainWindowViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Registrér billeder af nuværende hovedvindue ved 960 × 600 og 1.220 × 760 før ændringer.
2. Gør kontekstpanelet sammenklappeligt med »Vis kontekst«; skjul det som udgangspunkt ved smal bredde. Fastlæg et målt brudpunkt ud fra kontrolbredder og dokumentér det.
3. Gør personlisten justerbar med GridSplitter, minimum 220 og udgangspunkt 260. Bevar arbejdsfladens minimum 420. Kontekstpanelets skjulte kolonne og mellemrum skal frigive plads.
4. Gør topbjælken pladsbesparende i samarbejde med sektion 03. Lad filstier bruge fleksibel bredde og afkortning med fuld tekst i tooltip; ombryd lange personnavne.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Panelbredder kan blive ugyldige ved størrelsesskift. Kontrollér genåbning og at skjulte kolonner ikke optager plads. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
