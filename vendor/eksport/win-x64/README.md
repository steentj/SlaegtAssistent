# Eksportværktøj til Windows x64

Arkivet indeholder en privat sidecar med Python 3.13.1, WeasyPrint 66.0, Pandoc 3.13, referencestile, skrifttype, native biblioteker samt indsamlede licenser og pakkeoplysninger. Det kan genskabes uden netværk med `python scripts/genskab-eksportvaerktoej-windows.py`. En Windows-build genskaber og kopierer pakken, hvis den lokale mappe mangler.

Pakken er bygget og afprøvet under Wine i en emuleret Linux x64-container uden netværk. PDF, DOCX og ODT er kontrolleret med dansk tekst, lokalt billede og links. Dette er ikke en godkendelse på native Windows; appens samlede Windows-build, brugerflade og distribution skal stadig prøves der. Licensmaterialet i arkivet er indsamlet, men den fulde gennemgang af komponenternes oprindelse og distributionsforpligtelser er ikke afsluttet.
