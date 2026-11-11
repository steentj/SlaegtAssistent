# Krav: Tegning og interaktion

Dækker **G-05, G-06, G-07, G-08, G-12** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- Alle farver, størrelser, zoomgrænser og labels følger Grafkontrakt.md; ukendt køn er eksplicit.
- Hover og fokus giver nøjagtig samme informationsmængde; manglende datoer udelades.
- Direkte naboer får labels samtidig, og den komplette oversigt forbliver anvendelig ved høj grad.
- Kontrollen har ingen dokument-/procesansvar og ingen UI-typer lækker til ViewModel.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
