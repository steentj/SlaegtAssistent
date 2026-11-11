# Plan: Sprint 7C – biografieksport til DOCX, PDF og ODT

Tredje og sidste del af roadmap-trin 12. Forudsætning: persondokumenter og godkendt synkronisering fra trin 5–11 samt lokal værktøjskæde fra 7B. Selve biografieksporten bruger ikke grafkoordinater. Status: planlagt.

Læs [Dokumentkontrakt.md](Dokumentkontrakt.md) og [fælles eksportkontrakt](../sprint-07b-grafeksport-og-udskrift/Eksportkontrakt.md). Gennemfør:

1. [Dokumentgrundlag og konvertering](01-dokumentgrundlag-og-konvertering/Plan.md).
2. [Udvalg, batch og udskrift](02-udvalg-batch-og-udskrift/Plan.md).
3. [Layout, platformspakker og afslutning](03-layout-platform-og-afslutning/Plan.md).

Målet er eksport af de færdige, redigerede tekster; der oprettes ingen nye Markdown-biografier. Hold konvertering og UI adskilt, genbrug den lokale sidecar og filpicker-/dialogmønstre, og indfør ingen cloudtjenester. Følg 7A's afgrænsede test- og dokumentationsrytme.

I feature 1 skal en fungerende DOCX, ODT og PDF fra den faste testbiografi bevise den valgte HTML-kæde, før dialogen bygges. Referencestile, skrifter, sideskift og interne links er en del af den samme opgave. Der må ikke skiftes til en anden eksportmotor uden at opdatere kontrakten med begrundelse og faktiske kompatibilitetsresultater.

Trin 12 er først færdigt, når 7A, 7B og 7C har dokumenteret samlet accept. Pakning uden systemværktøjer er en leverance, ikke en opgave til en senere udgivelse. AI, RAG og HTR følger først i roadmap-trin 13–15 og berøres ikke her.
