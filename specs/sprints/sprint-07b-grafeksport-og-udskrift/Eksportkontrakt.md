# Fælles kontrakt: lokale værktøjer, eksport og udskrift

Status: planlagt. Gælder sprint 7B og 7C. [Grafkontrakten](../sprint-07a-personnetvaerk/Grafkontrakt.md) er grundlaget for personer, relationer, farver og koordinater.

## Afhængigheder og forløb

Sprint 7A etablerer C#-procesrunneren og Graphviz. Sprint 7B tilføjer den lokale Python-sidecar og WeasyPrint til graf-PDF. Sprint 7C tilføjer Pandoc til DOCX/ODT og biografi-HTML, som samme WeasyPrint-kæde konverterer til PDF. Der er ingen afhængighed fra 7A tilbage til eksport. Brug ikke både python-docx og Pandoc til samme opgave; python-docx er ikke nødvendig for den aftalte første version.

Native AOT-hovedprogrammet bevares. Sidecar, Python-runtime, Graphviz, Pandoc, WeasyPrint, skrifttyper og nødvendige native biblioteker leveres i en privat platformspakke efter den eksisterende [distributions-ADR](../sprint-04e-dataintegritet-og-komplet-synkronisering/11-regression-platform-og-distribution/ADR-001-platformspakker-med-native-biblioteker.md). Slutbrugeren skal ikke installere Python, Pandoc, Graphviz, Word eller LibreOffice. Udvikleren må konfigurere lokale værktøjsstier. Der må ikke ske download eller pakkeinstallation under almindelig kørsel. Versioner og licenser fastlåses i implementeringsarbejdet efter en fungerende prøve på alle tre platforme; planens versionsnavne er ikke en påstand om afprøvet kompatibilitet.

## Procesrunner

Genbrug `ILocalProcessRunner` fra 7A. Foreslået input: absolut programsti, `IReadOnlyList<string>` argumenter, arbejdsmappe, timeout og `CancellationToken`. Resultat: exitkode, begrænset stdout/stderr og varighed. Brug `ProcessStartInfo.ArgumentList`, `UseShellExecute=false`, `CreateNoWindow=true`, samtidig læsning af output og `Kill(entireProcessTree: true)` ved stop. Ingen shellstrenge eller sammensatte kommandolinjer. Persontekst sendes som lokale filer, aldrig som procesargumenter eller loglinjer.

Standardtimeout: 30 sekunder for layout, 120 sekunder for én eksport. Ved flere separate dokumenter gælder timeout pr. dokument; samlet eksport kan annulleres mellem og under konverteringer. Begræns stdout til 1 MiB og stderr til 4 MiB; ved overskridelse dræbes processen og der vises en dansk fejl. Input- og outputgrænser kontrolleres før store filer læses i hukommelsen.

`LocalToolLocator` søger først i den private værktøjsmappe relativt til appens installation. En eksplicit udviklerkonfiguration må være reserve i udviklingsbyg. Normal udgivelse bruger ikke et vilkårligt program fra PATH. Kontrol af versioner køres ved første behov og caches. Manglende eksportværktøj deaktiverer kun de formater, der afhænger af det; SVG/PNG kræver ikke Python, når layout allerede findes.

## Sidecar-protokol version 1

Placér ny kode under `sidecar/export/`: `export.py`, `protocol.py`, `pdf_renderer.py`, senere `pandoc_converter.py`, `tests/` samt låst afhængighedsfil. Tilføj `scripts/package-export-tools.*` til platformspakning. Dette er nye mapper; der findes endnu ingen eksportsidecar i repoet.

C# skriver UTF-8 `request.json` i en unik jobmappe og starter den private Python med `export.py --request <absolut-sti>`. Sidecaren skriver præcis ét JSON-resultat til stdout. Tekniske fejl går til stderr uden fuld dokumenttekst. C# bruger kildegenereret JSON-serialisering og afviser ukendt protokolversion.

```json
{
  "protocolVersion": 1,
  "jobId": "lokalt-genereret-id",
  "operation": "graph-pdf",
  "inputPath": "/job/input.html",
  "outputPath": "/job/result.pdf",
  "assetRoot": "/job/assets",
  "options": { "paper": "A4", "orientation": "landscape" }
}
```

Stierne er eksempler og dannes platformsrigtigt. `operation` tillader kun `graph-pdf`, `biography-docx`, `biography-odt`, `biography-pdf`. Biografiformaterne tilføjes i 7C. Valider filendelser, filstørrelser, enum-værdier og at alle jobstier efter fysisk stiopløsning ligger inden for jobmappen. Resultat:

```json
{
  "protocolVersion": 1,
  "jobId": "lokalt-genereret-id",
  "status": "ok",
  "outputPath": "/job/result.pdf",
  "warnings": []
}
```

Ved fejl bruges `status: "error"`, `errorCode` fra `missing-tool`, `invalid-input`, `unsupported-version`, `conversion-failed`, `resource-blocked`, `timeout`; ingen outputsti. Alle forklaringer til brugeren oversættes i C#. Exitkode er 0 ved succes, forskellig fra 0 ved fejl. C# kræver matchende job-id, protokol, forventet outputsti, korrekt filsignatur og ikke-tom fil før godkendelse. Et exit-0 uden gyldigt output er en fejl.

