import os
import sys
import unittest
import tempfile
import subprocess
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from family_tree_layout import build_layout, validate_document
from family_tree_pdf import render_pdf
from family_tree_office import render_docx, render_odt
from protocol import ProtocolError


class FamilyTreeLayoutTests(unittest.TestCase):
    def test_fem_generationer_med_lange_navne_uden_generationsoverskrifter(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        tools = os.environ.get("SLAEGTSASSISTENT_TEST_TOOLS")
        libreoffice = os.environ.get("SLAEGTSASSISTENT_TEST_LIBREOFFICE")
        if not all((font, tools, libreoffice)):
            self.skipTest("Lokale eksportværktøjer er ikke angivet.")
        from pypdf import PdfReader
        rows = []
        for generation in range(5):
            count = 2 ** generation
            cells = []
            for number in range(count, count * 2):
                name = f"#{number} Christine Elisabeth Petersen"
                cells.append([name, "Født: 01-01-1900", "Fødested: København",
                              "Død: 01-01-1980", "Dødssted: København"] if count <= 5
                             else [name, "1900 – 1980"])
            rows.append({"label": f"Generationsoverskrift {generation}", "cells": cells})
        layout = build_layout({"title": "", "rows": rows}, "A4", font)
        self.assertEqual(len(layout.rows), 5)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for extension, render in (("pdf", render_pdf), ("docx", render_docx), ("odt", render_odt)):
                output = root / f"aner-{extension}.{extension}"
                if extension == "pdf":
                    render(layout, output)
                else:
                    render(layout, output, Path(tools))
                    subprocess.run([libreoffice, "-env:UserInstallation=" + (root / "profile").as_uri(),
                                    "--headless", "--convert-to", "pdf", "--outdir", str(root), str(output)],
                                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=60)
                pdf = PdfReader(output.with_suffix(".pdf"))
                self.assertEqual(len(pdf.pages), 1, extension)
                text = pdf.pages[0].extract_text()
                self.assertNotIn("Generationsoverskrift", text)
                self.assertIn("#31", text)
                self.assertIn("Christine", text)

    def test_validation_afviser_forkerte_felter(self):
        with self.assertRaises(ProtocolError):
            validate_document({"title": "Træ", "rows": [{"label": "Børn", "cells": [["for få"]]}]})

    def test_lodret_og_vandret_layout_bevarer_teksten(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        document = {
            "title": "Rodperson nr. 2 i hovedtræet",
            "rows": [
                {"label": "Rodperson", "cells": [["1", "Anna Æble", "Født: Ukendt", "Fødested: Århus", "Død: Ukendt", "Dødssted: Ukendt"]]},
                {"label": "Børn", "cells": [["Ukendt", "Et usædvanligt langt navn", "1900 – 1980"] for _ in range(9)]},
            ],
        }
        layout = build_layout(document, "A3", font)
        self.assertFalse(layout.rows[0].vertical)
        self.assertTrue(layout.rows[1].vertical)
        self.assertEqual("".join(layout.rows[1].cells[0].lines), "UkendtEt usædvanligt langt navn1900 – 1980")
        self.assertLessEqual(layout.font_size, 10)

    def test_for_bred_raekke_afvises(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        document = {"title": "Træ", "rows": [{"label": "Børn", "cells": [["Ukendt", "Anna", "1900 – 1980"] for _ in range(1000)]}]}
        with self.assertRaises(ProtocolError) as error:
            build_layout(document, "A4", font)
        self.assertEqual(error.exception.code, "page-overflow")

    def test_langt_navn_ud_over_en_sides_plads_afvises(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        document = {"title": "Træ", "rows": [{"label": "Børn",
                    "cells": [["Ukendt", "Sønder" * 500, "1900 – 1980"] for _ in range(32)]}]}
        with self.assertRaises(ProtocolError) as error:
            build_layout(document, "A3", font)
        self.assertEqual(error.exception.code, "page-overflow")

    def test_graenser_for_cellelayout_paa_a4(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        for count in (4, 5, 6, 8, 9):
            with self.subTest(antal=count):
                lines = (["3", "Åse <Øster> & Søn", "Født: 1900", "Fødested: Århus",
                          "Død: Ukendt", "Dødssted: Ukendt"] if count <= 5 else
                         ["3", "Åse <Øster> & Søn", "1900 – Ukendt"])
                layout = build_layout({"title": "Træ", "rows": [
                    {"label": "Generation", "cells": [lines for _ in range(count)]}]}, "A4", font)
                self.assertEqual(layout.rows[0].vertical, count >= 9)
                self.assertEqual("".join(layout.rows[0].cells[0].lines), "".join(lines))

    def test_aner_holder_position_naar_en_gren_mangler(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        person = ["#1 Rod", "Født: Ukendt", "Fødested: Ukendt", "Død: Ukendt", "Dødssted: Ukendt"]
        document = {"title": "", "rows": [
            {"label": "Rodperson", "cells": [person], "slots": 1, "positions": None},
            {"label": "Forældre", "cells": [person, person], "slots": 2, "positions": [0, 1]},
            {"label": "Bedsteforældre", "cells": [person, person], "slots": 4, "positions": [2, 3]},
        ]}
        layout = build_layout(document, "A3", font)
        self.assertEqual(layout.rows[0].width, layout.rows[1].width * 2)
        self.assertEqual(layout.rows[1].width, layout.rows[2].width * 2)
        self.assertEqual([cell.spacer for cell in layout.rows[2].cells], [True, True, False, False])

    def test_dybe_aner_bevarer_tomme_pladser_i_kontorformater(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        tools = os.environ.get("SLAEGTSASSISTENT_TEST_TOOLS")
        if not font or not tools:
            self.skipTest("Lokale eksportværktøjer er ikke angivet.")
        person = ["#62 A", "Født: 1900", "Fødested: Ukendt", "Død: 1980", "Dødssted: Ukendt"]
        document = {"title": "", "rows": [
            {"label": "Tiptipoldeforældre", "cells": [person, person],
             "slots": 32, "positions": [30, 31]}]}
        layout = build_layout(document, "A3", font)
        self.assertEqual(sum(cell.spacer for cell in layout.rows[0].cells), 30)
        with tempfile.TemporaryDirectory() as temporary:
            for extension, render in (("docx", render_docx), ("odt", render_odt)):
                output = Path(temporary) / ("aner." + extension)
                render(layout, output, Path(tools))
                with zipfile.ZipFile(output) as archive:
                    xml = archive.read("word/document.xml" if extension == "docx" else "content.xml")
                    root = ET.fromstring(xml)
                    namespace = ("http://schemas.openxmlformats.org/wordprocessingml/2006/main" if extension == "docx"
                                 else "urn:oasis:names:tc:opendocument:xmlns:table:1.0")
                    element = "tc" if extension == "docx" else "table-cell"
                    cells = list(root.iter(f"{{{namespace}}}{element}"))
                    self.assertEqual(len(cells), 32)
                    self.assertFalse("".join(cells[0].itertext()).strip())
                    self.assertIn("#62 A", "".join(cells[30].itertext()))

    def test_pdf_er_liggende_og_indeholder_alle_navne(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        if not font:
            self.skipTest("En lokal testskrifttype er ikke angivet.")
        try:
            from pypdf import PdfReader
        except ImportError:
            self.skipTest("PDF-læseren er ikke installeret.")
        document = {
            "title": "Rodperson nr. 1",
            "rows": [
                {"label": "Rodperson", "cells": [["1", "Søren Æble", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"]]},
                {"label": "Forældre", "cells": [[str(n), f"Forælder {n}", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"] for n in range(2)]},
                {"label": "Bedsteforældre", "cells": [[str(n), f"Ane {n}", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"] for n in range(4)]},
                {"label": "Generation 4", "cells": [[str(n), f"Person {n}", "1900 – 1980"] for n in range(8)]},
                {"label": "Generation 5", "cells": [[str(n), f"Person {n}", "1900 – 1980"] for n in range(9)]},
                {"label": "Børn", "cells": [["Ukendt", f"Anna Øster {n:02}", "1900 – 1980"] for n in range(32)]},
            ],
        }
        layout = build_layout(document, "A3", font)
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "træ.pdf"
            render_pdf(layout, output)
            pdf = PdfReader(output)
            self.assertEqual(len(pdf.pages), 1)
            self.assertGreater(pdf.pages[0].mediabox.width, pdf.pages[0].mediabox.height)
            text = pdf.pages[0].extract_text()
            self.assertIn("Søren Æble", text)
            for index in range(32):
                self.assertIn(f"AnnaØster{index:02}", "".join(text.split()))

    def test_kontorformater_bevarer_celler_og_lodret_tekst(self):
        font = os.environ.get("SLAEGTSASSISTENT_TEST_FONT")
        tools = os.environ.get("SLAEGTSASSISTENT_TEST_TOOLS")
        libreoffice = os.environ.get("SLAEGTSASSISTENT_TEST_LIBREOFFICE")
        if not all((font, tools, libreoffice)):
            self.skipTest("Lokale eksportværktøjer er ikke angivet.")
        try:
            from pypdf import PdfReader
        except ImportError:
            self.skipTest("PDF-læseren er ikke installeret.")
        document = {
            "title": "Rodperson nr. 1",
            "rows": [
                {"label": "Rodperson", "cells": [["1", "Søren Æble", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"]]},
                {"label": "Forældre", "cells": [[str(n), f"Forælder {n}", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"] for n in range(2)]},
                {"label": "Bedsteforældre", "cells": [[str(n), f"Ane {n}", "Født: 1900", "Fødested: Århus", "Død: 1980", "Dødssted: Odense"] for n in range(4)]},
                {"label": "Generation 4", "cells": [[str(n), f"Person {n}", "1900 – 1980"] for n in range(8)]},
                {"label": "Generation 5", "cells": [[str(n), f"Person {n}", "1900 – 1980"] for n in range(9)]},
                {"label": "Børn", "cells": [["Ukendt", f"Anna Øster {n:02}", "1900 – 1980"] for n in range(32)]},
            ],
        }
        layout = build_layout(document, "A3", font)
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for extension, render in (("docx", render_docx), ("odt", render_odt)):
                output = root / f"trae-{extension}.{extension}"
                render(layout, output, Path(tools))
                with zipfile.ZipFile(output) as archive:
                    xml = archive.read("word/document.xml" if extension == "docx" else "content.xml")
                    parsed = ET.fromstring(xml)
                    self.assertIn("Anna Øster 00", "".join(parsed.itertext()))
                    if extension == "docx":
                        direction = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}textDirection"
                        self.assertEqual(len(list(parsed.iter(direction))), 41)
                    else:
                        self.assertIn(b"tb-rl", xml)
                        self.assertEqual(archive.infolist()[0].filename, "mimetype")
                        self.assertEqual(archive.infolist()[0].compress_type, zipfile.ZIP_STORED)
                subprocess.run([libreoffice, "-env:UserInstallation=" + (root / "profile").as_uri(),
                                "--headless", "--convert-to", "pdf", "--outdir", str(root), str(output)],
                               check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=60)
                pdf = PdfReader(output.with_suffix(".pdf"))
                self.assertEqual(len(pdf.pages), 1)
                self.assertGreater(pdf.pages[0].mediabox.width, pdf.pages[0].mediabox.height)
                text = pdf.pages[0].extract_text()
                for index in range(32):
                    self.assertIn(f"Anna Øster {index:02}", text)


if __name__ == "__main__":
    unittest.main()
