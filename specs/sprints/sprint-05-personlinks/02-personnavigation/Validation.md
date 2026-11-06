# Validering: Personnavigation

## Automatiske scenarier
- Relative links med URI-kodede danske navne opløses til rette katalogperson.
- Kun godkendte links omskrives til intern navigation; ↗ findes alene i preview/UI.
- Eksterne, absolutte, manglende og tvetydige mål åbnes ikke.
- Klik vælger korrekt person; kilde og mål beholder ugemte editorer ved skift.
- Personfilter forhindrer ikke navigation. Forældet mål valideres på ny ved klik.

## Manuel kontrol
Klik på personnavn/↗ i native preview på macOS og Windows, brug Tab/Enter på personknapper, skift frem og tilbage med ugemt tekst, og kontrollér at ingen markører gemmes i filerne.

## Resultat
## Automatiske resultater
- Samlet build: 0 advarsler, 0 fejl.
- Samlet testsuite: 272 bestået (120 Core, 152 App), ingen fejl eller oversprungne tests.
- De 12 målrettede navigationstests omfatter URI-kodede navne, sikre mål, tvetydige identiteter, slettede mål, previewrendering og editorens kommandoer.
- Headless Avalonia-test verificerer, at personknappen renderes med korrekt navn/↗, kan få fokus og aktiverer den rigtige kommando uden at markere dokumentet som ændret.
- Integrationstest verificerer skift begge veje, bevaret ugemt tekst i begge editorer, navigation gennem aktivt personfilter, afvisning af ukendt intern adresse og slettet mål samt gemning uden UI-markører/interne adresser.

Kommandoer, kørt fra `/private/tmp` med SDK 10.0.403:
```sh
dotnet build /Users/steen/Projects/SlaegtAssistent/SlaegtsAssistent.sln --no-restore
dotnet test /Users/steen/Projects/SlaegtAssistent/SlaegtsAssistent.sln --no-build --no-restore
```
Projektets låste SDK 10.0.402 er ikke installeret; `global.json` er uændret.

## Manuel status og begrænsninger
Den native WebViews faktiske klikforløb på macOS og Windows er ikke manuelt afprøvet. Headless-testen udelader native WebView, men kontrollerer de faktiske Avalonia-knapper og bindinger. Platformkontrollen ovenfor udestår.

Personnavigation bruger den eksisterende arbejdsflade og editorcache, ikke ekstra vinduer. Brugeren kan vende tilbage via personlisten eller et personlink i det nye dokument. Kun dokumentmål fra det aktuelle personkatalog åbnes; eksterne links og øvrige lokale filer får ingen udvidet adgang.
