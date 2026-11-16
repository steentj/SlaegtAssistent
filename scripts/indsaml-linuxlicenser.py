#!/usr/bin/env python3
"""Indsaml versionsoplysninger og tilgængelige copyrightfiler fra Debian/Ubuntu."""

import argparse
import json
from pathlib import Path
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser(description='Indsaml lokale systempakkers licensoplysninger.')
    parser.add_argument('maal', type=Path)
    args = parser.parse_args()
    args.maal.mkdir(parents=True, exist_ok=True)
    rows = subprocess.run(['dpkg-query', '-W', '-f=${binary:Package}\t${Version}\n'], check=True, text=True, capture_output=True).stdout.splitlines()
    entries = []
    for row in rows:
        package, version = row.split('\t', 1)
        name = package.split(':', 1)[0]
        source = Path('/usr/share/doc') / name / 'copyright'
        found = source.is_file()
        if found:
            destination = args.maal / name / 'copyright'
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
        entries.append({'pakke': package, 'version': version, 'licensfil': found})
    (args.maal / 'systempakker.json').write_text(json.dumps(entries, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
