# Plan: Indfør menuer og platformens genveje

Status: **implementeret; manuel validering og godkendelse udestår**. Kilde: GUI-anbefaling R3. Alvor: **Major**. Indsats: **M** (relativ størrelse, ikke et tidsestimat). Afhængighed: **Ingen; koordiner med 01**.

## Mål og evidens

Målet er at indfør menuer og platformens genveje. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Hovedvinduets menu indeholder kun eksport og hjælp. Der blev ikke fundet NativeMenu eller hovedvinduesgenveje. Skabeloneditoren har faste Ctrl+Z/Ctrl+Y-bindinger.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/App.axaml`
- `src/SlaegtsAssistent.App/App.axaml.cs`
- `src/SlaegtsAssistent.App/Views/MainWindow.axaml`
- `src/SlaegtsAssistent.App/Views/MainWindow.axaml.cs`
- `src/SlaegtsAssistent.App/Views/TemplateEditorWindow.axaml`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Etablér native menulinje på macOS og vinduesmenu på Windows/Linux med fælles kommandoer og CanExecute.
2. Tilføj »Indlæs GEDCOM…«, »Gem alle«, »Eksportér biografier…«, »Versionshistorik…« og »Anvend skabelon…«. Brug Cmd+O/Cmd+S på macOS og Ctrl+O/Ctrl+S på Windows/Linux.
3. Placér »Indstillinger…« i macOS-appmenuen med Cmd+,; tilbyd den i vinduesmenuen på Windows/Linux. Lad Luk bruge eksisterende beskyttelse af ugemte ændringer.
4. Tilføj Rediger-menu med fokusstyret fortryd, gentag, klip, kopiér, indsæt og markér alt. Brug Cmd+Z/Shift+Cmd+Z på macOS og Ctrl+Z/Ctrl+Y på Windows/Linux.
5. Fjern sjældne topbjælkehandlinger, når de er tilgængelige i menuer. Opdatér skabeloneditorens genveje uden at lade globale bindinger opsluge tekstfeltets egen fortryd.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Globale genveje kan overtage tekstredigering eller udløse kommandoer to gange. Bevar native vinduesramme og eksisterende livscyklus. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
