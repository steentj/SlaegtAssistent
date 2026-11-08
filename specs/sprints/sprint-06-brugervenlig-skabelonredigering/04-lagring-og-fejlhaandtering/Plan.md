# Plan: Lagring og fejlhåndtering

1. Kortlæg projektets atomiske filskrivning og genbrug samme abstraktion og gendannelsespolitik til skabeloner.
2. Skriv integrationstests for ny fil, overskrivning, **Gem som**, annullering, adgangsfejl, afbrudt skrivning, ugyldig genindlæsning og en sti til det tidligere format.
3. Implementér tofaset gemning: skriv sikkert, genindlæs og validér, og opdatér først derefter `AppSettings`.
4. Implementér overgangsbeskeden og oprettelse af en ny standardskabelon, når det tidligere format findes.
5. Integrér den gemte model med hash-, importpreflight- og kandidatflow.
6. Gennemfør manuel fejlindsprøjtning med testfiler og stop for godkendelse.
