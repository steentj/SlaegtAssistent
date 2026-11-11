# Plan: Layout, platformspakker og afslutning

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

Alle tidligere features i 7A, 7B og 7C.

Kravsporbarhed: **B-09, B-10, B-11 samt samlet G-/X-/B-krav**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Saml en syntetisk golden-fixture med alle indholdstyper og flersidet tabel/billeder. Opret reproducerbar kommando til at generere alle formater og rendere PDF til QA uden virkelige persondata.
2. Inspicér alle PDF-sider og faktisk DOCX/ODT-layout; ret kun konkrete layoutfejl i CSS/referencefiler og knyt dem til regressionsscenarier. Dokumentér eventuelle kontorprogramforskelle uden at acceptere klippet indhold eller ødelagte links.
3. Færdiggør private runtime-/værktøjspakker og offline-licensmanifest. Brug distributions-ADR'ens osx-arm64, win-x64 og linux-x64, Native AOT og private biblioteker. Signering følger det eksisterende releaseflow; der publiceres ikke en udgivelse som del af planens kodeopgave.
4. Afprøv pakkede apps på rene målmiljøer uden værktøjer fra PATH og med netværk afbrudt. Kontrollér graf, eksport af alle formater, systemfremviser, skrifter og midlertidig oprydning.
5. Kør hele .NET-testpakken og Python-testpakken. Gennemfør manuelle regressioner af opstart, import, afviste værdier, skabelonredigering, navigation, dirty buffers og grafeksport.
6. Mål grafens performance igen på endelige pakker, og registrér eksporttid/hukommelse ved 1/10/100 biografier. 120 sekunders timeout pr. dokument/konvertering må ikke overskrides i standardfixtures uden synlig fejl.
7. Udfyld sprintjournaler og opdatér roadmap med faktisk status, kendte afvigelser og ejerens accept. Kun dokumenterede grønne kontroller kan markeres bestået; ellers står trin 12 fortsat uafsluttet.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
