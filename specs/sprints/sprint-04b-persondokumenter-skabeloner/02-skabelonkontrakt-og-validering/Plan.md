# Plan: Skabelonkontrakt og validering

1. Skriv failing Core-tests for felter, `if`, `each`, tomme værdier og Markdown-escaping.
2. Fastlæg den offentlige skabelonkontekst og syntaks.
3. Implementér loader med UTF-8 og sikker syntaksanalyse.
4. Implementér validering med linje- og kolonnefejl.
5. Test deterministisk og uden adgang til vilkårlig programkode.
6. Stop for manuel afprøvning af gyldige og ugyldige skabeloner.
