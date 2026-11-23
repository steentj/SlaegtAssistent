#!/usr/bin/env python3
"""Afprøv slægtstræets tre formater i en lokal værktøjspakke."""

import json
from pathlib import Path
import subprocess
import zipfile


def check_family_tree(package, job, environment):
    executable = package / ("slaegt-export.exe" if (package / "slaegt-export.exe").is_file() else "slaegt-export")
    document = {
        "title": "Rodperson nr. 1",
        "rows": [{
            "label": "Rodperson",
            "cells": [["1", "Søren Æble", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"]],
        }],
    }
    input_path = job / "slaegtstrae.json"
    input_path.write_text(json.dumps(document, ensure_ascii=False), encoding="utf-8")
    for extension in ("pdf", "docx", "odt"):
        output = job / ("slaegtstrae." + extension)
        request = {
            "protocolVersion": 1, "jobId": "slaegtstrae-" + extension,
            "operation": "family-tree-" + extension, "inputPath": str(input_path),
            "outputPath": str(output), "assetRoot": str(job / "assets"),
            "options": {"paper": "A4", "orientation": "landscape"},
        }
        request_path = job / "slaegtstrae-request.json"
        request_path.write_text(json.dumps(request), encoding="utf-8")
        process = subprocess.run([str(executable), "--request", str(request_path)], capture_output=True,
                                 text=True, env=environment, timeout=120, check=True)
        if json.loads(process.stdout).get("status") != "ok":
            raise RuntimeError("Prøvepakken kunne ikke danne slægtstræ i " + extension + ".")
        if extension == "pdf":
            if not output.read_bytes().startswith(b"%PDF-"):
                raise RuntimeError("Slægtstræets PDF er ugyldig.")
        else:
            with zipfile.ZipFile(output) as archive:
                name = "word/document.xml" if extension == "docx" else "content.xml"
                if "Søren Æble" not in archive.read(name).decode("utf-8"):
                    raise RuntimeError("Slægtstræets kontordokument mangler personens navn.")
