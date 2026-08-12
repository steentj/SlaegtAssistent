# Plan: Stabil dokumentidentitet og levende arbejdsområde

1. Skriv fejlende tests for navneændring, dubleret `recordId`, ikke-matchede dokumenter og to importer i samme session.
2. Skriv fejlende tests for mappeskift med og uden dirty editorer samt kontrol af, at gammel mappe ikke skrives.
3. Definér arbejdsområde- og dokumentidentitetsmodellen med stabil filsti pr. dokument.
4. Implementér et levende katalog, som alle oprettelser og migreringer opdaterer.
5. Kobl personliste, editor-cache, snapshot og import til det aktive arbejdsområde.
6. Implementér tydelig tvetydig status og blokér automatisk match ved dubletter.
7. Ret eksisterende tests, der forventer, at ikke-matchede dokumenter forsvinder.
8. Kør målrettede tests, hele testpakken og build.
9. Gennemfør manuel test af navneændring, to importer og mappe A til mappe B.
10. Dokumentér resultatet, og stop før feature 03.
