# Validation: Sprint 6 – brugervenlig skabelonredigering

## Automatiske kvalitetsporte

- [ ] Alle fem features har `Requirements.md`, `Plan.md` og `Validation.md`.
- [ ] Hver feature har dokumenteret en rød-grøn-refaktorér-cyklus.
- [ ] Core-tests dækker editormodel, generator, versionsmetadata, serialisering, genåbning og deterministisk output.
- [ ] App-tests dækker blokredigering, previewvalg, annullering, gemning, fejl og genåbning.
- [ ] Integrationstests bekræfter atomisk lagring og tydelig overgangsbesked ved det tidligere format.
- [ ] Regressionstests dækker importpreflight, personlinks, kandidatgodkendelse, genrendering og fri tekst.
- [ ] Headless UI-tests dækker tastaturbetjening, fokus, danske labels og minimumsstørrelse.
- [ ] `dotnet build` består uden warnings.
- [ ] `dotnet test` består samlet.

## Samlet manuel validering

- [ ] Åbn editoren uden GEDCOM og bekræft, at et komplet eksempelpreview vises.
- [ ] Flyt “Kilder” før “Hændelser”, skjul “Medier” og skift hændelser til tabel.
- [ ] Gem, luk og genåbn; bekræft samme blokke, rækkefølge, valg og preview.
- [ ] Vælg en faktisk person og sammenlign preview med en efterfølgende produktionsrendering.
- [ ] Tryk **Annuller** efter ændringer og bekræft, at fil og aktiv indstilling er byte-for-byte uændret.
- [ ] Start med en indstilling, der peger på det tidligere format, og bekræft den tydelige overgangsbesked og muligheden for at oprette en ny standardskabelon.
- [ ] Simulér en skrivefejl og bekræft, at den tidligere aktive skabelon fortsat virker.
- [ ] Nulstil til standard, kontrollér preview, annuller og gentag derefter med gemning.
- [ ] Godkend en skabelonændring gennem kandidatflowet og bekræft, at fri tekst er uændret.
- [ ] Gennemfør hele hovedforløbet med tastatur ved 200 % skalering.
- [ ] Kontrollér, at preview ikke foretager netværkskald.

## Brugerbarhedstest

| Deltager | Uerfaren målgruppe bekræftet | Tid | Uden hjælp | Observationer |
| --- | --- | ---: | --- | --- |
| 1 |  |  |  |  |
| 2 |  |  |  |  |
| 3 |  |  |  |  |

- [ ] Mindst 80 % gennemfør hovedopgaven på højst fem minutter.
- [ ] Ingen kritiske eller alvorlige brugerbarhedsproblemer står åbne.
- [ ] Fund og efterfølgende rettelser er dokumenteret.

## Endelig godkendelse

- **Dato:**
- **Godkendt af:**
- **Testede platforme:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Sprint 6 er manuelt godkendt, og roadmap-trin 12 må påbegyndes.
