# Validation: Feltstyret konfliktløsning og kandidatgodkendelse

- [x] Skalare felter kan vælges uafhængigt.
- [x] Hændelser, kilder og medier viser tilføj, ændr og fjern korrekt.
- [x] Massevalg kan fortrydes før godkendelse.
- [x] GEDCOM-, skabelon- og migreringsårsag er tydelig.
- [x] Preview afspejler præcis de valgte feltbeslutninger.
- [x] Fri tekst uden for markørerne bevares byte-for-byte.
- [x] Afvisning og lukning ændrer intet vedvarende eller i en åben editor.
- [x] Godkendt åben editor markeres som ugemt.
- [x] Migrering er en særskilt, tydelig kandidat.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel blandet konfliktløsning er gennemført.

## Automatiseret resultat

- **Dato:** 2026-08-13
- **Målrettet Core-suite:** `BiographyConflictResolutionTests`, 6 tests bestået for skalare trevejsforskelle, stabile samlingshandlinger, individuelle beslutninger, årsager, preview og bytebevarelse.
- **Målrettet App-forløb:** 2 tests bestået for reversibelt massevalg og for blandet accept/afvisning, som bevarer en brugerændret dokumentværdi, viser samlingsfelter, opdaterer baseline korrekt og markerer en åben editor som ugemt.
- **Afvisning og migrering:** eksisterende regressionsforløb for lukning uden anvendelse, manglende baseline og ukendt baselineversion er bestået.
- **Core-suite:** 98 tests bestået.
- **App-suite:** 99 tests bestået.
- **Samlet:** 197 tests bestået, 0 fejlet.
- **Build:** bestået med 0 advarsler og 0 fejl.

## Foreslået manuel prøve

1. Importér en GEDCOM med fødsel, flere hændelser, mindst to kilder og to medier, og åbn personens Markdown-dokument.
2. Ret fødselsdatoen manuelt i dokumentets fakta og tilføj fri tekst efter den genererede slutmarkør. Gem ikke editoren.
3. Importér en kopi, hvor fødestedet ændres, én hændelse ændres, én kilde tilføjes, og ét medie fjernes.
4. Kontrollér stabil feltsti, handlingen **Tilføj**, **Ændr** eller **Fjern**, årsagen **GEDCOM** og de tre kolonner **Dokument**, **Godkendt** og **Ny GEDCOM**.
5. Vælg **Bevar alle dokumentværdier**, vælg derefter alle nye GEDCOM-værdier, og fortryd enkelte valg. Kontrollér, at previewet følger hvert valg uden at ændre editoren.
6. Bevar den manuelt ændrede fødselsdato, acceptér det nye fødested og kilden, men afvis hændelsen og mediefjernelsen. Tryk **Anvend valgte**.
7. Kontrollér, at previewresultatet anvendes, den frie tekst er byte-identisk, og editoren er markeret som ugemt.
8. Luk uden at gemme eller gentag i en kopi med et lukket dokument. Kontrollér, at en afvisning ikke ændrer dokument, baseline eller GEDCOM-snapshot.
9. Godkend kandidaten for det lukkede dokument, og kontrollér, at filen først ændres under importens samlede commit.
10. Gentag med et dokument uden markører og et dokument uden baseline. Kontrollér den særskilte handling **Migrér**, og afvis den for at verificere, at intet ændres.

## Manuel godkendelse

- **Dato:**
- **Godkendt af:**
- **Build eller commit:**
- **Bemærkninger:**

- [ ] Feature 08 er godkendt, og feature 09 må påbegyndes.
