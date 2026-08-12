# Requirements: Robust frontmatter, migrering og katalogdiagnostik

- Frontmatter skal parses med en versionsstyret, deterministisk og ikke-kastende kontrakt ved katalogindlæsning.
- En fejl i én fil må ikke blokere øvrige dokumenter eller appens opstart.
- Dublerede nøgler, ugyldige værdier, manglende obligatoriske felter og ukendt formatversion skal give præcis diagnostik.
- Ukendte formatversioner må ikke automatisk ændres eller matches som kendte dokumenter.
- Understøttede ældre versioner skal migreres gennem eksplicitte, testede migreringstrin.
- Migrering skal bevare dokumentindhold uden for metadata byte-for-byte.
- Defekte og tvetydige dokumenter skal være synlige i UI med filsti, fejlkategori og sikker næste handling.
- Kataloget skal fortsætte efter I/O-, JSON-, YAML-, format- og dubletfejl.
