import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


GRAPH = '''<html><body><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 200">
<line x1="60" y1="100" x2="200" y2="100" stroke="#777777" stroke-width="2"/>
<line x1="200" y1="100" x2="340" y2="100" stroke="#777777" stroke-width="2"/>
<circle cx="60" cy="100" r="8" fill="#d84444"/>
<circle cx="200" cy="100" r="12" fill="#4488dd" stroke="#000000" stroke-width="2"/>
<circle cx="340" cy="100" r="8" fill="#d84444"/>
<text x="45" y="130" font-size="14">Åse Østergaard</text>
<text x="175" y="130" font-size="14">Søren Æble</text>
<text x="315" y="130" font-size="14">Anna</text>
</svg></body></html>'''


@unittest.skipUnless(os.environ.get("SLAEGTSASSISTENT_PDF_INTEGRATION") == "1", "Kræver lokalt afprøvede PDF-afhængigheder og pypdf")
class PdfIntegrationTests(unittest.TestCase):
    def test_pdf_has_searchable_danish_text_and_paper_size(self):
        from pypdf import PdfReader
        with tempfile.TemporaryDirectory(prefix="graf æøå ") as temporary:
            root = Path(temporary).resolve()
            (root / "assets").mkdir()
            (root / "input.html").write_text(GRAPH, encoding="utf-8")
            script = Path(__file__).resolve().parents[1] / "export.py"
            for paper, orientation, expected in (("A4", "landscape", (841.89, 595.28)), ("A4", "portrait", (595.28, 841.89)), ("A3", "landscape", (1190.55, 841.89)), ("A3", "portrait", (841.89, 1190.55))):
                with self.subTest(paper=paper, orientation=orientation):
                    output = root / f"{paper}-{orientation}.pdf"
                    request = {"protocolVersion": 1, "jobId": "graf-123", "operation": "graph-pdf", "inputPath": str(root / "input.html"), "outputPath": str(output), "assetRoot": str(root / "assets"), "options": {"paper": paper, "orientation": orientation}}
                    (root / "request.json").write_text(json.dumps(request), encoding="utf-8")
                    result = subprocess.run([sys.executable, str(script), "--request", str(root / "request.json")], capture_output=True, text=True, timeout=30)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertEqual(json.loads(result.stdout)["status"], "ok")
                    reader = PdfReader(output)
                    self.assertEqual(len(reader.pages), 1)
                    page = reader.pages[0]
                    self.assertAlmostEqual(float(page.mediabox.width), expected[0], places=1)
                    self.assertAlmostEqual(float(page.mediabox.height), expected[1], places=1)
                    for name in ("Åse Østergaard", "Søren Æble", "Anna"):
                        self.assertIn(name, page.extract_text())
                    self.assertIn(b" l\n", page.get_contents().get_data())


if __name__ == "__main__":
    unittest.main()
