# Plan: Ret temaressourcer og statusflader

Status: **implementeret; manuel validering og godkendelse udestår**. Kilde: GUI-anbefaling R9. Alvor: **Minor**. Indsats: **S** (relativ størrelse, ikke et tidsestimat). Afhængighed: **Ingen; udføres tidligt som grundlag for 08**.

## Mål og evidens

Målet er at ret temaressourcer og statusflader. Evidensmærke: **Inferred** (udledt af kode, ingen observeret rendering).

Billedvælgeren bruger AppSurfaceMutedBrush uden fundet definition. Versionshistorik bruger faste statusfarver og altid synlige statusflader. Diffens farver ligger i BackgroundHex i ViewModel.

## Filer og implementeringsgrundlag

- `src/SlaegtsAssistent.App/App.axaml`
- `src/SlaegtsAssistent.App/Views/MediaPickerWindow.axaml`
- `src/SlaegtsAssistent.App/Views/VersionHistoryWindow.axaml`
- `src/SlaegtsAssistent.App/Views/VersionDiffWindow.axaml`
- `src/SlaegtsAssistent.App/ViewModels/VersionDiffViewModel.cs`

Stierne er udgangspunkt fra gennemgangen; kontrollér aktuelle symboler og kaldere før ændringer. Læs [krav](Requirements.md), [validering](Validation.md) og [fælles plan](../Plan.md).

## Implementeringstrin

1. Ret billedvælgerens baggrund til eksisterende AppPanelMutedBrush.
2. Tilføj semantiske ressourcer AppInfoBackgroundBrush, AppErrorBackgroundBrush, AppDiffAddedBackgroundBrush, AppDiffRemovedBackgroundBrush og AppDiffHeaderBackgroundBrush i Light og Dark. Vælg farver efter målt kontrast.
3. Brug eksisterende DiffLineViewModel.Kind til at vælge visningsstil og fjern BackgroundHex efter kontrol af alle brugere. Hold Avalonia-farver i visningslaget.
4. Skjul fejl- og informationsflader uden tekst. Bevar tekstlige tegn eller labels for tilføjede og fjernede linjer.
5. Kontrollér samspil med systemets høje kontrast; brug tema-/systemressourcer og en dokumenteret læsbar reservevisning ved behov.

## Fælles rammer

- Alle brugerrettede tekster og dokumentation er danske; identifikatorer følger .NET-konventioner.
- Genbrug Avalonia Fluent, eksisterende kommandoer, services og semantiske ressourcer. Ingen nye afhængigheder.
- Core forbliver uden UI-afhængigheder; ViewModels må ikke indeholde Avalonia-typer.
- Bevar fri biografitekst, godkendte snapshots, ugemte editorbuffers, kandidatgodkendelse og lokal behandling. Ingen dokumentmigration som sideeffekt.
- Afprøv lyst, mørkt og systemstyret tema, minimumsstørrelse, lange danske tekster, 100 % og 200 % skalering, høj kontrast, tastatur og skærmlæser. Native prøver på macOS, Windows og Linux registreres særskilt; headless-tests erstatter dem ikke.

## Risiko og sikker tilbageførsel

Farveændringer kan gøre diff eller markering utydelig. Kontrollér både tekst og markering før gamle ressourcer fjernes. Hold ændringen afgrænset, så UI-omlægningen kan tilbageføres uden at ændre persondokumenter eller andre brugerdata.
