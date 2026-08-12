# Requirements: Atomisk dokumentlagring og gendannelse

- Alle overskrivninger af Markdown-dokumenter, indstillinger, manifest og andre brugerbærende tekstfiler skal ske gennem en fælles sikker lagringsabstraktion.
- En overskrivning skal bruge en midlertidig fil i samme mappe og en atomisk erstatning, hvor platformen understøtter det.
- En fejl før commit skal efterlade den tidligere fil byte-for-byte uændret.
- En fejl efter commit må ikke rapporteres som om den gamle version stadig er aktiv.
- Midlertidige filer skal ryddes sikkert op uden at slette en mulig recovery-kopi.
- En eksisterende fil må aldrig trunkeres direkte.
- Editorens dirty-state må kun nulstilles efter dokumenteret succesfuld commit.
- Fejl skal vises på dansk med filsti og en sikker anbefaling til brugeren.
- Implementationen skal have deterministisk fejlinjektion til tests ved oprettelse, flush og erstatning.
