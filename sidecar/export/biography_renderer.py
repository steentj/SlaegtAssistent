import os
import io
import uuid
import zipfile
import xml.etree.ElementTree as ET
import base64
import hashlib
from html.parser import HTMLParser
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import urlsplit
from urllib.request import url2pathname

from protocol import ProtocolError

STYLE = '@page{size:A4;margin:20mm;@bottom-center{content:counter(page)}}body{font-size:11pt;line-height:1.35;overflow-wrap:anywhere}h1,h2,h3,h4,h5,h6{break-after:avoid}img{max-width:100%}thead{display:table-header-group}.chapter+.chapter{break-before:page}'
TAGS = {'html', 'head', 'meta', 'style', 'body', 'header', 'section', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'p', 'a', 'img', 'span', 'pre', 'code', 'blockquote', 'ul', 'ol', 'li', 'em', 'strong', 'del', 'sup', 'sub', 'table', 'thead', 'tbody', 'tr', 'th', 'td', 'hr', 'br', 'div'}
ATTRS = {'lang', 'charset', 'class', 'id', 'href', 'src', 'alt', 'title', 'start', 'colspan', 'rowspan', 'align', 'style'}
ASSET = re.compile(r'assets/[0-9a-f]{64}\.(?:png|jpg)\Z')
ASSET_ATTRIBUTE = re.compile(r'''\bsrc\s*=\s*(["'])(assets/[0-9a-f]{64}\.(?:png|jpg))\1''', re.IGNORECASE)


