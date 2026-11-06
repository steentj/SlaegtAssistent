# Krav: Links mellem persondokumenter

1. Genererede strukturerede omtaler af forældre, børn og partnere skal vises som `[navn](relativ-sti.md)`, når der findes ét entydigt dokumentmål eller et dokument oprettes i samme batch.
2. Relationer identificeres ved GEDCOM-record-id, aldrig alene ved personnavn. To personer med samme navn må have forskellige korrekte mål.
3. Eksisterende dokumenters faktiske sti skal anvendes, også efter navneændring eller ved brugerdefineret filnavn. Målregister og filskrivning skal være enige.
4. Manglende eller tvetydigt dokumentmål giver læsbar almindelig tekst uden et opdigtet link. Tomt navn må bruge record-id som et læsbart alternativ.
5. Relative stier skal håndtere undermapper, mellemrum, danske bogstaver, parenteser, `#` og `%`. Linktekster skal escapes, så personnavne ikke ændrer Markdown-strukturen.
6. Standardskabelon, understøttede egne skabeloner og rendering fra godkendte snapshots skal anvende samme linkregler. Nye felter skal valideres og beskrives i hjælpen.
7. Eksisterende dokumenter opdateres gennem den normale kandidatgodkendelse. Afviste ændringer og fri biografitekst bevares. Links er renderingsdata og må ikke ændre GEDCOM-fakta eller skabe vedvarende falske faktadifferencer.
8. Vilkårlige navne i noter og fri tekst må ikke sammenkædes ved et usikkert globalt tekstmatch. Brugerens eksisterende links og kodeafsnit må ikke omskrives automatisk.
9. Ingen cloud-kald, telemetri eller automatisk adgang til eksterne mål. Core og ViewModels holdes fri for Avalonia-typer.
10. Gentagen rendering med samme data og målregister skal give samme resultat. Genimport efter godkendelse må ikke give en ny identisk kandidat.
