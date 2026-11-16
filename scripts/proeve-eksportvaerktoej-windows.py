#!/usr/bin/env python3
"""Byg en lokal Windows-prøvepakke med private eksportværktøjer."""

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile


def copy_file(source, destination):
    with open(source, 'rb') as input_file, open(destination, 'wb') as output_file:
        shutil.copyfileobj(input_file, output_file)
    return str(destination)


def main():
    parser = argparse.ArgumentParser(description='Byg og afprøv en Windows-pakke med private biblioteker.')
    parser.add_argument('--python', type=Path, required=True, help='Python-miljø med WeasyPrint og PyInstaller')
    parser.add_argument('--biblioteker', type=Path, required=True, help='Mappe med alle private native DLL-filer')
    parser.add_argument('--bibliotekslicenser', type=Path, required=True, help='Mappe med DLL-komponenternes licenser og versionsoplysninger')
    parser.add_argument('--pandoc', type=Path, required=True, help='Privat Pandoc 3.13 til Windows x64')
    parser.add_argument('--pandoc-licenser', type=Path, required=True, help='Pandocs licenstekster')
    parser.add_argument('--skrifttype', type=Path, required=True, help='Lokal skrifttype med danske tegn')
    parser.add_argument('--skrifttypelicens', type=Path, required=True, help='Skrifttypens licens')
    parser.add_argument('--maal', type=Path, required=True, help='Ny mappe til prøvepakken')
    args = parser.parse_args()
    if sys.platform != 'win32':
        parser.error('Prøvepakken skal bygges og afprøves på Windows.')
    try:
        from pypdf import PdfReader
    except ImportError:
        parser.error('Byggemiljøet skal indeholde pypdf til kontrol af prøvefiler.')
    python = args.python.resolve(strict=True)
    libraries = args.biblioteker.resolve(strict=True)
    target = args.maal.absolute()
    required = ('libgobject-2.0-0.dll', 'libpango-1.0-0.dll', 'libpangoft2-1.0-0.dll', 'libharfbuzz-0.dll', 'libfontconfig-1.dll')
    if any(not (libraries / name).is_file() for name in required):
        parser.error('Mappen mangler en eller flere nødvendige native DLL-filer.')
    architecture = subprocess.run([str(python), '-c', 'import struct; print(struct.calcsize("P") * 8)'], capture_output=True, text=True, check=True).stdout.strip()
    if architecture != '64':
        parser.error('Python-miljøet skal være 64-bit.')
    if target.exists() or target.is_symlink():
        parser.error('Målmappen findes allerede; vælg en ny mappe.')
    for path in (args.pandoc, args.skrifttype, args.skrifttypelicens):
        if not path.is_file():
            parser.error('Pandoc, skrifttype eller skrifttypelicens mangler.')
    for path in (args.pandoc_licenser, args.bibliotekslicenser):
        if not path.is_dir() or not any(path.rglob('*')):
            parser.error('Licensmaterialet mangler.')
    if subprocess.run([str(args.pandoc), '--version'], capture_output=True, text=True, check=True).stdout.splitlines()[0] != 'pandoc 3.13':
        parser.error('Pakken kræver Pandoc 3.13.')

    source = Path(__file__).resolve().parents[1]
    with tempfile.TemporaryDirectory(prefix='slaegt-eksportpakke-') as temporary:
        root = Path(temporary)
        command = [str(python), '-m', 'PyInstaller', '--noconfirm', '--clean', '--onedir', '--name', 'slaegt-export', '--distpath', str(root / 'dist'), '--workpath', str(root / 'build'), '--specpath', str(root), '--collect-all', 'weasyprint']
        build_environment = os.environ.copy()
        build_environment['PATH'] = str(libraries) + os.pathsep + build_environment.get('PATH', '')
        build_environment['WEASYPRINT_DLL_DIRECTORIES'] = str(libraries)
        subprocess.run([*command, str(source / 'sidecar/export/export.py')], check=True, env=build_environment)
        package = root / 'dist/slaegt-export'
        internal = package / '_internal'
        for dll in libraries.glob('*.dll'):
            destination = internal / dll.name
            if destination.exists():
                def checksum(path):
                    with path.open('rb') as file:
                        return hashlib.file_digest(file, 'sha256').digest()
                if checksum(destination) != checksum(dll):
                    raise ValueError('Forskellige DLL-filer har samme navn: ' + dll.name)
            else:
                copy_file(dll, destination)
        copy_file(args.pandoc, package / 'pandoc.exe')
        for name in ('reference.docx', 'reference.odt'):
            copy_file(source / 'sidecar/export/reference' / name, package / name)
        shutil.copytree(args.pandoc_licenser, package / 'licenser/pandoc', copy_function=copy_file)
        shutil.copytree(args.bibliotekslicenser, package / 'licenser/native', copy_function=copy_file)
        subprocess.run([str(python), str(source / 'scripts/indsaml-pythonlicenser.py'), str(package / 'licenser/python')], check=True)
        fonts = internal / 'fonts'
        fonts.mkdir(exist_ok=True)
        copy_file(args.skrifttype, fonts / ('biografi' + args.skrifttype.suffix.lower()))
        copy_file(args.skrifttypelicens, fonts / 'licens.txt')
        config = internal / 'fonts.conf'
        config.write_text('<?xml version="1.0"?><!DOCTYPE fontconfig SYSTEM "fonts.dtd"><fontconfig><dir prefix="relative">fonts</dir><cachedir prefix="xdg">fontconfig</cachedir></fontconfig>', encoding='utf-8')
        job = root / 'job'
        job.mkdir()
        (job / 'assets').mkdir()
        image = base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEklEQVR4nGOsmLWHgYGBiQEMABWOAdJufWw5AAAAAElFTkSuQmCC')
        image_name = hashlib.sha256(image).hexdigest() + '.png'
        (job / 'assets' / image_name).write_bytes(image)
        html = '<!doctype html><html lang="da"><body><section class="chapter"><h1><span id="person-000001"></span>Åse Østergaard</h1><p>Dansk tekst æøå.</p><img src="assets/' + image_name + '" alt="Foto" /></section><section class="chapter"><h1>Bo</h1><p><a href="#person-000001">Åse</a></p></section></body></html>'
        (job / 'input.html').write_text(html, encoding='utf-8')
        environment = os.environ.copy()
        environment['FONTCONFIG_FILE'] = str(config)
        environment['PATH'] = str(internal) + os.pathsep + str(Path(os.environ['SystemRoot']) / 'System32')
        version = subprocess.run([str(package / 'slaegt-export.exe'), '--version'], capture_output=True, text=True, env=environment, timeout=30, check=True)
        if json.loads(version.stdout).get('protocolVersion') != 1:
            raise RuntimeError('Prøvepakken har en ugyldig protokolversion.')
        for extension in ('pdf', 'docx', 'odt'):
            output = job / ('result.' + extension)
            request = {'protocolVersion': 1, 'jobId': extension, 'operation': 'biography-' + extension,
                       'inputPath': str(job / 'input.html'), 'outputPath': str(output), 'assetRoot': str(job / 'assets'),
                       'options': {'paper': 'A4', 'orientation': 'portrait'}}
            request_path = job / 'request.json'
            request_path.write_text(json.dumps(request), encoding='utf-8')
            result = subprocess.run([str(package / 'slaegt-export.exe'), '--request', str(request_path)], capture_output=True, text=True, env=environment, timeout=120, check=True)
            if json.loads(result.stdout).get('status') != 'ok':
                raise RuntimeError('Prøvepakken kunne ikke danne ' + extension + '.')
            if extension == 'pdf':
                if not output.read_bytes().startswith(b'%PDF-'):
                    raise RuntimeError('Prøvepakken gav en ugyldig PDF.')
                pdf = PdfReader(output)
                if 'Åse Østergaard' not in ''.join(page.extract_text() for page in pdf.pages) or not any(page.images for page in pdf.pages):
                    raise RuntimeError('PDF-prøven mangler dansk tekst eller billede.')
            else:
                if not zipfile.is_zipfile(output):
                    raise RuntimeError('Prøvepakken gav et ugyldigt kontordokument.')
                with zipfile.ZipFile(output) as archive:
                    xml = archive.read('word/document.xml' if extension == 'docx' else 'content.xml').decode('utf-8')
                    prefix = 'word/media/' if extension == 'docx' else 'Pictures/'
                    if 'Åse Østergaard' not in xml or not any(name.startswith(prefix) for name in archive.namelist()):
                        raise RuntimeError('Kontorprøven mangler dansk tekst eller billede.')
        shutil.copytree(package, target, copy_function=copy_file)
    print('Foreløbig Windows-prøvepakke dannet og afprøvet:', target)
    print('Pakken kræver yderligere native, visuelle og distributionsmæssige prøver.')


if __name__ == '__main__':
    main()