class Validator(HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.in_style = False
        self.style = ''

    def handle_starttag(self, tag, attrs):
        if tag not in TAGS or any(name not in ATTRS or value is None for name, value in attrs):
            raise ProtocolError('resource-blocked')
        if len({name for name, _ in attrs}) != len(attrs):
            raise ProtocolError('resource-blocked')
        for name, value in attrs:
            if name == 'style' and (tag not in ('th', 'td') or value not in ('text-align: left;', 'text-align: right;', 'text-align: center;')):
                raise ProtocolError('resource-blocked')
            if name == 'src' and (tag != 'img' or not ASSET.fullmatch(value)):
                raise ProtocolError('resource-blocked')
            if name == 'href' and (tag != 'a' or not (value.startswith('#') or urlsplit(value).scheme in ('http', 'https'))):
                raise ProtocolError('resource-blocked')
        if tag == 'style':
            if self.in_style or self.style:
                raise ProtocolError('resource-blocked')
            self.in_style = True

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        self.handle_endtag(tag)

    def handle_endtag(self, tag):
        if tag not in TAGS:
            raise ProtocolError('resource-blocked')
        if tag == 'style':
            self.in_style = False
            if self.style != STYLE:
                raise ProtocolError('resource-blocked')

    def handle_data(self, data):
        if self.in_style:
            self.style += data

    def handle_comment(self, data):
        raise ProtocolError('resource-blocked')

    def handle_decl(self, decl):
        if decl.lower() != 'doctype html':
            raise ProtocolError('resource-blocked')


def validate_html(content):
    parser = Validator()
    try:
        parser.feed(content)
        parser.close()
        if parser.in_style:
            raise ProtocolError('resource-blocked')
    except (ValueError, AssertionError):
        raise ProtocolError('invalid-input') from None
    return content


def inline_assets(content, asset_root, marker=None):
    def replace(match):
        name = match.group(2).split('/')[1]
        path = asset_root / name
        if path.is_symlink() or not path.is_file() or path.stat().st_size > 20 * 1024 * 1024:
            raise ProtocolError('resource-blocked')
        data = path.read_bytes()
        if len(data) > 20 * 1024 * 1024 or hashlib.sha256(data).hexdigest() != name[:64]:
            raise ProtocolError('resource-blocked')
        mime = 'image/png' if name.endswith('.png') else 'image/jpeg'
        return 'src="data:' + mime + ';base64,' + base64.b64encode(data).decode('ascii') + '"'

    class AssetInliner(HTMLParser):
        def __init__(self):
            super().__init__(convert_charrefs=False)
            self.parts = []
            self.chapters = 0

        def handle_starttag(self, tag, attrs):
            raw = self.get_starttag_text()
            if marker and tag == 'section' and ('class', 'chapter') in attrs:
                self.chapters += 1
                if self.chapters > 1:
                    self.parts.append('<p>' + marker + '</p>')
            self.parts.append(ASSET_ATTRIBUTE.sub(replace, raw) if tag == 'img' else raw)

        def handle_startendtag(self, tag, attrs):
            self.handle_starttag(tag, attrs)

        def handle_endtag(self, tag):
            self.parts.append('</' + tag + '>')

        def handle_data(self, data):
            self.parts.append(data)

        def handle_entityref(self, name):
            self.parts.append('&' + name + ';')

        def handle_charref(self, name):
            self.parts.append('&#' + name + ';')

        def handle_decl(self, decl):
            self.parts.append('<!' + decl + '>')

    parser = AssetInliner()
    parser.feed(content)
    parser.close()
    return ''.join(parser.parts)


def insert_office_page_breaks(output_path, extension, marker, expected):
    word = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
    office = 'urn:oasis:names:tc:opendocument:xmlns:office:1.0'
    text = 'urn:oasis:names:tc:opendocument:xmlns:text:1.0'
    style = 'urn:oasis:names:tc:opendocument:xmlns:style:1.0'
    fo = 'urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0'
    target = 'word/document.xml' if extension == 'docx' else 'content.xml'
    with zipfile.ZipFile(output_path) as source:
        entries = [(info, source.read(info.filename)) for info in source.infolist()]
    xml = next((data for info, data in entries if info.filename == target), None)
    if xml is None:
        raise ProtocolError('conversion-failed')
    try:
        root = ET.fromstring(xml)
        count = 0
        for parent in root.iter():
            for child in list(parent):
                if extension == 'docx' and child.tag == '{%s}p' % word:
                    paragraphs = ''.join(node.text or '' for node in child.iter('{%s}t' % word))
                elif extension == 'odt' and child.tag == '{%s}p' % text:
                    paragraphs = ''.join(child.itertext())
                else:
                    continue
                if paragraphs != marker:
                    continue
                count += 1
                for node in list(child):
                    child.remove(node)
                child.text = None
                if extension == 'docx':
                    run = ET.SubElement(child, '{%s}r' % word)
                    ET.SubElement(run, '{%s}br' % word, {'{%s}type' % word: 'page'})
                else:
                    child.set('{%s}style-name' % text, 'SlaegtsAssistentSideskift')
        if count != expected:
            raise ProtocolError('conversion-failed')
        if count:
            if extension == 'odt':
                styles = root.find('{%s}automatic-styles' % office)
                if styles is None:
                    raise ProtocolError('conversion-failed')
                entry = ET.SubElement(styles, '{%s}style' % style, {'{%s}name' % style: 'SlaegtsAssistentSideskift', '{%s}family' % style: 'paragraph'})
                ET.SubElement(entry, '{%s}paragraph-properties' % style, {'{%s}break-before' % fo: 'page'})
            data = ET.tostring(root, encoding='utf-8', xml_declaration=True)
            with zipfile.ZipFile(output_path, 'w') as dest:
                for info, original in entries:
                    dest.writestr(info, data if info.filename == target else original)
    except (ET.ParseError, ValueError, KeyError, zipfile.BadZipFile):
        raise ProtocolError('conversion-failed') from None


def render_biography(input_path, output_path, asset_root, operation):
    content = validate_html(input_path.read_text(encoding='utf-8'))
    if operation == 'biography-pdf':
        inline_assets(content, asset_root)
        try:
            from weasyprint import HTML
        except (ImportError, OSError):
            raise ProtocolError('missing-tool') from None

        blocked_resource = False

        def fetch(url, *args, **kwargs):
            nonlocal blocked_resource
            try:
                parsed = urlsplit(url)
                path = Path(url2pathname(parsed.path))
                if parsed.scheme != 'file' or parsed.netloc or parsed.query or parsed.fragment or path.parent != asset_root or path.is_symlink() or not path.is_file() or not ASSET.fullmatch('assets/' + path.name):
                    raise ProtocolError('resource-blocked')
                return {'file_obj': path.open('rb')}
            except (OSError, ValueError, ProtocolError):
                blocked_resource = True
                raise ProtocolError('resource-blocked') from None

        HTML(string=content, base_url=input_path.parent.as_uri() + '/', url_fetcher=fetch).write_pdf(str(output_path))
        if blocked_resource:
            output_path.unlink(missing_ok=True)
            raise ProtocolError('resource-blocked')
        return
    # Kun den medleverede Pandoc og de medleverede referencefiler må bruges.
    tool_root = Path(sys.executable).resolve().parent
    pandoc = tool_root / ('pandoc.exe' if os.name == 'nt' else 'pandoc')
    extension = 'docx' if operation == 'biography-docx' else 'odt'
    reference = tool_root / ('reference.' + extension)
    if not pandoc.is_file() or not reference.is_file():
        raise ProtocolError('missing-tool')
    marker = 'SLAEGTSASSISTENT_SIDESKIFT_' + uuid.uuid4().hex
    content = inline_assets(content, asset_root, marker)
    try:
        completed = subprocess.run([str(pandoc), '--from=html', '--to=' + extension, '--sandbox', '--resource-path=' + str(input_path.parent), '--reference-doc=' + str(reference), '--output=' + str(output_path)], input=content, text=True, cwd=input_path.parent, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120, check=False)
    except subprocess.TimeoutExpired:
        raise ProtocolError('timeout') from None
    if completed.returncode != 0:
        raise ProtocolError('conversion-failed')
    insert_office_page_breaks(output_path, extension, marker, content.count(marker))
