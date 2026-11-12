# Licenser for de lokale værktøjer

Denne mappe samler licenstekster og metadata, der blev anvendt ved den lokale macOS-pakke. Tredjepartsteksterne er bevaret på deres oprindelige sprog.

- `graphviz/COPYING` kommer fra den anvendte Graphviz 16.1.1-kildekode.
- `python` indeholder licenser og metadata fra det Python 3.13.1-miljø, der blev brugt til PDF-pakken. `pillow-sbom.json` er Pillow-pakkens egen fortegnelse over dens indbyggede komponenter.
- `native` indeholder tilgængelige licenstekster fra lokale Homebrew-pakker eller deres kildearkiver. Mappenavnene angiver den tilhørende pakke. Skrifttypens licens findes også i PDF-værktøjspakkens `_internal/fonts/licens.txt`.
- `filinventar.json` angiver kontrolsummer for de faktiske pakkede filer og målene for symbolske links. Det kan genskabes med `python3 scripts/registrer-lokale-vaerktoejer.py`, når begge lokale værktøjer er på plads.

De lokale værktøjsarkiver er fastlåste binærpakker, der kan genskabes uden netværk. Oplysningerne her er ikke en attestering af, at alle binærfilers kildearkiver og præcise versioner er identificeret; især kan Pillow indeholde egne kopier af native biblioteker. En separat distribution til andre kræver, at den fulde oprindelse og de relevante licensbetingelser gennemgås først.
