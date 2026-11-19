# Validering af filformater og udskrift

Status: Planlagt.

## Automatiske prøver

- Core og App med xUnit og FluentAssertions. Test papirvalg, fast liggende orientering, samme PDF-grundlag til gemning og udskrift, filnavn, annullering, skrivefejl og manglende PDF-fremviser. Kontrollér, at en eksisterende eksportfil, en biografi og dens markering af ugemte ændringer bevares ved fejl og annullering.
- Python-protokolprøver. Godkend hver træoperation med korrekt inputtype og filendelse. Afvis stående papir, forkert indhold, mere end seks rækker, ressourcehenvisninger og ugyldige stier. Biografi og personnetværk testes med hidtidige options og formater.
- Integration med WeasyPrint og Pandoc. Prøv liggende A4 og A3 og tabeller med 1, 2, 4, 5, 6, 8, 9 og 32 celler. Brug lange navne og steder, tegnene æ/ø/å, punktummer, anførselstegn og `<>&`. Kontrollér, at tekst forbliver tekst, at personfelter findes på siden og i ODT/DOCX, at kompakte rækker bruger år og at XML-egenskaber for rotation kun findes på rækker fra ni celler. Kontrollér PDF-sidetal og sidemål. Kør både almindelig produktion og et tilfælde, der ikke kan rummes selv på A3.
- Kør `dotnet test`, `python3 -m unittest discover -s sidecar/export/tests` og røgtest med pakkede værktøjer ved udgivelse. Rapportér eventuelt oversprungne integrationer som uafprøvede.

## Native prøver

- Åbn DOCX i Word og ODT i LibreOffice. Brug visning med sidegrænser eller eksportér fra kontorprogrammet til midlertidig PDF for at tælle sider. Kontrollér én side, liggende mål, rotation, placering, komplette tekstfelter, cellernes kanter og redigerbarhed for rækker med 5, 8, 9 og 32 celler. Sammenlign rækkernes indhold med appens PDF, som renderes og efterprøves visuelt. Gem syntetiske artefakter til næste regression.
- I en native app på hver målplatform, eksportér alle formater via filvælgeren og prøv »Udskriv…« i systemets PDF-fremviser. Kontrollér, at siden står vandret i dialogen og kan udskrives på én side. Prøv fejl ved konvertering, annullering, manglende værktøjspakke og Gem PDF-reserven.
- Prøv personnetværkets SVG-, PNG- og PDF-eksport samt biografiernes DOCX, ODT, PDF og udskriftsflow igen. Brug eksisterende røgtestscenarier fra sprint 7B–7C.
