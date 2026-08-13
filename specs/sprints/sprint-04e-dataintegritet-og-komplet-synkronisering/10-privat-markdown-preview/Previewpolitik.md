# Previewpolitik

## Grundregel

Markdown-previewet er en lokal, passiv visning. Det må ikke hente indhold fra nettet, køre kode eller skrive til Markdown-dokumentet. Hovedpreviewet og begge hjælpevinduer bruger den samme `SafeMarkdownPreviewService` og den samme sikkerhedspolitik.

## Markdown og HTML

- Markdown fortolkes med Markdig med rå HTML deaktiveret.
- Rå HTML vises som tekst. Tags som `script`, `iframe`, `link` og `style` bliver derfor aldrig aktive elementer.
- Blokeret indhold erstattes med eller ledsages af en diskret dansk forklaring i det genererede preview.
- Forklaringen findes kun i renderresultatet; kildeteksten ændres ikke.

## Ressourcer og netværk

Det genererede HTML-dokument har følgende sikkerhedspolitik for indhold:

```text
default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src 'none'; connect-src 'none'; frame-src 'none'; media-src 'none'; object-src 'none'; script-src 'none'; base-uri 'none'; form-action 'none'
```

WebView-controlleren tillader desuden kun `data:` og `about:` som underressourcer. Den stopper andre ressourceanmodninger som et ekstra forsvar. Previewet indeholder ingen eksterne stylesheets, skrifttyper, scripts, frames eller medier.

## Lokale billeder

- Billeder opløses af applikationen og indlejres som `data:`; WebView'en får aldrig en lokal filsti.
- Dokumentets mappe er tilladt. Derudover kan applikationen angive kontrollerede arbejdsområde- og mediemapper, aktuelt Markdown-outputmappen, standardoutputmappen og GEDCOM-filens mappe.
- Stiudbrud uden for disse mapper blokeres, også når udbruddet forsøges gennem et symbolsk link.
- Tilladte formater er PNG, JPEG, GIF, WebP og BMP. SVG er bevidst blokeret, fordi formatet kan indeholde aktivt indhold.
- En billedfil må højst fylde 20 MB.
- Manglende, ulæselige, for store eller ikke-tilladte filer vises som blokerede og forklares på dansk.

## Links

- Interne fragmentlinks kan blive i previewet.
- `http:`- og `https:`-links åbnes aldrig automatisk. Et klik standser navigationen og viser hele destinationen i en dialog. Brugeren kan kopiere destinationen eller lukke dialogen.
- Nye vinduer håndteres på samme måde.
- `javascript:`, `data:`, `file:`, `ftp:` samt lokale og ukendte linktyper fjernes fra det aktive link.

## Temaer og kontrol

Den fælles HTML bruger eksplicitte farver, som previewets eksisterende temalag omsætter til mørkt tema. Lyst, mørkt og systemstyret tema skal kontrolleres manuelt. Den manuelle validering omfatter også en netværksmonitor, fordi dette kontrollerer WebView-motorens faktiske adfærd ud over de automatiske pipeline- og policytests.
