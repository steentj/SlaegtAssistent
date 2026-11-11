# Validering: Lokalt layout med Graphviz

Status: **ikke kørt**. Scenarierne er planlagte og er ikke testresultater. Fælles fixtures og afsluttende scenarier findes i [sprintets validering](../Validation.md).

- Automatisk: kendt plain-input giver præcis forventet x/y efter tommer-til-DIP og y-invertering.
- Automatisk: argument med mellemrum er ét argument; stderr og stdout læses uden deadlock; for stort output stopper processen.
- Integration: kør rigtig sfdp med alle komponenter, én node og stor fixture; kontrollér præcis node-id-mængde.
- Måling: registrér tre Release-kørsler, værktøjsversion og maskine mod Grafkontraktens 5/30-sekundersmål.

## Journal ved implementering

| Dato | Krav/scenarie | Test/kommando og miljø | Faktisk resultat | Evidens/åben afvigelse |
| --- | --- | --- | --- | --- |
| Ikke kørt | G-04, G-12 | Ikke registreret | Afventer implementering | Ingen accept endnu |
| 11. november 2026 | Del af G-04 | `dotnet test` på udviklingsmaskinen | 141 Core-tests og 210 App-tests bestået | DOT-generator og plain-parser har automatiske tests. `sfdp` blev ikke fundet i PATH, så en faktisk Graphviz-kørsel er ikke afprøvet. Procesrunner, integration og platformspakker mangler. |
| 11. november 2026 | Del af G-04 | Målrettede `LocalProcessRunnerTests` og `GraphvizLayoutServiceTests` | 5 tests bestået på macOS | Procesargumenter, timeout og outputgrænse er testet med lokale programmer; Graphviz-service er testet med falsk proces. Rigtig Graphviz, cache, integration og platformspakker mangler. |
| 11. november 2026 | Del af G-04 | Graphviz 16.1.1 bygget lokalt fra kildekode, `sfdp -Tplain` | Ikke bestået | Builden mangler GTS og rapporterer trianguleringsfejl på stderr trods exitkode 0. Servicen afviser nu denne fejl. En korrekt privat build og platformspakker mangler. |
| 11. november 2026 | Del af G-04 | Graphviz 16.1.1 med GTS 0.7.6, macOS arm64; midlertidig integrationstest af `GraphvizLayoutService` | Bestået for tre noder og to kanter | Den lokale udviklingsbuild virker med `overlap=prism`. Den afhænger af midlertidige stier og er ikke en distribuerbar platformspakke. |
