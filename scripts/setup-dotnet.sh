#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
if ! command -v dotnet >/dev/null; then
    echo "En .NET 10-vært skal være installeret og tilgængelig på PATH før opsætningen." >&2
    exit 1
fi

installer="$(mktemp)"
trap 'rm -f "$installer"' EXIT

echo "Installerer projektets .NET SDK i .dotnet..."
curl --fail --silent --show-error --location https://dot.net/v1/dotnet-install.sh --output "$installer"
bash "$installer" --jsonfile "$root/global.json" --install-dir "$root/.dotnet" --no-path

cd "$root"
echo "Installeret SDK:"
dotnet --version
