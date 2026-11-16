# Samlede krav: sprint 7E

Status: **planlagt**. Detaljerne i [Mediekontrakt.md](Mediekontrakt.md) er en del af kravene. Featurekravene er fordelt i undermapperne.

| Id | Krav og målbar accept | Feature |
| --- | --- | --- |
| K01 | Mediemappe kan vælges, skrives, gemmes, genåbnes og ryddes som de eksisterende mapper; gamle indstillinger virker uden feltet. | 01: M01 |
| K02 | GEDCOM-reference opløses deterministisk under valgte lokale rødder. Tvetydige referencer giver ingen automatisk filudvælgelse. | 01: M02 |
| K03 | Mappeskift ændrer ikke dokumenttekst, dirty-status eller originalfiler; åbne previewkontekster opdateres. | 01: M03–M04 |
| K04 | Genererede medieafsnit er liste/tabel og viser ingen automatisk indlagte billeder. | 02: O01–O02 |
| K05 | Gamle skabeloner kan åbnes; ændring af eksisterende genererede dokumentafsnit sker kun via kandidataccept. | 02: O03–O04 |
| K06 | Én fælles vælger viser miniature, titel, filnavn og status, med søgning samt Personens medier/Alle refererede medier. | 03: B01 |
| K07 | Kun læsbare, validerede PNG/JPEG-filer inden for grænserne kan vælges. Manglende og andre medier forbliver synlige med forklaring. | 03: B02 |
| K08 | Vælgeren har alternativ tekst, valgfri billedtekst, Annullér og eksplicit bekræftelse; fuld tastaturbetjening og danske tilgængelighedsnavne. | 03: B03 |
| K09 | Galleri, indeks og dekodning overholder ressourcebudget og afbrydelse. Ingen forældede resultater tilføjes efter kontekstskift. | 03: B04 |
| K10 | Ét topbillede kan vælges, udskiftes og fjernes pr. dokument. Det ligger før den genererede sektion og gemmes som Markdown. | 04: T01–T02 |
| K11 | Topbillede og fri tekst bevares ved genstart, GEDCOM-/skabelonopdatering, kandidataccept og afvisning. | 04: T03–T04 |
| K12 | Ét eller flere livsforløbsbilleder kan indsættes ved markøren som selvstændige billedafsnit; valide frie markeringer kan erstattes. | 05: L01–L02 |
| K13 | Indsættelse i fakta/topblok, over sektionsgrænser eller i forældet kontekst afvises uden tekstændring. | 05: L03 |
| K14 | Billedændringer bruger aktuel editorbuffer, sætter dirty-status og kan fortrydes/gentages; eksisterende Gem, lukkeadvarsel og Commit gælder. | 04–05: T03, L04 |
| K15 | Preview, PDF, DOCX, ODT og udskrifts-PDF indeholder valgte billeder, også fra ugemt buffer, og overholder samme lokale rodpolitik. | 06: E01–E02 |
| K16 | Der er ingen netværkskald, automatiske downloads, originalændringer eller kopiering af mediebiblioteket til output. | Alle |
| K17 | Core og ViewModels forbliver UI-uafhængige. Eksisterende byggemål, testkonvention og Native AOT respekteres. | Alle, 06: E03 |
| K18 | Hjælp beskriver placering, formater, relativ sti, mappeskift og at billedoriginaler ikke indgår i dokumenternes versionshistorik. | 06: E04 |

## Brugerhistorier

- Som slægtsforsker vil jeg angive den mappe, mine GEDCOM-medier faktisk ligger i, så jeg kan bruge dem uden at flytte filerne til GEDCOM-mappen.
- Som skribent vil jeg vælge personens topbillede ud fra små forhåndsvisninger, så jeg kan genkende det rigtige foto.
- Som skribent vil jeg placere billeder mellem mine egne afsnit om livsforløbet, så billederne understøtter fortællingen.
- Som dokumentejer vil jeg fortsat have kontrol over alle billeder, så nye mediereferencer eller skabelonændringer ikke fylder dokumentet med billeder eller fjerner mine valg.
- Som modtager vil jeg se de valgte billeder i de eksporterede dokumenter uden adgang til skribentens mediemappe.

## Uden for kravene

Ingen automatisk portrætgenkendelse, ny GEDCOM-medieredigering, billedredigering, formatkonvertering, fri filupload, automatisk reparation af gamle billedlinks eller versionering af originalfiler. Manuel billedsyntaks i eksisterende dokumenter fjernes ikke automatisk.
