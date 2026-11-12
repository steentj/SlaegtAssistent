#!/usr/bin/env python3
"""Registrér kontrolsummer for de lokale værktøjer uden persondata."""

import hashlib
import json
from pathlib import Path


def inventory(directory):
    result = []
    for path in sorted(directory.rglob("*")):
        relative = path.relative_to(directory).as_posix()
        if path.is_symlink():
            result.append({"sti": relative, "symbolskLink": str(path.readlink())})
        elif path.is_file():
            with path.open("rb") as source:
                checksum = hashlib.file_digest(source, "sha256").hexdigest()
            result.append({"sti": relative, "sha256": checksum})
    return result


def main():
    root = Path(__file__).resolve().parents[1]
    export = root / "tools/export/osx-arm64/slaegt-export"
    graphviz = root / "tools/graphviz/osx-arm64"
    if not (export / "slaegt-export").is_file() or not (graphviz / "sfdp").is_file():
        raise SystemExit("Genskab først begge lokale værktøjspakker med en almindelig build.")
    report = {
        "formatVersion": 1,
        "platform": "macOS arm64",
        "pdfVaerktoej": {"oplysteVersioner": {"python": "3.13.1", "weasyprint": "66.0", "pyinstaller": "6.22.3"}, "filer": inventory(export)},
        "graphviz": {"oplystVersion": "16.1.1", "filer": inventory(graphviz)},
    }
    target = root / "vendor/licenser/filinventar.json"
    target.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Filinventar skrevet: {target}")


if __name__ == "__main__":
    main()
