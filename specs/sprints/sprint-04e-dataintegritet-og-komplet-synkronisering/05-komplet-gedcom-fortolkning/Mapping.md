# Mapping: GEDCOM 5.5.1 til produktets domæne

Denne tabel er den normative datakontrakt for feature 4.8.5. Inputrækkefølgen bevares for records og gentagne elementer. `CONT` indsætter `\n`; `CONC` sammenkæder uden et indsat tegn. Betydende mellemrum efter den ene obligatoriske tagseparator bevares.

## Records

| GEDCOM-record | Understøttede tags og understrukturer | Domænemål | Bevaringsregel |
| --- | --- | --- | --- |
| `HEAD` | `SUBM`, `CHAR` | `FamilyTree.SubmitterRecordId`, tegnsætsvalg | `CHAR` styrer afkodning; manglende `CHAR` giver advarsel og sikker UTF-8-standard. |
| `INDI` | Se persontabellen | `Person` | Hele det afkodede personsegment bevares med oprindelige linjeskift i `RawGedcom`. |
| `FAM` | Se familietabellen | `Family` og relationer | Recordrækkefølge samt rækkefølgen af børn, kilder, noter og hændelser bevares. |
| `SOUR` | `TITL`, `AUTH`, `PUBL`, `TEXT`, `REPO`, `PAGE`, `DATA`, `DATE`, `NOTE` | `Source` | Fritekst og fortsættelseslinjer bevares som tekst. |
| `OBJE` | `FILE`, `FORM`, `TITL`, `TYPE`, `NOTE` | `Media` | Inline- og recordbaserede medier følger samme felter. |
| `SUBM` | `NAME`, `ADDR`, `PHON`, `EMAIL`, `WWW`, `LANG` | `Submitter` | Den submitter, som `HEAD.SUBM` peger på, aktiveres. |
| `REPO` | Referencen i `SOUR.REPO` | `Source.Repository` | Repository-pointeren bevares. Selvstændige repository-detaljer er ikke del af den nuværende produktmodel. |
| `NOTE`, `SUBN` og øvrige level-0-records | Ingen strukturerede domænefelter i denne version | Intet selvstændigt domæneobjekt | De må ikke oprette personer, familier eller hændelser. Pointertekst, der optræder i et understøttet `NOTE`-felt, bevares dér. |

## Persondata (`INDI`)

| GEDCOM-tag | Underfelter | Domænemål |
| --- | --- | --- |
| `NAME` | Primær linjeværdi | `Person.FullName`; GEDCOM-skråstreger fjernes kun i visningsnavnet. |
| `SEX` | Linjeværdi | `Person.Sex`. |
| `NOTE` | `CONT`, `CONC` | `Person.Notes` i inputrækkefølge. |
| `SOUR` | Citationstabellen | `Person.Sources`. |
| `OBJE` | Mediefelterne ovenfor | `Person.Media`. |
| `CENS` | `DATE`, `PLAC`, `NOTE`, `SOUR` | `Person.Census`; citationen følger citationstabellen. |
| `BIRT`, `DEAT` | Hændelsesfelterne | `Person.Events` samt kompatibilitetsfelterne for fødsel/død. |
| `ADOP`, `BAPL`, `BAPM`, `BARM`, `BASM`, `BLES`, `BURI`, `CAST`, `CHR`, `CHRA`, `CONF`, `CONL`, `CREM`, `DSCR`, `EDUC`, `EMIG`, `ENDL`, `EVEN`, `FACT`, `FCOM`, `GRAD`, `IDNO`, `IMMI`, `NATI`, `NATU`, `NCHI`, `NMR`, `OCCU`, `ORDN`, `PROB`, `PROP`, `RELI`, `RESI`, `RETI`, `SLGC`, `SSN`, `TITL`, `WILL` | `DATE`, `PLAC`, `TYPE`, `NOTE`, `SOUR` | `Person.Events` i inputrækkefølge. Den rå tagværdi bevares. |
| Ukendt tag, herunder leverandørtag med `_` | `DATE`, `PLAC`, `TYPE`, `NOTE`, `SOUR` når de findes | `Person.Events` med rå tag og diagnostik. |
| `FAMC`, `FAMS`, `CHAN`, `AFN`, `ALIA`, `ANCI`, `ASSO`, `DESI`, `REFN`, `RESN`, `RFN`, `RIN`, `SUBM`, `UID`, `_UID`, `_FSFTID` | Eventuelle underfelter | Strukturtags. De oprettes aldrig som hændelser; relationer udledes normativt fra `FAM`. |

