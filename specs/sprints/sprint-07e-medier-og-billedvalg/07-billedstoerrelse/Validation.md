# Validering: Proportional billedstørrelse

Status: Implementeret og automatisk prøvet; manuel visuel validering mangler. Biografieksporten anvender fortsat stående A4 med 20 mm margener. Eksportkopier normaliseres til 96 billedpunkter pr. tomme og højst 642 × 971 billedpunkter; originalfilerne ændres ikke.

Prøv alle tre størrelser i billedvælger, preview og PDF, DOCX og ODT. Kontrollér dimensioner og billedforhold, gamle Markdown-billeder, ugemt buffer, genåbning og bevarelse af originalfilen. Registrér manuel visuel kontrol og eventuelle platformsbegrænsninger særskilt.


| Dato/platform | Prøve | Resultat |
| --- | --- | --- |
| 16. november 2026, macOS arm64 | Billedvalg, forhåndsvisning, skalerede eksportkopier og EXIF-drejning | Automatiske prøver består. |
| 16. november 2026, macOS arm64 | Pakket PDF-, DOCX- og ODT-eksport | Automatiske prøver bekræfter hel, halv og kvart bredde samt at små billeder ikke forstørres. |
| 16. november 2026, macOS arm64 | Ændret original efter gennemgang | Eksporten afviser en ændret original, også når den skalerede kopi ser uændret ud. |
| 16. november 2026, macOS arm64 | Native AOT | Publicering lykkedes; røgt-testen afsluttede med kode 137 uden output. |
| Ikke kørt | Visuel kontrol og øvrige platforme | Afventer kontrol i PDF- og kontorfremvisere samt native platformprøver. |
