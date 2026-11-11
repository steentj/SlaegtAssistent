# Validering: Datagrundlag og relationer

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: 10 noder og den eksplicitte kantliste i sprintets fixture; A har naboerne B,C,D,E,G. CH er én kant med to linkårsager.
- Automatisk: barnets afviste forælder genindføres ikke fra forældresidens snapshot; ukendt baselineversion giver diagnostik og kendte fakta bevares.
- Integration: flyt original GEDCOM, genåbn med lokal kopi og sammenlign grafidentiteter. Test defekt hash og ufuldstændigt gammelt dokument.
- Demonstration: udskriv kun syntetiske fixture-id'er og relationer i testværten; dokumentér forventet/faktisk mængde i sprintets journal.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | G-01, G-02, G-03 | Ikke registreret | Afventer implementering | Ingen accept endnu |
| 11. november 2026 | Del af G-01 og G-03 | `dotnet test tests/SlaegtsAssistent.Core.Tests --filter FullyQualifiedName~PersonGraphBuilderTests` | 3 tests bestået | Grafbyggerens kantmængde, naboer og diagnostik er testet. Dokumentkilder, godkendte data og integration mangler fortsat. |
| 11. november 2026 | Del af G-02 | `dotnet test tests/SlaegtsAssistent.App.Tests --filter FullyQualifiedName~GedcomSnapshotStoreTests` | 4 tests bestået | Den lokale GEDCOM-kopi kan findes efter sletning af originalen og afviser ændret indhold. Genopbygning af grafen ved opstart mangler fortsat. |
| 11. november 2026 | Del af G-01–G-03 | Målrettede tests af `GraphWorkspaceBuilderTests` og `MarkdownGraphLinkExtractorTests` | 3 Core-tests og 1 App-test bestået | Godkendte forældrefakta og Markdig-linkudtræk er afprøvet isoleret. Arbejdsområdeintegration og genindlæsning mangler. |
| 11. november 2026 | Del af G-01–G-03 | `dotnet test` | 144 Core-tests og 213 App-tests bestået | `WorkspaceGraphSource` er testet med to dokumenter og et ugemt link. Integration i hovedvinduet, opstartsgenindlæsning og øvrige scenarier mangler. |
