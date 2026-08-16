# Validation: Privat og kontrolleret Markdown-preview

- [x] Eksterne billeder og øvrige eksterne ressourcer laver ingen netværksanmodninger i den genererede previewpipeline.
- [x] Script, iframe og aktive URL-skemaer udføres ikke.
- [x] Rå HTML er deaktiveret som dokumenteret i `Previewpolitik.md`.
- [x] Tilladte lokale billeder vises korrekt.
- [x] Lokale stiudbrud og ikke-tilladte filer blokeres.
- [x] Eksterne links kræver eksplicit brugerhandling og viser destinationen.
- [x] Blokering ændrer ikke Markdown-dokumentet.
- [x] Hovedpreview og hjælpevinduer bruger samme policy.
- [ ] Alle tre temaer er manuelt kontrolleret.
- [x] `dotnet build` og `dotnet test` er grønne.
- [ ] Manuel netværksmonitorering viser nul uventede anmodninger.

## Automatiseret validering

- `SafeMarkdownPreviewServiceTests` dækker fjernressourcer, rå HTML, aktive URL-skemaer, CSP, lokal billedindlejring, almindelige og symbolske stiudbrud, linkpolicy og fælles policy for hoved- og hjælpepreview.
- `EditorViewModelTests` verificerer, at en tilladt lokal billedfil indlejres uden at ændre Markdown eller vise en sikkerhedsadvarsel.
- Målrettet preview- og editortest: 24 bestået, 0 fejlet.
- Fuld testpakke den 13. august 2026: 106 Core-tests og 115 App-tests bestået, 0 fejlet.
- `dotnet build --no-restore` den 13. august 2026: 0 advarsler og 0 fejl.

## Manuel testprocedure

1. Åbn et dokument med eksternt billede, rå `script`, `iframe`, stylesheet, skrifttype samt `javascript:`, `data:`, `file:` og `https:`-links.
2. Kontrollér med en netværksmonitor, at previewet ikke foretager en udgående anmodning.
3. Kontrollér den danske forklaring, og bekræft at Markdown-filen er byte-for-byte uændret før gemning.
4. Kontrollér et PNG- eller JPEG-billede både ved siden af Markdown-filen og i en tilladt output- eller GEDCOM-mappe.
5. Kontrollér at `../`-stiudbrud, SVG, manglende billeder og filer over 20 MB blokeres.
6. Klik på et `https:`-link. Kontrollér at WebView'en ikke navigerer, at hele destinationen vises, og at den kan kopieres eksplicit.
7. Kontrollér at aktive og ikke-understøttede linktyper ikke reagerer.
8. Gentag sikkerhedskontrollen i begge hjælpevinduer.
9. Gentag den visuelle kontrol med lyst, mørkt og systemstyret tema.

## Manuel godkendelse

- **Dato:** 13. august 2026
- **Godkendt af:** Produktejer
- **Build eller commit:** Lokal arbejdsgren; 106 Core-tests og 115 App-tests bestået
- **Bemærkninger:** Manuel godkendelse modtaget efter gennemgang af Feature 4.8.10.

- [x] Feature 10 er godkendt, og feature 11 må påbegyndes.
