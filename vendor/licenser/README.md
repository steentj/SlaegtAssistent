# Licenser for de lokale værktøjer

Denne mappe samler licenstekster og metadata, der blev anvendt ved den lokale macOS-pakke. Tredjepartsteksterne er bevaret på deres oprindelige sprog.

- `graphviz/COPYING` kommer fra den anvendte Graphviz 16.1.1-kildekode.
- `python` indeholder licenser og metadata fra det tidligere Python 3.13.1-miljø til PDF-pakken. `pillow-sbom.json` er Pillow-pakkens egen fortegnelse over dens indbyggede komponenter. Det aktuelle eksportarkivs `licenser/python` beskriver miljøet anvendt til den nye pakke.
- `native` indeholder tilgængelige licenstekster fra lokale Homebrew-pakker eller deres kildearkiver. Mappenavnene angiver den tilhørende pakke. Skrifttypens licens findes også i PDF-værktøjspakkens `_internal/fonts/licens.txt`.
- `filinventar.json` angiver kontrolsummer for de faktiske pakkede filer og målene for symbolske links. Det kan genskabes med `python3 scripts/registrer-lokale-vaerktoejer.py`, når begge lokale værktøjer er på plads.

De lokale værktøjsarkiver er fastlåste binærpakker, der kan genskabes uden netværk. Oplysningerne her er ikke en attestering af, at alle binærfilers kildearkiver og præcise versioner er identificeret; især kan Pillow indeholde egne kopier af native biblioteker. En separat distribution til andre kræver, at den fulde oprindelse og de relevante licensbetingelser gennemgås først.

Den opdaterede macOS-eksportpakke indeholder desuden Pandoc 3.13 og GMP 6.3.0 samt deres medfølgende licenstekster under `licenser`. Python-miljøets pakkeversioner og licensfiler er indsamlet under `licenser/python` i selve arkivet. Disse oplysninger supplerer, men erstatter ikke, den ovenfor beskrevne kontrol af native komponenters oprindelse og distributionsforpligtelser.

Linux x64-prøvearkivet indeholder indsamlede Python- og Pandoc-licenser samt en pakkeoversigt og tilgængelige Ubuntu-copyrightfiler under `licenser/linux`. Arkivet er opbygget under emulering, og listen over installerede pakker dokumenterer ikke i sig selv nøjagtigt, hvilke native komponenter der indgår eller alle nødvendige distributionsforpligtelser.

Windows x64-prøvearkivet indeholder Python- og Pandoc-licenser samt indsamlede MSYS2-pakkeoplysninger, kontrolsummer og tilgængelige licenstekster under `licenser`. Pakken er afprøvet under Wine og emulering. En samlet kontrol af native komponenter, runtime og distributionsforpligtelser er fortsat nødvendig.
