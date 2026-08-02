# Requirements: Sprint 04B – skabelonbaserede persondokumenter

- Persondokumenter skal kunne genereres ud fra en global Markdown-skabelon.
- Skabelonen skal kunne vælge GEDCOM-felter og formatere dem som tekst, lister eller tabeller.
- Hændelser skal omfatte person- og familiebegivenheder, herunder vielser.
- `EVEN` skal klassificeres ud fra tag og `TYPE`; ukendte typer skal bevares under andre hændelser.
- Kilder skal kunne vises inline og i en samlet kildeliste.
- Medier skal kunne gengives som relative Markdown-links.
- `SUBM` fra GEDCOM-headeren skal kunne vises gennem skabelonen.
- Skabeloner må ikke udføre vilkårlig programkode.
- Fri Markdown- og AI-tekst må ikke overskrives automatisk.
- Skabelon- eller GEDCOM-ændringer skal kunne gennemgås som diff.