Sidecaren skriver kun i jobmappen. C# flytter/publicerer det validerede output atomisk til brugerens valgte mål gennem eksisterende `IAtomicFileWriter.WriteBytes` for filer op til 100 MiB. Større output afvises med forståelig besked i første version; en fremtidig streamingvariant kræver en separat kontrakt. Gamle målfiler må ikke ændres ved konverteringsfejl eller annullering før publicering. Annullering efter atomisk publicering betyder, at filen allerede er gemt, og UI skal rapportere dette korrekt.

Jobmapper slettes i `finally`. Rester efter nedbrud slettes ved næste opstart, hvis de er markeret som appens egne, ældre end et døgn og ikke aktive. Udskrifts-PDF beholdes i højst et døgn, fordi den eksterne fremviser kan læse asynkront. Midlertidige dokumenter indgår ikke i almindelig applog.

## Netværk og lokale ressourcer

Konverteringen bruger kun jobfiler og private værktøjer. Appen samler på forhånd tilladte lokale billeder/skrifttyper i `assets` med genererede navne; ingen CSS-import, plugins, Lua-filtre, webfonts eller brugerdefinerede procesargumenter. Afvis eksterne billed-URL'er og fysisk stiudbrud, inklusive symlinks. Eksterne tekstlinks må bevares som passive hyperlinks; de hentes aldrig under eksport.

WeasyPrint får en egen URL-fetcher, der kun læser de fysisk kontrollerede jobaktiver. Den må aldrig kalde standardfetcheren for netværks-URL'er. HTML/CSS/SVG kontrolleres før rendering; SVG fra brugerens mediefiler er ikke tilladt i første version. Den internt genererede graf-SVG indeholder kun linjer, cirkler og tekst. Dette følger nødvendigheden af kontrolleret ressourcelæsning beskrevet i [WeasyPrints dokumentation](https://doc.courtbouillon.org/weasyprint/stable/common_use_cases.html).

## Grafeksport

Dialogen **Eksportér graf** tilbyder SVG, PNG og PDF samt **Hele grafen** (standard) eller **Aktuelt udsnit**. Begge bruger et fastfrosset graf-/layoutøjebliksbillede ved klik på Eksportér; nye importer må ikke ændre det igangværende output. Hele grafen indeholder alle noder og kanter uanset skærmens culling. Udsnittet klipper efter kameraets rektangel; kanter, som krydser udsnittet, medtages og klippes korrekt.

`GraphExportSceneBuilder` danner samme rene scenestruktur for alle formater: linjer, prikker, labels, signatur og bounds. UI og eksport deler størrelses-/farvetokens og tekstformatter, ikke live kontrolinstanser. Eksport bruger lyst tema, hvid baggrund, probandmarkering og valgfrit **Vis navne og datoer** (standard til). Hover/fokus fremhæves ikke. Labels overlapkontrolleres; ved tætte grafer forklares, at brugeren kan eksportere et udsnit eller større format, men ingen labels fjernes i stilhed.

- SVG: C# `GraphSvgWriter`, invariant talformat, escaped tekst, fast `viewBox`, ingen scripts, links til appen, eksterne fonts eller ressourcer. Brug almindelige SVG-elementer. Output er selvstændigt.
- PNG: `AvaloniaGraphRasterizer` tegner scenen på en separat bitmap; ingen skærmbilledeoptagelse. Størrelse vælges som langside 2048, 4096 (standard) eller 8192 px; bevar aspektforhold og begræns til 64 megapixel. Brug UI-dispatcher kun hvor Avalonia kræver det, mål blokering, og opdel arbejde ved behov. Den levende graf må ikke flyttes eller omstyles for at eksportere.
- PDF: samme scene som intern SVG i kontrolleret HTML gennem lokal WeasyPrint. A4/A3, stående/liggende, standard A4 liggende, 10 mm margen. Én side med hele valgte grafområde skaleret til siden; ingen flersidet plakat i første version. For lille tekst varsles i dialogen. Teksten skal være tekst i PDF, ikke ét bitmapbillede.

## Udskrift

Den konkrete første leverance er **Udskriv…**, som genererer den samme PDF og åbner den i systemets PDF-fremviser. En kort dansk besked forklarer: **PDF'en åbnes. Vælg Udskriv i PDF-fremviseren for at vælge printer.** Brugeren vælger printer og antal i fremviserens systemdialog. Dette er udskriftsflowet i trin 12; der loves ikke en indbygget, ens Avalonia-printdialog eller automatisk printning.

`ISystemDocumentLauncher` åbner kun den netop genererede lokale PDF efter den udtrykkelige handling. OS-filassociation må anvendes med en valideret sti; den særlige åbning bruger ingen dokumentindhold som kommando. Fejl/manglende fremviser giver **Gem PDF i stedet**. Annullering af systemdialogen er ikke en eksportfejl. Ved fremviseren kan appen kun rapportere **PDF åbnet til udskrift**, aldrig **Udskrevet** uden printerkvittering. Biografier genbruger dette flow i 7C.
