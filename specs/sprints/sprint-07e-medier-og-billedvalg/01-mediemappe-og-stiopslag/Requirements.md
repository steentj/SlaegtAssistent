# Krav: Mediemappe og entydigt stiopslag

Status: **planlagt**. Fælles kontrakt: [Mediekontrakt.md](../Mediekontrakt.md).

| Id | Acceptkriterium |
| --- | --- |
| M01 | Indstillinger gemmer, genåbner og kan rydde mediemappen; gamle JSON-filer uden feltet virker. |
| M02 | Opslag følger prioritet, entydighed, fysiske rødder og relativ sti fra Mediekontrakt.md. |
| M03 | Skift/annullering/gemmefejl ændrer ikke dokumenter og mister ikke ugemte buffers. |
| M04 | Import, skabelonkontekst, preview og eksport modtager samme aktuelle mediekontekst. |

Alle brugerrettede tekster er danske. Funktionaliteten arbejder lokalt og respekterer eksisterende atomisk gemning, editorcache og arbejdsområdespærring. Core og ViewModels må ikke få Avalonia-typer.
