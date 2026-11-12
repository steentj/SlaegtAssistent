# Lokalt grafværktøj til macOS arm64

Arkivet indeholder den lokalt afprøvede `sfdp` og dens private biblioteker. Ved en macOS-build genskabes værktøjet automatisk, hvis `tools/graphviz/osx-arm64` mangler. Arkivet kontrolleres med SHA-256 inden udpakning og kræver ikke netværk.

Manuel genskabelse kan foretages med `python3 scripts/genskab-graphviz-macos.py`. Scriptet kræver Python 3.12 eller nyere på udviklingsmaskinen og overskriver ikke en eksisterende mappe.

Arkivet fastholder de afprøvede binærfiler; det er ikke en genskabelse fra kildekode. Indsamlede tredjepartslicenser og begrænsninger ved oplysninger om komponenternes oprindelse findes i [licensoversigten](../../licenser/README.md).
