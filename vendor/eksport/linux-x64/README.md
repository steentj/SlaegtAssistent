# Eksportværktøj til Linux x64

Arkivet er bygget i Ubuntu 24.04 x64 under emulering på macOS og indeholder privat sidecar, Pandoc 3.13, WeasyPrint 66.0, referencestile, skrifttype samt indsamlet licens- og pakkeinformation. Det kan genskabes med `python3 scripts/genskab-eksportvaerktoej-linux.py`. Appens build kopierer den lokale mappe til output, når den findes.

I en Ubuntu 24.04 x64-container under emulering er sidecaren prøvet med slægtstræ til PDF, DOCX og ODT uden netværk. Dette er ikke en native platformsgodkendelse. Kompatibilitet med andre Linux-distributioner og ældre glibc-versioner, en fuldt pakket app og den samlede licensgennemgang mangler fortsat.
