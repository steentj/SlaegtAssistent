#!/usr/bin/env python3
"""Indsaml metadata og licensfiler fra et lokalt Python-miljø."""

import argparse
from importlib import metadata
import json
from pathlib import Path
import shutil


def main():
    parser = argparse.ArgumentParser(description='Indsaml licenser fra det aktuelle Python-miljø.')
    parser.add_argument('maal', type=Path)
    args = parser.parse_args()
    args.maal.mkdir(parents=True, exist_ok=True)
    report = []
    for package in sorted(metadata.distributions(), key=lambda item: item.metadata.get('Name', '').lower()):
        name = package.metadata.get('Name')
        if not name:
            raise ValueError('En Python-pakke mangler navn.')
        files = []
        for path in package.files or []:
            value = str(path)
            if '.dist-info/' not in value:
                continue
            relative = value.split('.dist-info/', 1)[1]
            if not relative.lower().startswith(('licenses/', 'license', 'copying', 'sboms/')):
                continue
            source = package.locate_file(path)
            if source.is_file():
                target = args.maal / name / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                with source.open('rb') as input_file, target.open('wb') as output_file:
                    shutil.copyfileobj(input_file, output_file)
                files.append(relative)
        if not files:
            raise ValueError('Python-pakken mangler en identificeret licensfil: ' + name)
        report.append({'navn': name, 'version': package.version,
                       'licens': package.metadata.get('License-Expression') or package.metadata.get('License') or '',
                       'filer': files})
    (args.maal / 'pakker.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
