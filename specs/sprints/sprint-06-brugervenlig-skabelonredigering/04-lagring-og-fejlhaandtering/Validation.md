# Validation: Lagring og fejlhåndtering

- [x] **Annuller** efter redigering efterlader alle vedvarende data uændrede.
- [x] **Gem** aktiverer kun en fil, som kan genindlæses og valideres.
- [x] Skrivefejl og afbrudt skrivning bevarer tidligere fil og aktiv sti.
- [x] En skabelon i det nye format genåbnes med identisk editormodel.
- [x] En sti til det tidligere format giver en tydelig overgangsbesked og mulighed for at oprette en ny standardskabelon.
- [x] En skabelonændring vises som kandidat; eksisterende fri tekst er uændret før og efter godkendelse.
- [x] Integrations- og regressionstests består.
- [ ] Manuel fejlindsprøjtning er godkendt.

**Godkendelsesdato:** 8. november 2026 (godkendelse til at fortsætte)

**Godkendt af:** Brugeren i samtalen: »Godkendt, fortsæt«
**Bemærkninger:**

- Automatisk validering den 8. november 2026. Afkrydsningerne ovenfor dokumenterer automatiske tests, ikke manuel godkendelse.
- Rød kørsel: de første lagringstests kunne ikke kompileres, fordi `TemplateEditorStorageService` endnu ikke fandtes.
- Grøn kørsel: `dotnet test --no-restore --verbosity minimal` bestod med 129 Core-tests og 190 App-tests, i alt 319 tests.
- `dotnet build --no-restore --verbosity minimal` bestod med 0 advarsler og 0 fejl.
- Fejlindsprøjtning dækker tre afbrydelsespunkter i den eksisterende atomiske writer, manglende skriveadgang, ugyldig genindlæsning og en gyldig, men ændret model efter skrivning. Den tidligere destinationsfil bevares; nye, ugyldige destinationer aktiveres ikke.
- Tests dækker ny fil, overskrivning, **Gem som**, afbrudt filvalg, genåbning af kladde og gemt model, annullering og alle indlæste personer som previewvalg.
- Integrationsregressionen gennemfører import med en ændret, gemt editormodel både ved godkendelse og afvisning; fri biografitekst bevares i begge tilfælde.
- Brugeren har godkendt den leverede funktionalitet og opfølgningen som grundlag for at fortsætte til trin 5. Godkendelsen er ikke en registrering af særskilt gennemført manuel fejlindsprøjtning eller native filvælgertest; de konkrete manuelle kontrolpunkter bevares åbne, hvor testresultater ikke er oplyst.

## Manuel kontrol til godkendelse

1. Åbn Indstillinger uden GEDCOM, vælg **Rediger skabelon…**, ændr en blok og kontrollér eksempelpreviewet.
2. Vælg **Brug kladde** og derefter **Annuller** i Indstillinger. Kontrollér, at den oprindelige skabelon og den aktive sti er uændrede.
3. Gentag, vælg **Gem som…**, gem som `.slaegtskabelon`, og genåbn editoren. Kontrollér blokke, rækkefølge og indstillinger.
4. Vælg en tidligere tekstskabelon. Kontrollér overgangsbeskeden og oprettelsen af en ny standardskabelon uden automatisk overskrivning af den gamle fil.
5. Forsøg at gemme i en mappe uden skriveadgang. Kontrollér fejlbeskeden, at dialogen forbliver åben, og at den tidligere fil og aktive sti stadig virker.
6. Vælg **Anvend skabelon** efter en gemt skabelonændring. Kontrollér kandidatgennemgangen og bevaring af fri biografitekst ved både afvisning og godkendelse, også efter genstart uden adgang til GEDCOM-kilden.

## Opfølgning på afprøvning – 8. november 2026

- [x] Overskriftindtastning bevarer valgt blok og feltinstanser uden genopbygning af bloklisten. En headless-test skriver ti tegn i det rigtige editorvindue og kontrollerer fokus og tekstposition efter hvert tegn.
- [x] GEDCOM-filvælgeren har et eksplicit dansk alle-filer-filter med macOS-typeidentifikatoren `public.item`. Den overlader ikke længere filtervalget til en tidligere native filvælger.
- [x] En integrationstest opretter en skabelon før første import, gemmer indstillinger og importerer derefter GEDCOM uden at genstarte ViewModelen.
- [x] **Anvend skabelon** fungerer efter genstart, selv når den oprindelige GEDCOM-fil er slettet. Godkendelse, afvisning og lukning uden ændringer er testet.
- [x] Gentagen anvendelse af samme skabelon er en no-op uden ekstra blanklinjer.
- [x] Fri biografitekst, dokumentfakta og GEDCOM-baseline bevares; ugemte editorer skrives ikke til disk.
- [x] Annullering, samtidige editorændringer under gennemgangen og tilbagerulning efter en fejl på anden filskrivning er regressionstestet.
- [x] Samlet regression: 132 Core-tests og 201 App-tests, i alt 333 tests. Build: 0 advarsler og 0 fejl.
- [ ] Den native macOS-filvælger er manuelt kontrolleret efter **Gem som…** og før første GEDCOM-import i samme session.
- [ ] Overskriftredigering og **Anvend skabelon** er manuelt godkendt i den kørende app.

Rød kørsel reproducerede både udskiftning af den valgte blok ved overskriftindtastning og det manglende eksplicitte filfilter. De efterfølgende grønne tests omfatter det rigtige Avalonia-fokusflow, men headless-tests erstatter ikke kontrollen af macOS' native filpanel.
