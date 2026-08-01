# Requirements: Stabil dokumentidentitet og migrering

- Frontmatter skal mindst indeholde `formatVersion`, `recordId`, `displayName` og strukturerede fakta.
- Frontmatter skal kunne parses deterministisk.
- Legacy-match må aldrig overskrive filen uden brugerens valg.
- Ugyldig frontmatter skal vises som en fejltilstand, ikke give et falsk match.
