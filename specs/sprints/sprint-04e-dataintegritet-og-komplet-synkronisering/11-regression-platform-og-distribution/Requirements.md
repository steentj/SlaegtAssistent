# Requirements: Samlet regression, platform og distribution

- Der skal findes en sporbar risikomatrix fra Missionens garantier og `Review_20260811.md` til automatiske og manuelle tests.
- Integrationsscenarier skal dække atomisk skrivning, navneændring, dubletter, mappeskift, importfejl, cancellation, feltvalg, afvisning, godkendelse og genstart.
- Rendererløse Avalonia-tests skal dække kritiske bindings-, dialog-, tastatur- og vindueslivscyklusforløb.
- En repræsentativ GEDCOM-testdatapakke skal dække mappings, tegnsæt, fortsættelseslinjer, citationer og fejl, der kan isoleres.
- Store testdata skal verificere responsivt UI og dokumenteret ressourceforbrug uden vilkårlige tidsbaserede tests.
- SDK-version og dependencies skal låses reproducerbart.
- CI skal bygge og teste på macOS ARM64, Windows x64 og den valgte understøttede Linux-arkitektur.
- Native AOT- og enkeltfilsmålet fra `TechStack.md` skal enten bestå publicering og grundlæggende funktionstest pr. platform eller ændres gennem en udtrykkeligt godkendt arkitekturbeslutning.
- WebView og øvrige native komponenter skal gennemgå grundlæggende funktionstest på hver målplatform.
- Alle manuelle valideringer fra feature 01–10 skal være godkendt før den samlede prøve.
