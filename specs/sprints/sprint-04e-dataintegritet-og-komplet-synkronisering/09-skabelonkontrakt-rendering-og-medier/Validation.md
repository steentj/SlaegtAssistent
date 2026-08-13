# Validation: Valideret skabelonkontrakt, rendering og medier

- [x] Ukendte felter og forkert kontekst afvises med fil, linje og kolonne.
- [x] Indstillinger kan ikke gemme en ugyldig eller manglende skabelon som aktiv.
- [x] Preview og import bruger samme validerings- og renderingssti.
- [x] To eller flere forældre formateres læsbart.
- [x] Hændelseskategorier vises på dansk.
- [x] Samme input giver byte-identisk genereret output.
- [x] Relative medier virker, når GEDCOM- og outputmappe er forskellige.
- [x] Manglende medier giver synlig advarsel uden at blokere øvrigt output.
- [x] Cheat sheet og feltkontrakt stemmer overens.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel skabelon- og medietest er gennemført.

## Automatiseret resultat

- **Dato:** 2026-08-13
- **Målrettet Core-suite:** `BiographyTemplateContractTests`, 8 tests bestået for AST-kontrakt, placering, kontekst, alle offentlige felter, dansk referenceoutput, determinisme, fælles preview/rendering og mediepolitik.
- **Målrettede App-forløb:** indstillingsgemning af manglende/ugyldig skabelon, ugyldig importskabelon før commit, separat GEDCOM-/outputmappe og synlig advarsel for manglende medie er bestået.
- **Cheat sheet:** automatisk test verificerer kontraktversion og alle normative feltstier.
- **Core-suite:** 106 tests bestået.
- **App-suite:** 104 tests bestået.
- **Samlet:** 210 tests bestået, 0 fejlet.
- **Build:** bestået med 0 advarsler og 0 fejl.

## Foreslået manuel prøve

1. Opret en global skabelon med `{{ person.ukendt }}` på linje 3, vælg den i indstillinger og tryk **Gem**. Kontrollér dansk fejl med fil, linje 3 og kolonne samt at dialogen ikke lukker.
2. Ret feltet, men skriv `{{#each events}}{{ title }}{{/each}}`. Kontrollér, at `title` afvises som ugyldigt i en hændelsesløkke.
3. Brug en gyldig skabelon med `person.parentNames`, `allEvents`, `sources`, `media` og `submitter`. Kontrollér, at preview og efterfølgende import viser samme genererede tekst.
4. Importér en person med mindst to forældre samt fødsel, dåb, vielse, død og en ukendt hændelse. Kontrollér separatorer og danske kategorinavne.
5. Gentag samme import og kontrollér byte-identisk genereret sektion og status **Færdig – ingen ændringer**.
6. Placér GEDCOM i mappen `gedcom`, et foto i `gedcom/medier`, og Markdown-output i en separat mappe `markdown`. Brug en relativ `FILE medier/foto med mellemrum.jpg`.
7. Kontrollér, at Markdown-linket er relativt fra outputmappen, bruger `/`, og URL-koder mellemrummet som `%20`.
8. Tilføj en reference til en manglende fil. Kontrollér en synlig advarsel, fortsat rendering af øvrigt indhold og intet dødt link i den genererede sektion.
9. Gør en eksisterende fil ulæselig og gentag. Kontrollér samme ikke-blokerende advarsel.
10. Brug en absolut fil uden for GEDCOM- og outputmapperne. Kontrollér tydelig fejl og manuelt valg før importen må fortsætte.
11. Afvis gennemgangen, og kontrollér, at dokumenter, editorer, baseline og GEDCOM-snapshot er uændrede.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Feature 09 er godkendt, og feature 10 må påbegyndes.
