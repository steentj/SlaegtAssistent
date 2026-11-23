#!/usr/bin/env python3
"""Genskab den afprøvede lokale eksportpakke fra projektets arkiv."""

import argparse
import hashlib
from pathlib import Path
import shutil
import tarfile
import tempfile


EXPECTED_SHA256 = "d9adcb0b4b30a0545c5f0e7d8af7856e058bb4dee8cb87c0277711f363793631"


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="Genskab Eksportværktøjet til Windows x64 uden netværk.")
    parser.add_argument("--maal", type=Path, default=root / "tools/export/win-x64/slaegt-export", help="Ny mappe til værktøjspakken")
    args = parser.parse_args()
    target = args.maal.absolute()
    archive = root / "vendor/eksport/win-x64/eksportvaerktoej.tar.gz"
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
        if not all((package / name).is_file() for name in ("slaegt-export.exe", "pandoc.exe", "reference.docx", "reference.odt", "_internal/fonts.conf", "_internal/libpango-1.0-0.dll")):
            raise ValueError("Værktøjsarkivet mangler nødvendige filer.")
        shutil.move(str(package), str(target))
    print(f"Eksportværktøjet er genskabt: {target}")


if __name__ == "__main__":
    main()
