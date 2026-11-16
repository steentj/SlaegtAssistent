#!/usr/bin/env python3
"""Genskab den afprøvede lokale eksportpakke fra projektets arkiv."""

import argparse
import hashlib
from pathlib import Path
import shutil
import tarfile
import tempfile


EXPECTED_SHA256 = "a607976be5c0e9f1dd073b120d57586f6f0ff3007faf7209f3bbb2a7781946e9"


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="Genskab Eksportværktøjet til Linux x64 uden netværk.")
    parser.add_argument("--maal", type=Path, default=root / "tools/export/linux-x64/slaegt-export", help="Ny mappe til værktøjspakken")
    args = parser.parse_args()
    target = args.maal.absolute()
    archive = root / "vendor/eksport/linux-x64/eksportvaerktoej.tar.gz"
    if target.exists() or target.is_symlink():
        parser.error("Målmappen findes allerede. Vælg en ny mappe.")
    with archive.open("rb") as source_file:
        checksum = hashlib.file_digest(source_file, "sha256").hexdigest()
    if checksum != EXPECTED_SHA256:
        raise ValueError("Værktøjsarkivets kontrolsum stemmer ikke.")
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="slaegt-genskab-", dir=target.parent) as temporary:
        with tarfile.open(archive, "r:gz") as source:
            source.extractall(temporary, filter="data")
        package = Path(temporary) / "slaegt-export"
        if not all((package / name).is_file() for name in ("slaegt-export", "pandoc", "reference.docx", "reference.odt", "_internal/fonts.conf", "_internal/libpango-1.0.so.0")):
            raise ValueError("Værktøjsarkivet mangler nødvendige filer.")
        shutil.move(str(package), str(target))
    print(f"Eksportværktøjet er genskabt: {target}")


if __name__ == "__main__":
    main()
