# Plan: Lokal værktøjskæde og sidecar

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

7A feature 02 samt ../Eksportkontrakt.md.

Kravsporbarhed: **X-01, X-02, X-08**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Genbrug procesrunneren; tilføj `ILocalExportSidecar`, request/result-records, kildegenereret JSON og jobmappeforvaltning. Ingen ny generisk jobplatform eller netværksserver.
2. Opret Python-modulerne fra Eksportkontrakt.md med strict schema-/stivalidering, én JSON-respons og faste fejlkoder. Skriv unittest-tests før konverteringskode.
3. Implementér operationen graph-pdf med kontrolleret HTML/SVG, privat font/CSS og WeasyPrint med afgrænset fetcher. Brug en fast 3-node-scene til den første rigtige PDF-prøve.
4. Skriv C#-kontrakttests med falsk runner for ugyldigt output, job-id, sti, timeout, annullering og fejlkoder. Valider PDF-signatur og størrelsesgrænser før bytes accepteres.
5. Tilføj Python-tests for netværksforbud, symlinks, ugyldig operation, rå HTML/SVG-ressource og output uden for jobroden. Værktøjet skal afvise kendte aktive ressourcer før WeasyPrint kaldes.
6. Afprøv private Python/WeasyPrint/native biblioteker på osx-arm64, win-x64 og linux-x64. Fastlås fungerende versioner og hash/låsefiler; medtag font- og værktøjslicenser i lokalt manifest. Skabelonen til pakning skal understøtte Graphviz fra 7A.
7. Implementér oprydning i finally og sikker genstartsrensning. Dokumentér udviklerkommandoer og pakningskommandoer på dansk; intet installeres automatisk i brugerens globale miljø.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
