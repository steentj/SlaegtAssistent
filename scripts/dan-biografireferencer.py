#!/usr/bin/env python3
"""Dan referencestile fra en lokalt installeret Pandoc 3.13."""

import argparse
import io
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import zipfile

W = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
S = 'urn:oasis:names:tc:opendocument:xmlns:style:1.0'
FO = 'urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0'


def tag(namespace, name):
    return '{%s}%s' % (namespace, name)


def parse(data):
    for _, (prefix, uri) in ET.iterparse(io.BytesIO(data), events=['start-ns']):
        if not prefix.startswith('ns'):
            ET.register_namespace(prefix, uri)
    return ET.fromstring(data)


def child(parent, namespace, name):
    result = parent.find(tag(namespace, name))
    return result if result is not None else ET.SubElement(parent, tag(namespace, name))


def set_values(element, namespace, values):
    for name, value in values.items():
        element.set(tag(namespace, name), value)


def docx_document(data):
    root = parse(data)
    section = root.find('.//' + tag(W, 'sectPr'))
    if section is None:
        raise ValueError('DOCX-referencen mangler sektionsindstillinger.')
    size = child(section, W, 'pgSz')
    set_values(size, W, {'w': '11906', 'h': '16838'})
    margin = child(section, W, 'pgMar')
    set_values(margin, W, {'top': '1134', 'right': '1134', 'bottom': '1134', 'left': '1134', 'header': '567', 'footer': '567', 'gutter': '0'})
    return ET.tostring(root, encoding='utf-8', xml_declaration=True)


def docx_styles(data):
    root = parse(data)
    defaults = root.find(tag(W, 'docDefaults'))
    if defaults is None:
        raise ValueError('DOCX-referencen mangler standardtypografi.')
    run = child(child(defaults, W, 'rPrDefault'), W, 'rPr')
    set_values(child(run, W, 'sz'), W, {'val': '22'})
    set_values(child(run, W, 'szCs'), W, {'val': '22'})
    set_values(child(run, W, 'lang'), W, {'val': 'da-DK'})
    paragraph = child(child(defaults, W, 'pPrDefault'), W, 'pPr')
    set_values(child(paragraph, W, 'spacing'), W, {'line': '324', 'lineRule': 'auto'})
    for entry in root.findall(tag(W, 'style')):
        if entry.get(tag(W, 'styleId')) in {'Heading1', 'Heading2', 'Heading3', 'Heading4', 'Heading5', 'Heading6'}:
            child(child(entry, W, 'pPr'), W, 'keepNext')
    return ET.tostring(root, encoding='utf-8', xml_declaration=True)


def odt_styles(data):
    root = parse(data)
    layouts = root.findall('.//' + tag(S, 'page-layout-properties'))
    if not layouts:
        raise ValueError('ODT-referencen mangler sidelayout.')
    for layout in layouts:
        set_values(layout, FO, {'page-width': '21cm', 'page-height': '29.7cm', 'margin-top': '2cm', 'margin-bottom': '2cm', 'margin-left': '2cm', 'margin-right': '2cm'})
        set_values(layout, S, {'print-orientation': 'portrait'})
    defaults = [entry for entry in root.findall('.//' + tag(S, 'default-style')) if entry.get(tag(S, 'family')) == 'paragraph']
    if len(defaults) != 1:
        raise ValueError('ODT-referencen mangler standardtypografi.')
    set_values(child(defaults[0], S, 'text-properties'), FO, {'font-size': '11pt', 'language': 'da', 'country': 'DK'})
    set_values(child(defaults[0], S, 'paragraph-properties'), FO, {'line-height': '135%'})
    for entry in root.findall('.//' + tag(S, 'style')):
        if entry.get(tag(S, 'name'), '').startswith('Heading_20_'):
            set_values(child(entry, S, 'paragraph-properties'), FO, {'keep-with-next': 'always'})
    return ET.tostring(root, encoding='utf-8', xml_declaration=True)


def build(pandoc, extension, target):
    result = subprocess.run([pandoc, '--print-default-data-file', 'reference.' + extension], capture_output=True, check=True)
    transforms = {'word/document.xml': docx_document, 'word/styles.xml': docx_styles} if extension == 'docx' else {'styles.xml': odt_styles}
    with zipfile.ZipFile(io.BytesIO(result.stdout)) as source, zipfile.ZipFile(target, 'w') as destination:
        names = set(source.namelist())
        if not set(transforms).issubset(names):
            raise ValueError('Pandocs reference mangler forventede filer.')
        for entry in source.infolist():
            data = source.read(entry.filename)
            entry.date_time = (1980, 1, 1, 0, 0, 0)
            destination.writestr(entry, transforms[entry.filename](data) if entry.filename in transforms else data)


def main():
    parser = argparse.ArgumentParser(description='Dan lokale referencestile til biografieksport.')
    parser.add_argument('--pandoc', default='pandoc', help='Sti til Pandoc 3.13')
    parser.add_argument('--maal', type=Path, default=Path(__file__).resolve().parents[1] / 'sidecar/export/reference')
    args = parser.parse_args()
    version = subprocess.run([args.pandoc, '--version'], capture_output=True, text=True, check=True).stdout.splitlines()[0]
    if version != 'pandoc 3.13':
        parser.error('Referencerne kræver Pandoc 3.13; fundet: ' + version)
    args.maal.mkdir(parents=True, exist_ok=True)
    for extension in ('docx', 'odt'):
        build(args.pandoc, extension, args.maal / ('reference.' + extension))
    print('Referencestile dannet med ' + version + '.')


if __name__ == '__main__':
    main()
