### Introduktion til GEDCOM

**GEDCOM** er en forkortelse for **GEnealogical Data COMmunication** 1-3. Det er et åbent filformat og den **de facto-standard**, der bruges til at gemme og udveksle genealogiske data (slægtsdata) mellem forskellige slægtsforskningsprogrammer og hjemmesider 1, 4, 5\.

* **Hvor kommer det fra?** Formatet blev oprindeligt udviklet af **Jesu Kristi Kirke af Sidste Dages Hellige** (LDS Church) i 1984 for at gøre det lettere at dele forskning og gemme information i et ensartet, computerlæsbart format 1, 2, 6, 7\.  
* **Hvor bruges det?** Det bruges i dag af næsten alle slægtsforskningsplatforme, såsom **Ancestry**, **MyHeritage** og **FamilySearch**, som et fælles sprog, der gør det muligt at flytte sit familietræ fra ét program til et andet uden at skulle indtaste alle data manuelt igen 1, 8, 9\.

### Tutorial: De basale elementer i en GEDCOM-fil

En GEDCOM-fil er en **almindelig tekstfil** (.ged), der er opbygget hierarkisk 8, 10\. Hver linje består af et **niveaunummer**, et **tag** (et mærke) og ofte en **værdi** eller en **reference** 11-13.

1. **Niveaunummer:** Starter ved 0 for hovedposter (personer, familier) og stiger (1, 2, 3...) for underliggende detaljer 13-15.  
2. **Tags:** Korte koder på 3-4 bogstaver (f.eks. INDI for individ, BIRT for fødsel) 11, 16, 17\.  
3. **Cross-reference ID:** Unikke ID'er (f.eks. @I1@) der bruges til at linke poster sammen 18-20.

#### Eksempel 1: Grundstruktur for en person

Dette eksempel viser en person med navn og fødselsdato.  
0 @I1@ INDI          (Niveau 0: Ny personpost med ID @I1@) \[21, 22\]  
1 NAME Hans /Jensen/ (Niveau 1: Navn; efternavn er mellem skråstreger) \[23, 24\]  
1 SEX M              (Niveau 1: Køn; Mand) \[21, 25\]  
1 BIRT               (Niveau 1: Begivenhed; Fødsel) \[26, 27\]  
2 DATE 12 MAY 1920   (Niveau 2: Under fødsel; Dato) \[28, 29\]  
2 PLAC Aarhus, DK    (Niveau 2: Under fødsel; Sted) \[30, 31\]

#### Eksempel 2: Grundstruktur for en familie

Dette eksempel viser, hvordan to personer (mand og kone) kobles sammen til en familie med et barn.  
0 @F1@ FAM           (Niveau 0: Ny familiepost med ID @F1@) \[32, 33\]  
1 HUSB @I1@          (Niveau 1: Ægtemand er person @I1@) \[32, 34\]  
1 WIFE @I2@          (Niveau 1: Hustru er person @I2@) \[32, 34\]  
1 CHIL @I3@          (Niveau 1: Barn er person @I3@) \[32, 34\]  
1 MARR               (Niveau 1: Begivenhed; Vielse) \[35, 36\]  
2 DATE 1 JUN 1945    (Niveau 2: Vielselsdato) \[37, 38\]

#### Eksempel 3: Et lille familietræ (3 generationer)

Her kobles bedsteforældre, forældre og børn via familie-tags (FAMS for familie hvor man er forælder, FAMC for familie hvor man er barn).  
0 @I1@ INDI (Bedstefar)  
1 NAME Ole /Jensen/  
1 FAMS @F1@

0 @I2@ INDI (Far)  
1 NAME Hans /Jensen/  
1 FAMC @F1@ (Barn af Ole i familie F1)  
1 FAMS @F2@ (Far i familie F2)

0 @I3@ INDI (Barn \- 3\. generation)  
1 NAME Erik /Jensen/  
1 FAMC @I2@ (Barn af Hans i familie F2)

0 @F1@ FAM (Bedsteforældrenes familie)  
1 HUSB @I1@  
1 CHIL @I2@

0 @F2@ FAM (Forældrenes familie)  
1 HUSB @I2@  
1 CHIL @I3@

### Almindelige GEDCOM-typer (poster)

Type,Tag,Indeholdte elementer (eksempler),Kort eksempel  
Individ,INDI,"Navn, køn, fødsel, død, titler 21, 39.",0 @I1@ INDI 21  
Familie,FAM,"Referencer til mand, kone og børn, vielse 32, 39.",0 @F1@ FAM 32  
Kilde,SOUR,"Titel, forfatter, udgivelsesdata 39, 40.",0 @S1@ SOUR 40  
Multimedie,OBJE,"Filsti til billede, format, titel 39, 41.",0 @O1@ OBJE 41  
Header,HEAD,"Programnavn, version, tegnsæt (UTF-8) 42, 43.",0 HEAD 42  
Note,NOTE,"Tekstlige kommentarer fra forskeren 44, 45.",0 @N1@ NOTE 44

### Almindelige problemer, fejl og udfordringer

Selvom GEDCOM er en standard, opstår der ofte udfordringer, når data flyttes mellem forskellige systemer:

* **Manglende understøttelse af tags:** Mange programmer implementerer deres egne "private" tags (starter med en understregning, f.eks. \_MYTAG), som andre programmer ikke forstår og derfor ignorerer eller forkaster 10, 46-48.  
* **Fejl i tegnsæt:** Ældre filer bruger ofte formater som ANSEL eller ANSI. Hvis de indlæses som UTF-8 (eller omvendt), kan danske bogstaver som **æ, ø og å** blive ødelagt 10, 49-51.  
* **Multimediefiler:** En GEDCOM-fil indeholder kun **tekst**. Billeder og dokumenter er ikke indlejret i selve filen, men findes kun som links til placeringer på en computer. Hvis man flytter filen uden at flytte billederne (eller opdatere stierne), brydes forbindelsen 52-54.  
* **Tab af data ved import:** Nogle programmer læser ikke alle niveauer af data (f.eks. noter knyttet til specifikke begivenheder), hvilket kan føre til permanent tab af information under en flytning 55, 56\.  
* **Kronologi:** Hvis fødselsdatoer mangler, kan rækkefølgen af børn i en familie blive forkert, da standarden ikke altid tvinger en bestemt sortering 57, 58\.  
* **Ambivalente standarder:** Der findes flere versioner (f.eks. 5.5, 5.5.1 og 7.0), og de er ikke altid 100% kompatible med hinanden 10, 59, 60\.