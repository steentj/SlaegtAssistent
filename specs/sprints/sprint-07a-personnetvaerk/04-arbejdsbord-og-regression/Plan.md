# Plan: Arbejdsbord, proband og regression

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

Feature 01–03 med grønne målrettede tests.

Kravsporbarhed: **G-09, G-10, G-11 samt samlet G-01–G-12**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Tilføj fanerne Graf og Biografi omkring den centrale arbejdsflade. Bevar eksisterende personliste, editor/preview, kontekstpanel og justerbare skillelinjer. Ved første åbning vælges Graf, hvis et ikke-tomt datagrundlag findes; ellers fortsætter den kendte dokumentarbejdsflade med tomtilstand.
2. Opret `MainWindowViewModel.Graph.cs` og en lille koordinatorservice til grafrevisioner og abonnementer. Tilføj afhængigheder i `App.axaml.cs` uden at gøre eksisterende konstruktørtests afhængige af native værktøjer. Grafdata indlæses asynkront efter dokumentkataloget.
3. Indfør en fælles sikker `TryOpenPersonDocument(recordId)`-indgang eller en adapter til eksisterende `OpenPersonDocument`. Opslag sker i `_allPeople`, kontrollér dubletter og fileksistens ved brug. Genbrug editorcache og ryd personfilter som eksisterende navigation gør. Skift først til Biografi efter vellykket åbning.
4. Tilslut søgning over alle grafnoder, centrering/fokus, Åbn biografi og Brug som proband. Navigation ændrer ikke proband; probandskift invaliderer ikke layout.
5. Implementér `graph-settings.json` pr. arbejdsområde gennem atomisk writer og kildegenereret JSON. Ukendt version/defekt fil giver standard og besked, ikke en opstartsfejl eller filoverskrivning uden brugerhandling.
6. Opdatér ved publiceret import, katalog-/bufferændring og genåbning. Under import navigeres ikke. Ved arbejdsområdeskift annulleres gammel graf, cache/abonnementer frigives, og ingen gammel proband eller graf genbruges på tværs af mapper.
7. Tilføj `GraphWorkspaceIntegrationTests` og udvid navigation/headless tests. Afslut med 7A's samlede manuelle scenarier og platformspakkers grafvisning; dokumentér reelle resultater.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
