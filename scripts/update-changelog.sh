#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "Not a git repository: $repo_root" >&2
  exit 1
fi

changelog="$repo_root/CHANGELOG.md"

if [[ ! -f "$changelog" ]]; then
  printf "# Changelog\n\n" > "$changelog"
fi

last_hash="$(grep -Eo '\[[0-9a-f]{7,40}\]' "$changelog" | tail -n1 | tr -d '[]' || true)"
last_heading_date="$(grep -E '^## [0-9]{4}-[0-9]{2}-[0-9]{2}$' "$changelog" | tail -n1 | awk '{print $2}' || true)"

if [[ -n "$last_hash" ]] && git rev-parse --verify "${last_hash}^{commit}" >/dev/null 2>&1; then
  commit_stream="$(git --no-pager log "${last_hash}..HEAD" --date=short --pretty=format:'%ad|%h|%s' --reverse)"
else
  commit_stream="$(git --no-pager log --date=short --pretty=format:'%ad|%h|%s' --reverse)"
fi

if [[ -z "$commit_stream" ]]; then
  echo "CHANGELOG is already up to date."
  exit 0
fi

if [[ -n "$(tail -c1 "$changelog")" ]]; then
  printf "\n" >> "$changelog"
fi

current_date=""
first_entry=true

while IFS='|' read -r commit_date short_hash subject; do
  [[ -z "$commit_date" ]] && continue

  if [[ "$current_date" != "$commit_date" ]]; then
    if [[ "$first_entry" == true && -n "$last_heading_date" && "$last_heading_date" == "$commit_date" ]]; then
      :
    else
      printf "\n## %s\n" "$commit_date" >> "$changelog"
    fi
    current_date="$commit_date"
  fi

  printf -- "- [%s] %s\n" "$short_hash" "$subject" >> "$changelog"
  first_entry=false
done <<< "$commit_stream"

echo "Updated $changelog"