## Familiedata (`FAM`)

| GEDCOM-tag | Underfelter | Domænemål |
| --- | --- | --- |
| `HUSB`, `WIFE`, `CHIL` | Record-pointer | `Family.Husband`, `Family.Wife`, `Family.Children` og de afledte personrelationer. |
| `NOTE` | `CONT`, `CONC` | `Family.Notes`. |
| `SOUR` | Citationstabellen | `Family.Sources`. |
| `ANUL`, `CENS`, `DIV`, `DIVF`, `ENGA`, `EVEN`, `MARB`, `MARC`, `MARL`, `MARR`, `MARS`, `RESI`, `SLGS` | `DATE`, `PLAC`, `TYPE`, `NOTE`, `SOUR` | `Family.Events` i inputrækkefølge. |
| Ukendt tag | Hændelsesfelter når de findes | `Family.Events` med rå tag og diagnostik. |
| `CHAN`, `NCHI`, `OBJE`, `REFN`, `RESN`, `RIN`, `SUBM` | Eventuelle underfelter | Strukturtags. De oprettes aldrig som hændelser. |

## Hændelsesfelter og klassifikation

| Felt | Domænemål |
| --- | --- |
| Hændelsens egen værdi | `GedcomEvent.Value`. |
| `DATE` | `GedcomEvent.Date`. |
| `PLAC` | `GedcomEvent.Place`. |
| `TYPE` | `GedcomEvent.Type`. |
| `NOTE` | `GedcomEvent.Note`. |
| `SOUR` | `GedcomEvent.Sources` efter citationstabellen. |

Kategorierne `Birth`, `Baptism`, `Confirmation`, `Marriage`, `Death`, `Burial`, `Census` og militærtjeneste bruges, hvor produktet har en særskilt kategori. Andre kendte og ukendte hændelser klassificeres som `Other`, men deres tag og data bevares.

## Kildecitation

| GEDCOM-sti relativt til `SOUR` | Domænefelt |
| --- | --- |
| Pointer eller inlinebeskrivelse | `Source.RecordId` eller `Source.Title`. |
| `PAGE` | `Source.Page`. |
| `DATA` | `Source.Data`. |
| `DATA.DATE` eller `DATE` | `Source.Date`. |
| `DATA.TEXT` eller `TEXT` | `Source.Text`. |
| `NOTE` | `Source.Note`. |
| `TITL`, `AUTH`, `PUBL`, `REPO` | De tilsvarende bibliografiske `Source`-felter. |

Citationstabellen gælder ens under person, personhændelse, census, familie og familiehændelse. Felter på citationen overskriver kun de samme felter på den konkrete citation; den refererede `SOUR`-record ændres ikke.

## Tegnsæt og fejlgrænser

| `HEAD.CHAR` | Fortolkning |
| --- | --- |
| `UTF-8` | Streng UTF-8-afkodning med eller uden UTF-8-BOM. |
| `UNICODE` | UTF-16 little- eller big-endian, bestemt af BOM eller byteorden. |
| `ASCII` | 7-bit ASCII; høje byteværdier afvises. |
| `ANSEL` | ASCII-delen samt GEDCOM 5.5.1 Appendix C's spacing- og diakritiske tegn. Diakritika kombineres deterministisk og normaliseres til Unicode NFC. |

En BOM eller fysisk UTF-16-byteorden, der modsiger `CHAR`, er fatal diagnostik. Ugyldige UTF-sekvenser, ukendte ANSEL-byteværdier og ikke-understøttede `CHAR`-værdier afbryder importen med en dansk fejl, før domænemodellen flettes. Der indsættes aldrig lydløst erstatningstegn.
