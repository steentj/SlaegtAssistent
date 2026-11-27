# Validering af formateret redigering

- Flyt markøren gennem `Start __tekst__ slut` med tastatur og klik på den fede tekst. Kontrollér visningen samme sted, når markøren flyttes ud.
- Markér hen over flere elementer, også baglæns. Prøv indlejret formatering, flerlinjet Markdown, dansk tekst og CRLF.
- Redigér et udtryk, skift visning, fortryd og gentag. Gem og genåbn, og sammenlign Markdown med den oprindelige tekst.
- Prøv dokumentvalg, eksisterende redigeringsmenu og tekstombrydning. Kør målrettede tests og derefter den samlede suite.

## Resultat

Den 27. november 2026 bestod de automatiske Avalonia-prøver af fed tekst ved tastatur og museklik, baglæns markering over CRLF, indlejret formatering, overskrifter, citater, lister, kodeblokke, tabeller, rå visning, fortrydelse og genåbning. Den samlede suite havde 691 beståede tests og én sprunget over. Den oversprungne test kræver en separat Python-sidecar. Den pakkede sidecar blev afprøvet i suiten.

Native AOT-udgivelsesbygningen til macOS ARM64 blev gennemført uden advarsler. De visuelle prøver kørte i Avalonias automatiske testmiljø. Manuel visuel afprøvning på de enkelte platforme udestår.
