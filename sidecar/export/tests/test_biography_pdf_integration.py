import base64
import hashlib
from http.server import BaseHTTPRequestHandler, HTTPServer
from threading import Thread
from unittest.mock import patch
import os
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from biography_renderer import STYLE, render_biography


@unittest.skipUnless(os.environ.get('SLAEGTSASSISTENT_PDF_INTEGRATION') == '1', 'Kræver lokalt afprøvet WeasyPrint og pypdf')
class BiographyPdfIntegrationTests(unittest.TestCase):
    def test_tekstlink_hentes_ikke_og_eksternt_billede_afvises(self):
        from protocol import ProtocolError
        requests = []
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                requests.append(self.path)
                self.send_response(200)
                self.end_headers()
            def log_message(self, *args):
                pass
        server = HTTPServer(('127.0.0.1', 0), Handler)
        thread = Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            with tempfile.TemporaryDirectory() as directory:
                root = Path(directory).resolve()
                (root / 'assets').mkdir()
                html = root / 'input.html'
                url = 'http://127.0.0.1:' + str(server.server_port) + '/test'
                html.write_text('<html><body><a href="' + url + '">Kilde</a></body></html>', encoding='utf-8')
                render_biography(html, root / 'result.pdf', root / 'assets', 'biography-pdf')
                html.write_text('<html><body><img src="' + url + '" alt="foto"></body></html>', encoding='utf-8')
                with self.assertRaises(ProtocolError):
                    render_biography(html, root / 'unsafe.pdf', root / 'assets', 'biography-pdf')
                self.assertEqual(requests, [])
        finally:
            server.shutdown()
            server.server_close()
            thread.join()

    def test_fanget_ressourcefejl_afviser_pdf_og_sletter_output(self):
        from protocol import ProtocolError
        class FakeHtml:
            def __init__(self, *args, url_fetcher, **kwargs):
                self.fetch = url_fetcher
            def write_pdf(self, path):
                try:
                    self.fetch('https://example.org/billede.png')
                except ProtocolError:
                    pass
                Path(path).write_bytes(b'%PDF-ufuldstaendig')
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / 'assets').mkdir()
            html = root / 'input.html'
            html.write_text('<html><body><p>Tekst</p></body></html>', encoding='utf-8')
            output = root / 'result.pdf'
            with patch('weasyprint.HTML', FakeHtml), self.assertRaises(ProtocolError):
                render_biography(html, output, root / 'assets', 'biography-pdf')
            self.assertFalse(output.exists())

    def test_lang_url_ombrydes_uden_at_miste_tekst(self):
        from pypdf import PdfReader
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / 'assets').mkdir()
            url = 'https://example.org/' + 'a' * 300
            html = root / 'input.html'
            html.write_text('<!doctype html><html lang="da"><head><style>' + STYLE + '</style></head><body><p>Lang kilde: <a href="' + url + '">' + url + '</a></p></body></html>', encoding='utf-8')
            output = root / 'result.pdf'
            render_biography(html, output, root / 'assets', 'biography-pdf')
            pdf = PdfReader(output)
            text = ''.join(''.join(page.extract_text().split()) for page in pdf.pages)
            self.assertIn(url, text)
            links = [annotation.get_object().get('/A', {}).get('/URI') for page in pdf.pages for annotation in page.get('/Annots', [])]
            self.assertIn(url, links)

    def test_flere_sider_har_sidenumre(self):
        from pypdf import PdfReader
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / 'assets').mkdir()
            html = root / 'input.html'
            html.write_text('<!doctype html><html lang="da"><head><style>' + STYLE + '</style></head><body>' + ''.join('<p>En lang dansk biografi med oplysninger om familien.</p>' for _ in range(150)) + '</body></html>', encoding='utf-8')
            output = root / 'result.pdf'
            render_biography(html, output, root / 'assets', 'biography-pdf')
            pdf = PdfReader(output)
            self.assertGreater(len(pdf.pages), 1)
            for index, page in enumerate(pdf.pages, 1):
                self.assertIn(str(index), page.extract_text().splitlines())

    def test_pdf_med_dansk_tekst_billede_og_internt_link(self):
        from pypdf import PdfReader
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            assets = root / 'assets'
            assets.mkdir()
            data = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC')
            name = hashlib.sha256(data).hexdigest() + '.png'
            (assets / name).write_bytes(data)
            html = root / 'input.html'
            html.write_text('<!doctype html><html lang="da"><body><section id="person-000001"><h1>Åse Østergaard</h1><p><a href="#person-000001">Person</a> æøå</p><img src="assets/' + name + '" alt="foto" /></section></body></html>', encoding='utf-8')
            output = root / 'result.pdf'
            render_biography(html, output, assets, 'biography-pdf')
            pdf = PdfReader(output)
            self.assertEqual(len(pdf.pages), 1)
            self.assertIn('Åse Østergaard', pdf.pages[0].extract_text())
            self.assertAlmostEqual(float(pdf.pages[0].mediabox.width), 595.28, places=1)
            self.assertIn('person-000001', pdf.named_destinations)
            self.assertTrue(pdf.pages[0].images)
