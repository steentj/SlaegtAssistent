# Plan: Lokalt layout med Graphviz

Status: planlagt. Denne feature er en afgrænset kodeopgave; implementér ikke efterfølgende features samtidig.

## Forudsætninger

Feature 01 og Grafkontrakt.md.

Kravsporbarhed: **G-04, G-12**. Se [krav](Requirements.md), [validering](Validation.md) og [sprintplan](../Plan.md).

## Kodeopgaver i rækkefølge

1. Opret `GraphDotWriter` og `GraphPlainParser` i Core samt de UI-uafhængige koordinattyper. Brug stabile interne node-id'er og én opbevaring af mappingen.
2. Skriv `GraphDotWriterTests` og `GraphPlainParserTests` før parserkode: dansk input, invariant kultur, citerede tokens, manglende stop/node, ukendt id, NaN/Infinity, skala forskellig fra 1 og negativ størrelse.
3. Implementér `ILocalProcessRunner` og `LocalToolLocator` i App. Afgræns programsti/argumenter, outputstørrelse, timeout og processetræ-stop som kontrakten kræver. Brug fakes i almindelige tests.
4. Implementér `GraphvizLayoutService`: unik jobmappe, sorteret DOT, `sfdp -Tplain`, validering, koordinatkonvertering og oprydning. Ingen Python-afhængighed i grafvisningen.
5. Tilføj cache efter topologi/parametre/værktøjsversion. Coalescér samtidige identiske forespørgsler eller annullér den gamle; publicér kun den nyeste revision. Tom graf kræver ingen proces.
6. Skriv `GraphvizLayoutServiceTests` for timeout, værktøj mangler, ikke-0 exit, outputgrænse, cache og A-svar efter B-svar. UI-publicering sker gennem koordinatoren i feature 04.
7. Kør en særskilt rigtig Graphviz-smoketest på de to fixturestørrelser og fastlås en fungerende værktøjsbuild. Bitidentiske koordinater kræves kun ved samme værktøjsversion/indstillinger, ikke på tværs af alle platforme.

## Aflevering

Kør målrettede tests og derefter berørte projekters samlede tests. Brug xUnit og FluentAssertions i .NET. Tilføj nødvendige konstruktør-/servicefakes til eksisterende tests, og kontrollér at de fortsat verificerer reel adfærd. Alle nye UI-tekster, kommentarer og dokumenter er på dansk; identifikatorer er på engelsk. Registrér berørte filer, testkommandoer, faktisk resultat og uafprøvede forhold i valideringsjournalen. En bestået mocktest må ikke beskrives som en bestået native værktøjs-/platformstest.
