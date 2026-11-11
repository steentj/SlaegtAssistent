# Krav: Lokalt layout med Graphviz

Dækker **G-04, G-12** fra [sprintets krav](../Requirements.md). Detaljer i sprintets kontraktdokument gælder også.

- Layoutresultat indeholder præcis grafens node-id'er og endelige koordinater i dokumenteret verdenssystem.
- Koordinater kan genbruges ved skift af navn, dato eller proband.
- Timeout/annullering stopper underprocesser og efterlader ikke aktivt jobarbejde.
- Værktøjsfejl ændrer ikke sidste gyldige graf eller dokumenter.

Featureen er færdig, når kravene har test-/afprøvningsevidens, fejlsituationerne er kontrolleret og eksisterende relevant adfærd fortsat fungerer. Der indføres ingen netværkskald, telemetri eller automatisk overskrivning af brugerens persondokumenter.
