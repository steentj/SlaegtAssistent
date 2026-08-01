# Requirements: Sprint 04 – dokumenter før GEDCOM og synkronisering

- Dokumenter fra standard Markdown-mappen indlæses ved appstart før GEDCOM.
- Nye dokumenter indeholder versionsstyret YAML-frontmatter med stabilt `recordId`.
- Eksisterende standardnavngivne filer kan genkendes og migreres ikke-destruktivt.
- Ukendte eller tvetydige filer må ikke automatisk knyttes til en person.
- GEDCOM-forskelle vises per felt med dokumentværdi og GEDCOM-værdi.
- Brugeren vælger per felt, om GEDCOM-værdien skal anvendes.
- Fri Markdown-biografi og AI-tekst må aldrig overskrives af synkronisering.
