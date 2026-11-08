# Requirements: Lagring og fejlhåndtering

- **Gem** og **Gem som** skal skrive atomisk og validere den genindlæste fil før aktivering.
- **Annuller** skal efterlade fil, indstillinger og aktive dokumenter uændrede.
- En skrive- eller valideringsfejl skal bevare den tidligere aktive skabelon.
- Skabeloner i det nye format skal genåbnes med samme editormodel.
- Hvis indstillinger peger på det tidligere format, skal appen forklare, at det ikke understøttes, og tilbyde en ny standardskabelon.
- Der kræves ikke import, konvertering eller fortsat brug af det tidligere format.
- En gemt skabelonændring skal fortsat følge kandidatflowet og må ikke omskrive persondokumenter direkte.
