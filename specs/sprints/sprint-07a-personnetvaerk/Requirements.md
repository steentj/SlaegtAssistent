# Krav: Sprint 7A

Normativ detaljering: [Grafkontrakt.md](Grafkontrakt.md).

| Id | Acceptkrav | Feature |
| --- | --- | --- |
| G-01 | Én node pr. kendt record-id; alle komponenter, isolerede personer og alle eksplicitte familie-/Markdown-forbindelser medtages | 01 |
| G-02 | Godkendte dokumentdata har forrang; afviste importværdier genindføres ikke; genstart kræver ikke original GEDCOM | 01 |
| G-03 | Dubletter, brudte links og utilstrækkeligt datagrundlag giver diagnostik uden vilkårlig navigation | 01 |
| G-04 | Graphviz udfører lokalt kraftbaseret layout uden UI-blokering; timeout, annullering og gamle resultater håndteres | 02 |
| G-05 | Kvinder er røde, mænd blå og ukendt/andet gråt; frit valgt proband er en større prik med ring | 03–04 |
| G-06 | Hover/fokus viser navn, eksisterende fødsels- og dødsdato ved personen og alle direkte naboer | 03 |
| G-07 | Alle oplysninger findes også i en tilgængelig nabooversigt; ingen afkortning til et fast antal personer | 03 |
| G-08 | Zoom, panorering, vis alle og centrér proband virker i begge temaer og ved skærmskalering | 03 |
| G-09 | Personklik åbner eksisterende Markdown-arbejdsflade og bevarer alle ugemte editorer; klik ændrer ikke proband | 04 |
| G-10 | Søgning og valg af proband omfatter hele datasættet uden at skjule noder eller forbindelser | 04 |
| G-11 | Proband gemmes pr. arbejdsområde; import, katalogændring og arbejdsområdeskift giver korrekt ny revision | 04 |
| G-12 | Grafen opfylder kontraktens ydelsesmål og fungerer uden cloud, telemetri eller nye netværksafhængigheder | 02–04 |

Der må ikke oprettes eller omskrives persondokumenter som bivirkning af grafvisning. En diagnosticeret eller manglende biografi forhindrer kun navigation til det berørte mål.
