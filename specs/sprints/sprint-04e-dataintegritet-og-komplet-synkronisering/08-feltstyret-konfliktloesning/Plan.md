# Plan: Feltstyret konfliktløsning og kandidatgodkendelse

1. Skriv fejlende Core-tests for feltvalg, samlingsforskelle, årsager, afvisning og bytebevarelse.
2. Skriv fejlende ViewModel- og rendererløse UI-tests for individuelle valg, massevalg, preview og annullering.
3. Definér en typed difference- og beslutningsmodel oven på feature 07-baselinen.
4. Implementér deterministisk diff for skalare og gentagne data.
5. Implementér dialog eller arbejdsflade med feltsti, værdier, årsag og tilgængelig keyboardbetjening.
6. Generér previewkandidaten fra brugerens valg uden sideeffekter.
7. Integrér godkendelsen med importgennemførelsen fra feature 04 og lagringen fra feature 01.
8. Implementér særskilt migreringsflow for manglende markører eller baseline.
9. Kør målrettede tests, hele testpakken og build.
10. Gennemfør manuel blandet accept og afvisning på både skalare og gentagne data.
11. Dokumentér resultatet, og stop før feature 09.
