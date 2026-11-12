# Lokalt PDF-værktøj til macOS arm64

Arkivet indeholder den allerede byggede PDF-sidecar, dens private biblioteker og en skrifttype. Det er en fast binær pakke til lokal brug på denne Mac. Kontrolsummen kontrolleres før udpakning, og der kræves ikke netværk eller Python i selve programmet.

Hvis den lokale mappe `tools/export/osx-arm64/slaegt-export` mangler, genskaber en almindelig macOS-build den automatisk fra projektets arkiv. Den kan også genskabes manuelt med:

```bash
python3 scripts/genskab-pdfvaerktoej-macos.py
dotnet build
```

Scriptet kræver Python 3.12 eller nyere på udviklingsmaskinen. Det overskriver ikke en eksisterende mappe. Appens build kopierer derefter pakken til sin egen værktøjsmappe og bevarer symbolske links.

Pakken blev fremstillet med Python 3.13.1, WeasyPrint 66.0 og PyInstaller 6.22.3. Arkivet gør det muligt at genskabe de samme pakkede filer; det er ikke en dokumenteret reproduktion af binærfilerne fra kildekode. Indsamlede licenser og begrænsninger ved oprindelsesoplysningerne står i [licensoversigten](../../licenser/README.md).
