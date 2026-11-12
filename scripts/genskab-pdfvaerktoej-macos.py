#!/usr/bin/env python3
"""Genskab den afprøvede lokale PDF-pakke fra projektets arkiv."""

import argparse
import hashlib
from pathlib import Path
import shutil
import tarfile
import tempfile


EXPECTED_SHA256 = "90375500d4ff1a566a48c2d159b59740ca7c40534edc75707a4a1e932599ba1f"


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="Genskab PDF-værktøjet til denne Mac uden netværk.")
    parser.add_argument("--maal", type=Path, default=root / "tools/export/osx-arm64/slaegt-export", help="Ny mappe til værktøjspakken")
    args = parser.parse_args()
    target = args.maal.absolute()
    archive = root / "vendor/eksport/osx-arm64/pdf-vaerktoej.tar.gz"
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
        if not (package / "slaegt-export").is_file() or not (package / "_internal/fonts.conf").is_file():
            raise ValueError("Værktøjsarkivet mangler nødvendige filer.")
        shutil.move(str(package), str(target))
    print(f"PDF-værktøjet er genskabt: {target}")


if __name__ == "__main__":
    main()
