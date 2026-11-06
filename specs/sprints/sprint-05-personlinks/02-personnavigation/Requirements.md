# Krav: Personnavigation

- Ét klik på et godkendt personlink eller en personknap åbner det refererede dokument i appens eksisterende arbejdsflade.
- ↗ vises kun i UI. Markdown, eksport og dokumentets gemmestatus ændres ikke af markøren eller navigationen.
- Navigation vælger efter dokumentsti/record-id, aldrig alene efter navn. Ens navne må ikke forveksles.
- Ugemt tekst bevares ved skift og er tilgængelig ved tilbagevenden via personlisten eller personlinks.
- Manglende, tvetydige og fejlbehæftede mål åbnes ikke. Vilkårlige lokale filer, netværksstier og URL-skemaer må ikke få udvidet adgang.
- Personknapper skal have dansk tekst, tydeligt navn, tooltip og almindelig tastaturaktivering.
- Ingen netværkskald; ViewModels er uden Avalonia-typer.
