#!/usr/bin/env python3
"""Byg en foreløbig lokal PDF-værktøjspakke til udviklingsprøver på macOS."""

import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    parser = argparse.ArgumentParser(description="Byg og afprøv en foreløbig macOS-pakke uden globale installationer.")
    parser.add_argument("--python", type=Path, required=True, help="Python i et særskilt miljø med WeasyPrint og PyInstaller")
    parser.add_argument("--biblioteker", type=Path, required=True, help="Mappe med private, indbyrdes flytbare dylib-filer")
    parser.add_argument("--skrifttype", type=Path, required=True, help="Lokal skrifttype med dansk tegnsæt")
    parser.add_argument("--skrifttypelicens", type=Path, required=True, help="Licens for skrifttypen")
    parser.add_argument("--maal", type=Path, required=True, help="Ny mappe til prøvepakken")
    args = parser.parse_args()
    python = args.python.absolute()
    libraries = args.biblioteker.resolve(strict=True)
    target = args.maal.absolute()
    if not python.is_file() or not libraries.is_dir() or not list(libraries.glob("*.dylib")) or not args.skrifttype.is_file() or not args.skrifttypelicens.is_file():
        parser.error("Python, biblioteker, skrifttype eller skrifttypelicens mangler.")
    if target.exists() or target.is_symlink():
        parser.error("Målmappen findes allerede; vælg en ny mappe.")
    script = Path(__file__).resolve().parents[1] / "sidecar/export/export.py"
    with tempfile.TemporaryDirectory(prefix="slaegt-pdfpakke-") as temporary:
        root = Path(temporary)
        env = os.environ.copy()
        env["DYLD_FALLBACK_LIBRARY_PATH"] = str(libraries)
        subprocess.run([str(python), "-m", "PyInstaller", "--noconfirm", "--clean", "--onedir", "--name", "slaegt-export", "--distpath", str(root / "dist"), "--workpath", str(root / "build"), "--specpath", str(root), "--collect-all", "weasyprint", "--add-binary", str(libraries / "*.dylib") + ":.", str(script)], check=True, env=env)
        package = root / "dist/slaegt-export"
        fonts = package / "_internal/fonts"
        fonts.mkdir()
        shutil.copy2(args.skrifttype, fonts / ("graf" + args.skrifttype.suffix.lower()))
        shutil.copy2(args.skrifttypelicens, fonts / "licens.txt")
        fontconfig = package / "_internal/fonts.conf"
        fontconfig.write_text('<?xml version="1.0"?><!DOCTYPE fontconfig SYSTEM "fonts.dtd"><fontconfig><dir prefix="relative">fonts</dir><cachedir prefix="xdg">fontconfig</cachedir></fontconfig>', encoding="utf-8")
        job = root / "job"
        job.mkdir()
        (job / "assets").mkdir()
        (job / "input.html").write_text('<html><body><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><circle cx="50" cy="50" r="8" fill="#4488dd"/><text x="10" y="75" font-size="10">Søren Æble</text></svg></body></html>', encoding="utf-8")
        request = {"protocolVersion": 1, "jobId": "proeve", "operation": "graph-pdf", "inputPath": str(job / "input.html"), "outputPath": str(job / "result.pdf"), "assetRoot": str(job / "assets"), "options": {"paper": "A4", "orientation": "landscape"}}
        (job / "request.json").write_text(json.dumps(request), encoding="utf-8")
        probe_env = {"PATH": "/usr/bin:/bin", "HOME": str(root), "DYLD_FALLBACK_LIBRARY_PATH": str(package / "_internal"), "FONTCONFIG_FILE": str(fontconfig)}
        version_result = subprocess.run([str(package / "slaegt-export"), "--version"], capture_output=True, text=True, env=probe_env, timeout=30, check=True)
        version = json.loads(version_result.stdout)
        if version.get("protocolVersion") != 1 or version.get("toolVersion") != "0.1.0" or not version.get("rendererVersion"):
            raise RuntimeError("Prøvepakken har en ugyldig værktøjsversion.")
        result = subprocess.run([str(package / "slaegt-export"), "--request", str(job / "request.json")], capture_output=True, text=True, env=probe_env, timeout=120, check=True)
        response = json.loads(result.stdout)
        if response.get("status") != "ok" or not (job / "result.pdf").read_bytes().startswith(b"%PDF-"):
            raise RuntimeError("Prøvepakken gav ikke en gyldig PDF.")
        shutil.copytree(package, target, symlinks=True)
    print(f"Foreløbig prøvepakke dannet og afprøvet: {target}")
    print("Pakken er ikke en færdig distribution: versioner, licenser og platformstest mangler.")


if __name__ == "__main__":
    main()
