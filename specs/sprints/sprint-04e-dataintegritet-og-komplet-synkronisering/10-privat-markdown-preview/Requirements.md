# Requirements: Privat og kontrolleret Markdown-preview

- Preview må ikke foretage udgående netværkskald fra Markdown, HTML, CSS, billeder, frames eller scripts.
- Rå HTML skal enten deaktiveres eller renses efter en eksplicit tilladelsesliste.
- Script, iframe, eksternt stylesheet, ekstern skrifttype og aktive URL-skemaer skal blokeres.
- Lokale billeder skal indlæses gennem en kontrolleret resolver og kun fra tilladte arbejdsområde- eller medieplaceringer.
- Klik på eksterne links må ikke navigere WebView'en automatisk; brugeren skal se destinationen og vælge en eksplicit handling.
- Preview skal have en restriktiv sikkerhedspolitik for indhold som ekstra forsvar.
- Blokeret indhold skal give en diskret dansk forklaring uden at ændre Markdown-dokumentet.
- Samme sikkerhedspolitik skal gælde hovedpreview og hjælpevinduers previews.
- Preview skal fortsat fungere i lyst, mørkt og systemstyret tema.
