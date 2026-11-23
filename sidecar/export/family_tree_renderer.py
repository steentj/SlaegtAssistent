"""Konvertér et valideret generationsgrundlag med lokale værktøjer."""

import json
import sys
from pathlib import Path

from family_tree_layout import build_layout
from family_tree_office import render_docx, render_odt
from family_tree_pdf import render_pdf
from protocol import ProtocolError, _unique_object


def render_family_tree(input_path, output_path, operation, options):
    try:
        document = json.loads(input_path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object)
    except (OSError, UnicodeError, ValueError):
        raise ProtocolError("invalid-input") from None
    bundled_root = Path(getattr(sys, "_MEIPASS", Path(sys.executable).resolve().parent))
    fonts = bundled_root / "fonts"
    font = next((fonts / name for name in ("graf.ttf", "biografi.ttf")
                 if (fonts / name).is_file()), fonts / "graf.ttf")
    layout = build_layout(document, options["paper"], font)
    if operation == "family-tree-pdf":
        render_pdf(layout, output_path)
    elif operation == "family-tree-docx":
        render_docx(layout, output_path)
    elif operation == "family-tree-odt":
        render_odt(layout, output_path)
    else:
        raise ProtocolError("invalid-input")
