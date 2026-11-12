import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from protocol import ProtocolError, read_request
from pdf_renderer import validate_html


class ProtocolTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="graf æøå ; ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        (self.root / "assets").mkdir()
        (self.root / "input.html").write_text("<html><body><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><circle cx=\"10\" cy=\"10\" r=\"4\" fill=\"red\"/><text x=\"10\" y=\"20\">Æ &amp; Ø</text></svg></body></html>", encoding="utf-8")
        self.request = {"protocolVersion": 1, "jobId": "job-1", "operation": "graph-pdf", "inputPath": str(self.root / "input.html"), "outputPath": str(self.root / "result.pdf"), "assetRoot": str(self.root / "assets"), "options": {"paper": "A4", "orientation": "landscape"}}

    def read(self):
        path = self.root / "request.json"
        path.write_text(json.dumps(self.request), encoding="utf-8")
        return read_request(str(path))

    def test_valid_request_and_graph(self):
        data, path, output = self.read()
        self.assertEqual(data["jobId"], "job-1")
        self.assertIn("Æ &amp; Ø", validate_html(path.read_text(encoding="utf-8")))
        self.assertEqual(output, self.root / "result.pdf")

    def test_invalid_fields_and_paths(self):
        for key, value in (("protocolVersion", 2), ("operation", "biography-pdf"), ("outputPath", str(self.root.parent / "outside.pdf")), ("options", {"paper": "A4", "orientation": "sideways"})):
            with self.subTest(key=key):
                old = self.request[key]
                self.request[key] = value
                with self.assertRaises(ProtocolError):
                    self.read()
                self.request[key] = old

    def test_symlink_is_rejected(self):
        (self.root / "link.html").symlink_to(self.root / "input.html")
        self.request["inputPath"] = str(self.root / "link.html")
        with self.assertRaises(ProtocolError):
            self.read()

    def test_active_resources_are_rejected(self):
        for element in ('<image href="https://example.org/a"/>', '<script/>', '<circle style="fill:url(http://example.org)"/>', '<foreignObject/>'):
            with self.subTest(element=element), self.assertRaises(ProtocolError):
                validate_html('<html><body><svg xmlns="http://www.w3.org/2000/svg">' + element + '</svg></body></html>')

    def test_html_styles_and_entities_are_rejected(self):
        for content in ('<!DOCTYPE html><html/>', '<html><head><style>@import "https://example.org";</style></head><body/></html>', '<html><body><svg xmlns="http://www.w3.org/2000/svg"><text>&unknown;</text></svg></body></html>'):
            with self.subTest(content=content), self.assertRaises(ProtocolError):
                validate_html(content)

    def test_missing_renderer_returns_one_json_result(self):
        self.read()
        script = Path(__file__).resolve().parents[1] / "export.py"
        result = subprocess.run([sys.executable, "-S", str(script), "--request", str(self.root / "request.json")], capture_output=True, text=True, check=False)
        self.assertNotEqual(result.returncode, 0)
        response = json.loads(result.stdout)
        self.assertEqual(response["jobId"], "job-1")
        self.assertEqual(response["errorCode"], "missing-tool")
        self.assertFalse((self.root / "result.pdf").exists())

    def test_version_reports_missing_renderer_as_json(self):
        script = Path(__file__).resolve().parents[1] / "export.py"
        result = subprocess.run([sys.executable, "-S", str(script), "--version"], capture_output=True, text=True, check=False)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(json.loads(result.stdout), {"protocolVersion": 1, "errorCode": "missing-tool"})


if __name__ == "__main__":
    unittest.main()
