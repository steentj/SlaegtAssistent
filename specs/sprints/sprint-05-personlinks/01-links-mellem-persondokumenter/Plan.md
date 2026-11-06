# Plan: Sprint 5 – links mellem persondokumenter

## Formål og placering
Roadmap-trin 10 gennemføres før grafisk slægtstræ og eksport. Når en genereret biografi omtaler en identificeret person fra GEDCOM, skal navnet være et relativt Markdown-link til personens dokument. Eksempel: `Forældre: [Anna Jensen](anna-jensen-ab12.md)`; det konkrete filnavn findes via dokumentidentiteten, aldrig ved at gætte ud fra navnet.

## Nuværende arkitektur
- `BiographyTemplateMarkdownGenerator` renderer standard- og brugerskabeloner samt godkendte kanoniske snapshots.
- `BiographyTemplateContext` og `BiographyTemplateRenderer` eksponerer person- og familiedata. Forældre findes allerede som strukturerede referencer; børn og partnere skal undersøges og eksponeres tilsvarende.
- `BiographyFileNameGenerator` giver navne til nye dokumenter. Eksisterende dokumenters faktiske stier kommer fra arbejdsområdets katalog.
- `MarkdownBiographyExportService` håndterer generering; `MainWindowViewModel` håndterer import, stabile stier, snapshots og kandidatgodkendelse.
- `BiographyGeneratedSectionMerger` beskytter den frie tekst. `SafeMarkdownPreviewService` håndhæver lokal, privat forhåndsvisning.

## 1. Fastlæg identitet og dokumentmål
1. Kortlæg alle produktionsveje til generering, herunder samlet eksport, ny import, eksisterende dokumenter, genrendering og skabelonforhåndsvisning.
2. Indfør en rendereruafhængig Core-kontrakt for opslag fra record-id til dokumentmål. App-laget leverer katalogoplysninger; Core må ikke kende Avalonia.
3. Byg ét konsistent opslag før en importbatch renderes. Eksisterende entydige dokumentstier vinder over nye beregnede filnavne. Nye personer får samme planlagte sti som den efterfølgende lagring anvender.
4. Manglende mål, tvetydige identiteter og personer uden dokument i batchen skal give almindelig tekst. Navnesammenfald må aldrig forbinde forskellige record-id'er.
5. Beregn relative stier fra kildedokumentets mappe. Brug `/` og korrekt URI-kodning af hvert stisegment; escape Markdown i linkteksten. Bevar danske bogstaver og undgå absolutte maskinspecifikke stier.

## 2. Strukturerede personomtaler og skabelonkontrakt
1. Udvid referencekonteksten med et sikkert linkfelt og eventuelt dokumentsti. Bevar et rent navnefelt til overskrifter og eksisterende skabelonlogik.
2. Understøt forældre, børn og partnere samt øvrige eksisterende strukturerede personreferencer. Deduplicér gentagne relationer deterministisk efter record-id.
3. Opdatér standardskabelonen, så alle de relationer den viser bruger linkrendering. Tilføj børn og partnere som betingede afsnit, så eksemplerne i brugerkravet dækkes direkte.
4. Sørg for, at understøttede personnavnefelter i egne skabeloner får dokumenterede linkmuligheder uden dobbelt escaping eller links inde i links. Opdatér loaderens feltvalidering og skabelonhjælpen.
5. Linkopslag er identitetsbaseret: navne i vilkårlige noter og brugerens frie prosa kan ikke sikkert identificeres ved navnesøgning og omskrives derfor ikke automatisk. Dette er en eksplicit dataintegritetsgrænse, ikke en ændring af brugerens tekst. Identificerede omtaler i genererede relationer dækkes fuldt.

## 3. Integrér import, eksport og synkronisering
1. Send det samme målregister gennem ny generering og snapshotbaseret genrendering. Brug godkendte relationer fra snapshot; afviste GEDCOM-relationer må ikke sive ind i dokumentet.
2. Gør standardskabelonændringen synlig som en kandidatopdatering i eksisterende dokumenter. Undersøg også egne skabeloner og ændrede målregistre, så allerede importerede personer kan få links uden at skulle ændre GEDCOM-data kunstigt.
3. Bevar eksisterende filnavne ved personnavneændringer. Genåbning skal anvende katalog og persistent snapshot frem for at kræve ny GEDCOM-import.
4. Opdater kun den markerede genererede sektion ved godkendelse. Afvisning, fri tekst og ugemte editorændringer skal følge det eksisterende beskyttelsesflow.
5. Bevar previewets sikkerhedspolitik. Markdown-filerne skal indeholde almindelige portable relative links; der må ikke åbnes vilkårlige lokale filer eller netværksressourcer automatisk. Intern navigation er kun relevant, hvis den eksisterende previewkontrakt understøtter den sikkert.

## 4. Implementering og arbejdsdeling
En implementerende subagent får ansvaret for Core, App, relevante tests og opdateret validering ud fra denne specifikation. Hovedagenten vedligeholder roadmap og specifikation, gennemgår ændringerne og kører samlet build/test. Agenten skal rapportere konkrete begrænsninger og må ikke betegne manuel GUI-afprøvning som udført uden faktisk afprøvning.

## 5. Test og afslutning
Skriv målrettede xUnit-tests med FluentAssertions for identitetsopslag, stier, relationer, skabeloner og kandidatflow. Kør relevante tests under udvikling og afslut med `dotnet build` og `dotnet test`. Registrér kommandoer, resultater og eventuelle udestående manuelle kontroller i Validation.md. Funktionaliteten er implementeret, når automatiske krav er dækket; manuel accept markeres særskilt.

## Implementeret løsning
- `BiographyPersonLinks` holder Core-opslaget og URI-koder relative dokumentstier.
- `BiographyPersonLinkFactory` forbinder App-katalogets faktiske stier med nye, planlagte dokumenter og udelader tvetydige eller optagede mål.
- Batchmetoden `GenerateBiographies` bygger ét målregister pr. batch og bevarer annullering mellem personer. Kandidatflowet genbruger sit målregister.
- Skabeloner får `person.children`, `person.partners`, `person.childNames` og `person.partnerNames`. Relationsfelterne `fullName` og `link` producerer sikre links; `plainName` giver et almindeligt navn, og `documentPath` giver den relative URI. Hovedpersonens overskrift er almindelig tekst.
- Standardskabelonen viser forældre, børn og partnere, når relationerne findes. Skabelonhjælpen beskriver felterne og previewets begrænsning.
- Implementeringen blev delegeret til en subagent. Efter agentens forbrugsgrænse overtog hovedagenten afsluttende gennemgang, integrationstest og samlet validering.
