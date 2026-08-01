# Sprint 02: PoC-forfinelse & udvidet GEDCOM

## Formål
Forfine PoC'en fra sprint 1 med små, testdrevne features, som kan valideres individuelt før næste feature startes.

## Strukturkrav for sprinten
- Alle leverancer opdeles i separate features under `specs/sprints/sprint-02-poc-forfinelse/`.
- Hver feature skal have `Requirements.md`, `Plan.md` og `Validation.md`.
- Feature-rækkefølge følges som beskrevet i sprintens `Plan.md`.

## Fastlåste produktbeslutninger
- `Fil -> Gem` gemmer alle ændrede filer i den aktuelle session.
- Lukning med ugemte ændringer viser Gem / Kassér / Annullér.
- Preview starter i renderet webvisning, med mulighed for rå HTML.
- Nye GEDCOM-felter bruges i denne sprint til parser + UI-visning (ikke auto-indsættelse i biografi).
- UI-design og temaskift er ikke en del af Sprint 2; det leveres i Sprint 3.

## Ekstra forslag (skal implementeres)
1. Statusfelt med aktiv person/fil og gemmestatus.
2. Eksplicit mapping-tabel for GEDCOM-tags/sub-tags.
3. Målbare UI-kriterier for den efterfølgende UI-sprint.

## GEDCOM mapping-tabel (obligatorisk reference)
| Område | Primære tags | Underfelter i scope (første iteration) |
| --- | --- | --- |
| Kilder | `SOUR` | `TITL`, `AUTH`, `PUBL`, `TEXT`, `REPO`, `PAGE`, `DATA`, `DATE` |
| Medier | `OBJE` | `FILE`, `FORM`, `TITL`, `TYPE`, `NOTE` |
| Hændelser | `EVEN`, `BAPM`, `CHR`, `BURI` (samt eksisterende `BIRT`, `DEAT`) | `DATE`, `PLAC`, `TYPE`, `NOTE`, `SOUR` |
| Census | `CENS` | `DATE`, `PLAC`, `NOTE`, `SOUR` |

## Afgrænsning
- Sprinten afsluttes efter feature 7. Moderne desktop-layout, lyst/mørkt tema og ny informationsarkitektur ligger i Sprint 3.
- Grafisk slægtstræ og eksport ligger efter Sprint 4.
- Ingen cloud-afhængigheder; alt lokalt som i øvrige sprintkrav.
