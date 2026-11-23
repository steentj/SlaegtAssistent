# Validering af eksportgrundlaget

Status: Implementeret og prøvet med automatiske tests. Den samlede manuelle validering nedenfor udestår.

Automatiske prøver dækker valg af dyb rod, ny nummerering, anesammenfald, sammenfald af efterkommere, cyklusstop, ugyldige niveauer og grænserne for celleformatering. En headless-prøve kontrollerer dialogens binding til generationstal.

- Byg en syntetisk fixture med mindst syv kendte led på begge sider af roden, to forældre, anesammenfald, genbrug af efterkommer via to stier, en manglende forælder og en cyklus. Bevis korrekt rækkefølge, øvre grænse, gruppering af anenumre og udelukkelse af cykliske led.
- Prøv 0, 1 og 5 led i én retning. Prøv 3 + 2, 1 + 4 og det ugyldige 3 + 3 i »Begge«. Kontrollér standardfordeling, hvis én side har færre led, og at roden altid tælles med.
- Brug en ane dybere end sjette generation som eksportrod. Kontrollér, at opslaget virker med lukket gren, dokumentets rod er 1, dens forældre er 2 og 3, overskriften viser oprindeligt nummer og hovedtræets proband og udfoldning er uændrede.
- Byg rækker med 4, 5, 6, 8 og 9 celler og afprøv alle tekstfelter og orienteringer fra E3, inklusive en »Ukendt«-celle ved en tærskel.
- Simulér ændret proband, nyt arbejdsområde og import efter start af et job. Den påbegyndte eksport holder sig til det bekræftede grundlag. Et nyt job bruger den nye visning.
