#!/usr/bin/env python3
"""Pak en lokalt bygget Graphviz med private macOS-biblioteker."""

import argparse
import os
from pathlib import Path
import shutil
import subprocess


def dependencies(path):
    lines = subprocess.check_output(["otool", "-L", str(path)], text=True).splitlines()[1:]
    return [line.strip().split(" ", 1)[0] for line in lines]


def main():
    parser = argparse.ArgumentParser(description="Pak Graphviz til lokal brug på macOS.")
    parser.add_argument("executable", type=Path, help="Sti til den GTS-aktiverede Graphviz-eksekverbare fil")
    parser.add_argument("destination", type=Path, help="Mappe til den private værktøjspakke")
    args = parser.parse_args()
    source = args.executable.resolve(strict=True)
    destination = args.destination.resolve()
    destination.mkdir(parents=True, exist_ok=True)
    library_dir = destination / "lib"
    library_dir.mkdir(exist_ok=True)
    executable = destination / "sfdp"
    shutil.copy2(source, executable)
    os.chmod(executable, 0o755)
    pending = [(source, executable)]
    copied = {}
    while pending:
        original, target = pending.pop()
        entries = dependencies(original)
        is_library = original.suffix == ".dylib"
        if is_library:
            subprocess.run(["install_name_tool", "-id", "@loader_path/" + target.name, str(target)], check=True)
        for dependency in entries[1 if is_library else 0:]:
            if dependency.startswith(("/usr/lib/", "/System/Library/")):
                continue
            if dependency.startswith("@rpath/"):
                resolved_dependency = original.parent / dependency.removeprefix("@rpath/")
            elif dependency.startswith("@loader_path/"):
                resolved_dependency = original.parent / dependency.removeprefix("@loader_path/")
            elif dependency.startswith("/"):
                resolved_dependency = Path(dependency)
            else:
                raise RuntimeError(f"Afhængigheden kan ikke pakkes automatisk: {dependency}")
            source_library = resolved_dependency.resolve(strict=True)
            name = Path(dependency).name
            if name in copied and copied[name] != source_library:
                raise RuntimeError(f"Flere biblioteker har samme navn: {name}")
            target_library = library_dir / name
            if name not in copied:
                copied[name] = source_library
                shutil.copy2(source_library, target_library)
                os.chmod(target_library, 0o755)
                pending.append((source_library, target_library))
            relative = ("@loader_path/" if is_library else "@loader_path/lib/") + name
            subprocess.run(["install_name_tool", "-change", dependency, relative, str(target)], check=True)
    for library in library_dir.iterdir():
        subprocess.run(["codesign", "--force", "--sign", "-", str(library)], check=True, capture_output=True)
    subprocess.run(["codesign", "--force", "--sign", "-", str(executable)], check=True, capture_output=True)
    probe = subprocess.run([str(executable), "-Tplain"], input='graph { graph [overlap=prism]; a -- b; }', text=True, capture_output=True, check=True)
    if "Error:" in probe.stderr or "unsupported" in probe.stderr.lower():
        raise RuntimeError("Den pakkede Graphviz-build mangler GTS-understøttelse: " + probe.stderr)
    print(f"Pakket og afprøvet: {executable}; {len(copied)} private biblioteker.")


if __name__ == "__main__":
    main()
