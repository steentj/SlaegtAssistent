# Lokalt eksportværktøj til macOS arm64

Arkivet indeholder en privat sidecar, WeasyPrint, Pandoc 3.13, GMP, referencestile, skrifttype og licensmateriale. Kontrolsummen kontrolleres før udpakning; appen kræver ikke system-Python, system-Pandoc eller netværk under eksport. Arkivet er en fast binær pakke til macOS arm64 og er ikke en dokumenteret reproduktion af binærfilerne fra kildekode.

Hvis `tools/export/osx-arm64/slaegt-export` mangler, genskaber en almindelig macOS-build pakken fra projektets arkiv. Den kan også genskabes med `python3 scripts/genskab-pdfvaerktoej-macos.py`. Scriptet kræver Python 3.12 eller nyere på udviklingsmaskinen og overskriver ikke en eksisterende mappe.

Sidecaren blev bygget med Python 3.13.1, WeasyPrint 66.0 og PyInstaller 6.22.3. Pakken er afprøvet lokalt med graf-PDF og biografier til PDF, DOCX og ODT. `licenser` i pakken og [licensoversigten](../../licenser/README.md) indeholder indsamlede licenser og pakkeversioner. Undersøg distributionens øvrige forpligtelser før offentlig udgivelse. Windows- og Linux-pakker er ikke en del af dette arkiv.
