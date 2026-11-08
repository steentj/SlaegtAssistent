# Brugertest: Guidet skabelonredigering

## Status og deltagere

Klar til lokal gennemførelse. Ingen deltagere er endnu registreret som inviteret eller testet. Den ansvarlige bruger inviterer mindst tre personer, som ikke arbejder professionelt med kode eller skabelonsprog. Gem kun deltagerkode, målgruppebekræftelse, tidsforbrug og observationer; ingen navne, skærmoptagelser eller slægtsdata er nødvendige.

## Ens forberedelse

1. Brug samme programversion og samme skærmindstilling for alle deltagere. Notér operativsystem, skalering og vinduesstørrelse i resultatarket.
2. Brug en separat, tom lokal testmappe til skabelonfiler, ikke mapper med virkelige persondokumenter. Indlæs ikke GEDCOM.
3. Åbn Indstillinger og nulstil den globale skabelon til standarden. Bekræft standardudgangspunktet før hver deltager. Lad Indstillinger stå åbent.
4. Alle bruger editorens indbyggede, deterministiske **Eksempelperson** fra `BiographyTemplateExampleData`: Anna Marie Jensen, familie, hændelser, folketællinger, kilder og lokale mediepladsholdere. Ingen netværkskilder eller ekstra datasæt skal hentes.
5. Giv deltageren opgaven nedenfor. Start tiden, når opgaven er læst, og stop, når den genåbnede skabelon er kontrolleret. Forklar ikke knapper eller fremgangsmåde undervejs.

## Opgaveark til deltageren

Tilpas skabelonen til persondokumenterne:

1. Åbn skabeloneditoren fra Indstillinger.
2. Flyt **Kilder** før **Hændelser**.
3. Skjul **Medier** i dokumentet.
4. Vis **Hændelser** som tabel.
5. Kontrollér resultatet i forhåndsvisningen med eksempelpersonen.
6. Gem skabelonen i den lokale testmappe. Luk og genåbn editoren, og kontrollér, at ændringerne er bevaret.

Du må gerne bruge programmets hjælp. Sig til, hvis du går i stå. Testen handler om programmet, ikke om dine evner.

## Registrering og bedømmelse

Notér invitation og faktisk gennemførelse hver for sig i sprintets samlede `Validation.md`. Målgruppen skal være bekræftet. Programhjælp er tilladt; vejledning fra testlederen registreres som hjælp. En gennemførelse tæller kun som succes, hvis alle seks delopgaver er løst og bevaret efter genåbning, på højst 300 sekunder og uden hjælp fra testlederen.

Succesandel = antal succeser / antal gennemførte forsøg × 100. Afbrudte forsøg tæller som ikke gennemført, ikke som succes. Ved præcis tre deltagere kræver målet på mindst 80 % derfor **tre af tre succeser**; to af tre er kun 67 %.

| Deltagerkode | Inviteret | Målgruppe bekræftet | Tid i sekunder | Alle opgaver løst | Hjælp fra testleder | Resultat og observation |
| --- | --- | --- | ---: | --- | --- | --- |
| 1 | Afventer | | | | | |
| 2 | Afventer | | | | | |
| 3 | Afventer | | | | | |

## Supplerende manuel tilgængelighedskontrol

- Gentag opgaven uden mus: Tab/Skift+Tab, piletaster, Mellemrum, Alt+Pil og Ctrl+Alt+Pil. Kontrollér synlig fokusmarkering og gentagen flytning af det samme felt.
- Kontrollér lyst og mørkt tema, herunder fejltekst, deaktiverede knapper, valgt sektion og fokus uden at støtte dig til farve alene.
- Gentag ved minimumsvindue og 200 % systemskalering. Kontrollér læsbarhed, rulning og synlige handlinger i editor og Indstillinger.
- Kontrollér danske oplæste navne med platformens skærmlæser og navigation ind i og ud af det native preview.
- På macOS: Opret først en skabelon med Gem som…, og vælg derefter en GEDCOM-fil i samme programsession uden genstart.
- Gentag native kontrol på de øvrige understøttede platforme før endelig godkendelse; headless-tests erstatter ikke skærmlæser, native filpanel eller visuel kontrol.

Registrér kritiske/alvorlige fund med konkrete trin, forventet og faktisk resultat. Ret dem med regressionstest, og gentag den berørte opgave før endelig sprintgodkendelse.
