#!/usr/bin/env python3
"""Pak en afprøvet macOS-værktøjsmappe med stabile arkivmetadata."""

import argparse
import gzip
from pathlib import Path
import tarfile


def main():
    parser = argparse.ArgumentParser(description='Pak en lokal eksportværktøjsmappe til offline-genskabelse.')
    parser.add_argument('--kilde', type=Path, required=True)
    parser.add_argument('--maal', type=Path, required=True)
    args = parser.parse_args()
    root = args.kilde.resolve(strict=True)
    for name in ('slaegt-export', 'pandoc', 'reference.docx', 'reference.odt', '_internal/fonts.conf', '_internal/libgmp.10.dylib'):
        if not (root / name).is_file():
            parser.error('Pakken mangler ' + name)
    if args.maal.exists():
        parser.error('Målfilen findes allerede.')
    with args.maal.open('xb') as destination:
        with gzip.GzipFile(filename='', mode='wb', fileobj=destination, mtime=0, compresslevel=6) as compressed:
            with tarfile.open(fileobj=compressed, mode='w', format=tarfile.PAX_FORMAT) as archive:
                paths = [root, *sorted(root.rglob('*'), key=lambda path: path.relative_to(root).as_posix())]
                for path in paths:
                    name = 'slaegt-export' if path == root else 'slaegt-export/' + path.relative_to(root).as_posix()
                    info = archive.gettarinfo(str(path), arcname=name)
                    info.uid = info.gid = 0
                    info.uname = info.gname = ''
                    info.mtime = 0
                    if info.isfile():
                        with path.open('rb') as source:
                            archive.addfile(info, source)
                    else:
                        archive.addfile(info)
    print('Værktøjsarkiv dannet:', args.maal)


if __name__ == '__main__':
    main()
