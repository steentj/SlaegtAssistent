# Plan: Resizable arbejdsbordspaneler

1. Skriv failing App-tests eller viewmodel-tests for paneltilstand og bevaret layout.
2. Indfør en flytbar `GridSplitter` mellem editor/preview og kontekstpanelet.
3. Definér minimumsbredder, så editor, preview og rå GEDCOM-data fortsat er anvendelige.
4. Sørg for, at højre panels rå GEDCOM-tekstboks udfylder og resizes med panelet.
5. Gem eventuelt brugerens panelbredde lokalt, hvis Avalonia-layoutet kræver persistens for genåbning.
6. Stop for manuel validering af flytning, resizing, scroll og editorens fortsatte funktion.
