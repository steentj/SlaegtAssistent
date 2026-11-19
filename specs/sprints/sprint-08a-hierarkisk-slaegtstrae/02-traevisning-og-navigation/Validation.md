# Validering af trævisningen

Status: Planlagt.

- Headless med `GraphWorkspaceUiTests` som mønster. Verificér, at grafens første underfane viser proband og direkte relationer, at de tre valg ændrer gruppesynlighed, og at ingen dybere personrækker er synlige som standard. Kontroller ydre dokumentindekser.
- ViewModel-test. Åbn en gren, skift underfane, retning og editorfane, og vend tilbage. Kontrollér bevaret tilstand og separat udfoldning af samme person i to anepositioner. Simulér import med én ændret sti og hurtigt skift mellem to arbejdsområder, inklusive forsinket indlæsning fra det første.
- ViewModel-test. Brug en person med GEDCOM-post og uden dokument som proband, prøv en dokumentproband uden GEDCOM-post, og test manglende og ugyldige gemte proband-id'er. Efter genstart genbruges det samme gyldige id i begge visninger, også uden Graphviz.
- Fælles navigation. Brug piletaster, Enter og »Åbn biografi«. Kontroller skærmlæserens navn og udfoldning fra træet, og kontroller at Enter på en gruppe ikke åbner et dokument. Åbn en biografi med ugemt tekst, og vend tilbage til samme trægren. Prøv personnetværkets egne piletaster, zoom og eksport.
- Native prøve på projektets målplatforme i begge temaer. Brug 960 × 600, høj DPI og dansk tekst med lange navne. Prøv et stort senest kendt GEDCOM-snapshot eller generer mindst 10.000 syntetiske personer. Verificér i en test, at indlæsning af dybe forekomster sker på forespørgsel, og at import og skift af underfane er responsive. Registrér platform, datasæt, observationer og eventuelle blokeringer.
