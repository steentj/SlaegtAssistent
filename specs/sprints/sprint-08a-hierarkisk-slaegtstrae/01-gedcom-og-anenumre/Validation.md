# Validering af GEDCOM-grundlag og anenumre

Status: Implementeret med automatiske prøver. Den fulde valideringsmatrix nedenfor er ikke gennemført.

19. november 2026: Core-testpakken bestod med 264 tests. Nye prøver dækker GEDCOM-navne og genindlæsning, biologisk prioritering, rollebestemt adoption, anesammenfald, nummeropslag over 64 bit og cyklusstop. De resterende kombinationer i tabellen skal fortsat prøves.

| Prøve | Forventet resultat |
| --- | --- |
| Proband og fire bedsteforældre | 1, 2–3 og 4–7 i korrekt rækkefølge, også med ombyttet rækkefølge af GEDCOM-poster eller manglende `SEX`. |
| Kun mor er kendt | Mor er 3, far er plads 2 med »Ukendt«. Når begge kandidater mangler, slutter grenen. |
| Flere familier, adoption og fælles entydig plads | Biologisk forrang, korrekt rolle ved rollebestemt adoption, ingen opfundne forældrepladser og tydelig diagnostik for modstridende familier. Barnelisten bevarer dokumenterede ikke-biologiske links og deres type. |
| Anesammenfald, dobbelt barnelink og cyklus | To anenumre kan give samme id. Dublet på samme barnelink fjernes. Cyklisk gren stopper, mens en anden gyldig sti fortsætter. |
| Dybde over 64 led | Et kendt nummer over `UInt64.MaxValue` kan slås op. Nul, minus, bogstaver, ukendte pladser og cirkulære stier giver ingen person. |
| Fødenavn, giftenavn, ændret fornavn og alias | Forventet fuldt navn med parentes, uden dubletter eller alias i parentesen. Kun giftenavn og helt manglende navn prøves særskilt. |
| Datoer og steder | Tomme felter giver »Ukendt«. Årstal, fuld dato, omtrentlig dato, før/efter og interval bevarer kildepræcisionen efter hvert visningsformats regler. |
| Eksisterende dokumenter og genindlæsning | Åbn et gammelt snapshot, genindlæs med ændrede navne og familielinks og gentag importen. Ingen forældede navne eller links i træet og ingen ny forskel i gamle testcases alene som følge af visningsreglerne. |

Brug xUnit og FluentAssertions. Kør Core-testprojektet og import- og snapshotregressionerne i App-testprojektet.
