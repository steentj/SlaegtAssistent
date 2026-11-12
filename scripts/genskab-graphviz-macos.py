#!/usr/bin/env python3
"""Genskab den afprøvede lokale Graphviz-pakke til macOS arm64."""

import argparse
import hashlib
from pathlib import Path
import shutil
import tarfile
import tempfile

EXPECTED_SHA256 = "285ecb6749d3e9cf67f7035028ecec32d2db16e9e2e964d910bb15331727dc62"


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description="Genskab grafværktøjet uden netværk.")
    parser.add_argument("--maal", type=Path, default=root / "tools/graphviz/osx-arm64")
    target = parser.parse_args().maal.absolute()
    archive = root / "vendor/graphviz/osx-arm64/graphviz.tar.gz"
    if target.exists() or target.is_symlink():
        parser.error("Målmappen findes allerede. Vælg en ny mappe.")
    with archive.open("rb") as source_file:
        if hashlib.file_digest(source_file, "sha256").hexdigest() != EXPECTED_SHA256:
            raise ValueError("Grafværktøjets kontrolsum stemmer ikke.")
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="slaegt-graphviz-", dir=target.parent) as temporary:
        package = Path(temporary) / "pakke"
        package.mkdir()
        with tarfile.open(archive, "r:gz") as source:
            source.extractall(package, filter="data")
        if not (package / "sfdp").is_file():
            raise ValueError("Grafværktøjets arkiv mangler sfdp.")
        shutil.move(str(package), str(target))
    print(f"Grafværktøjet er genskabt: {target}")


if __name__ == "__main__":
    main()
