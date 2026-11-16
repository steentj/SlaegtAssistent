import json
import base64
import hashlib
import shutil
import subprocess
import zipfile
import re
import os
import xml.etree.ElementTree as ET
from unittest.mock import patch
from pathlib import Path
import sys
import tempfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from biography_renderer import validate_html, render_biography
from protocol import ProtocolError, read_request


class BiographyTests(unittest.TestCase):
    def test_godkendt_html_og_aktive_ressourcer(self):
        validate_html('<table><tr><th style="text-align: right;">År</th></tr></table>')
        validate_html('<!doctype html><html lang="da"><body><p>Æøå <a href="https://example.com">kilde</a></p><img src="assets/' + 'a' * 64 + '.png" alt="foto"></body></html>')
        for html in ('<script>hej</script>', '<img src="https://example.com/a.png">', '<a href="file:///tmp/a">tekst</a>', '<p style="background:url(http://example.com)">tekst</p>', '<style>@import "https://example.com";</style>'):
            with self.subTest(html=html), self.assertRaises(ProtocolError):
                validate_html(html)

    @unittest.skipUnless(shutil.which('pandoc'), 'Kræver lokal Pandoc til udviklingsprøven')
    def test_pandoc_sandbox_med_billede_og_bogmaerke(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            assets = root / 'assets'
            assets.mkdir()
            data = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC')
            name = hashlib.sha256(data).hexdigest() + '.png'
            (assets / name).write_bytes(data)
            (root / 'pandoc').symlink_to(shutil.which('pandoc'))
            executable = root / 'sidecar'
            executable.touch()
            html = root / 'input.html'
            html.write_text('<html><body><h1><span id="person-000001"></span>Åse</h1><a href="#person-000001">Åse</a><img src="assets/' + name + '" alt="foto"></body></html>', encoding='utf-8')
            for extension in ('docx', 'odt'):
                reference = root / ('reference.' + extension)
                shutil.copy2(Path(__file__).resolve().parents[1] / 'reference' / ('reference.' + extension), reference)
                output = root / ('result.' + extension)
                with patch('biography_renderer.sys.executable', str(executable)):
                    render_biography(html, output, assets, 'biography-' + extension)
                with zipfile.ZipFile(output) as archive:
                    names = archive.namelist()
                    content = archive.read('word/document.xml' if extension == 'docx' else 'content.xml').decode('utf-8')
                    if extension == 'docx':
                        bookmarks = re.findall(r'w:bookmarkStart[^>]*w:name="([^"]+)"', content)
                        anchor = re.search(r'w:hyperlink w:anchor="([^"]+)"', content)
                        self.assertIsNotNone(anchor)
                        self.assertIn(anchor.group(1), bookmarks)
                    else:
                        self.assertIn('text:bookmark text:name="person-000001"', content)
                        self.assertIn('xlink:href="#person-000001"', content)
                    if extension == 'docx':
                        word = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
                        document = ET.fromstring(content)
                        self.assertEqual(document.find('.//' + word + 'pgSz').get(word + 'w'), '11906')
                        self.assertEqual(document.find('.//' + word + 'pgMar').get(word + 'left'), '1134')
                    else:
                        style = '{urn:oasis:names:tc:opendocument:xmlns:style:1.0}'
                        fo = '{urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0}'
                        styles = ET.fromstring(archive.read('styles.xml'))
                        page = styles.find('.//' + style + 'page-layout-properties')
                        self.assertEqual(page.get(fo + 'page-width'), '21cm')
                        self.assertEqual(page.get(fo + 'margin-left'), '2cm')
                    self.assertIn('Åse', content)
                    self.assertTrue(any(part.startswith('word/media/' if extension == 'docx' else 'Pictures/') for part in names))

    @unittest.skipUnless(shutil.which('pandoc'), 'Kræver lokal Pandoc til udviklingsprøven')
    def test_kapitler_faar_sideskift_i_begge_kontorformater(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            assets = root / 'assets'
            assets.mkdir()
            (root / 'pandoc').symlink_to(shutil.which('pandoc'))
            executable = root / 'sidecar'
            executable.touch()
            html = root / 'input.html'
            html.write_text('<html><body><section class="chapter"><h1>Anna</h1></section><section class="chapter"><h1>Bo</h1></section></body></html>', encoding='utf-8')
            for extension in ('docx', 'odt'):
                shutil.copy2(Path(__file__).resolve().parents[1] / 'reference' / ('reference.' + extension), root / ('reference.' + extension))
                output = root / ('result.' + extension)
                with patch('biography_renderer.sys.executable', str(executable)):
                    render_biography(html, output, assets, 'biography-' + extension)
                with zipfile.ZipFile(output) as archive:
                    raw = archive.read('word/document.xml' if extension == 'docx' else 'content.xml')
                    self.assertNotIn(b'SLAEGTSASSISTENT_SIDESKIFT_', raw)
                    tree = ET.fromstring(raw)
                    if extension == 'docx':
                        word = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
                        self.assertEqual(len([node for node in tree.iter(word + 'br') if node.get(word + 'type') == 'page']), 1)
                    else:
                        fo = '{urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0}'
                        self.assertEqual(len([node for node in tree.iter() if node.get(fo + 'break-before') == 'page']), 1)

    def test_medleverede_referencer_har_dansk_a4_layout(self):
        root = Path(__file__).resolve().parents[1] / 'reference'
        word = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
        style = '{urn:oasis:names:tc:opendocument:xmlns:style:1.0}'
        fo = '{urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0}'
        with zipfile.ZipFile(root / 'reference.docx') as archive:
            document = ET.fromstring(archive.read('word/document.xml'))
            page = document.find('.//' + word + 'pgSz')
            margin = document.find('.//' + word + 'pgMar')
            self.assertEqual((page.get(word + 'w'), page.get(word + 'h')), ('11906', '16838'))
            self.assertEqual(margin.get(word + 'left'), '1134')
            styles = ET.fromstring(archive.read('word/styles.xml'))
            defaults = styles.find(word + 'docDefaults')
            self.assertEqual(defaults.find('.//' + word + 'sz').get(word + 'val'), '22')
            self.assertEqual(defaults.find('.//' + word + 'lang').get(word + 'val'), 'da-DK')
        with zipfile.ZipFile(root / 'reference.odt') as archive:
            styles = ET.fromstring(archive.read('styles.xml'))
            page = styles.find('.//' + style + 'page-layout-properties')
            self.assertEqual((page.get(fo + 'page-width'), page.get(fo + 'page-height')), ('21cm', '29.7cm'))
            self.assertEqual(page.get(fo + 'margin-left'), '2cm')
            defaults = next(item for item in styles.iter(style + 'default-style') if item.get(style + 'family') == 'paragraph')
            self.assertEqual(defaults.find(style + 'text-properties').get(fo + 'font-size'), '11pt')
            self.assertEqual(defaults.find(style + 'paragraph-properties').get(fo + 'line-height'), '135%')

    @unittest.skipUnless(os.environ.get('SLAEGTSASSISTENT_KONTOR_INTEGRATION') == '1' and shutil.which('pandoc'), 'Kræver lokal Pandoc, LibreOffice og pypdf')
    def test_fodnotetekst_vises_i_libreoffice(self):
        from pypdf import PdfReader
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / 'assets').mkdir()
            (root / 'pandoc').symlink_to(shutil.which('pandoc'))
            (root / 'sidecar').touch()
            shutil.copy2(Path(__file__).resolve().parents[1] / 'reference/reference.odt', root / 'reference.odt')
            html = root / 'input.html'
            html.write_text('<html><body><section class="chapter"><h1>Åse</h1><p>Note<a href="#person-000001-fn-1"><sup>1</sup></a>.</p><div class="footnotes"><hr /><ol><li><span id="person-000001-fn-1"></span><p>Kirkebogen for København.</p></li></ol></div></section></body></html>', encoding='utf-8')
            output = root / 'result.odt'
            with patch('biography_renderer.sys.executable', str(root / 'sidecar')):
                render_biography(html, output, root / 'assets', 'biography-odt')
            subprocess.run([os.environ['SLAEGTSASSISTENT_SOFFICE'], '-env:UserInstallation=' + (root / 'profil').as_uri(), '--headless', '--convert-to', 'pdf', '--outdir', str(root), str(output)], capture_output=True, check=True, timeout=60)
            pdf = PdfReader(root / 'result.pdf')
            self.assertIn('Kirkebogen for København.', ''.join(page.extract_text() for page in pdf.pages))

    def test_protokol_kraever_a4_og_korrekt_endelse(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory).resolve()
            (root / 'assets').mkdir()
            (root / 'input.html').write_text('<html></html>', encoding='utf-8')
            request = {'protocolVersion': 1, 'jobId': 'abc', 'operation': 'biography-docx', 'inputPath': str(root / 'input.html'), 'outputPath': str(root / 'result.docx'), 'assetRoot': str(root / 'assets'), 'options': {'paper': 'A4', 'orientation': 'portrait'}}
            path = root / 'request.json'
            path.write_text(json.dumps(request), encoding='utf-8')
            self.assertEqual(read_request(str(path))[0]['operation'], 'biography-docx')
            request['outputPath'] = str(root / 'result.pdf')
            path.write_text(json.dumps(request), encoding='utf-8')
            with self.assertRaises(ProtocolError):
                read_request(str(path))
