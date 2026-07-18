---
name: changelog-by-date
description: Keep CHANGELOG.md in the project root with date headings and commit-based bullet entries. Bootstrap from git history when the changelog does not exist.
---

# Changelog by Date

Use this skill right before merging, or whenever the user asks to update the changelog.

## Expected changelog format

```markdown
# Changelog

## YYYY-MM-DD
- [abc1234] Commit subject
```

## Instructions

1. Run `bash ./scripts/update-changelog.sh` from the repository root.
2. Open `CHANGELOG.md` and confirm new entries were added under date headings.
3. Keep existing historical entries intact unless the user explicitly asks to rewrite them.
4. If the user asks for cleaner wording, edit only the newest bullet text while preserving date headings and commit hash references.
