"""Beregn et lokalt sidelayout til slægtstræets generationsrækker."""

from dataclasses import dataclass
from pathlib import Path

from protocol import ProtocolError


@dataclass(frozen=True)
class CellLayout:
    lines: tuple[str, ...]
    spacer: bool = False


@dataclass(frozen=True)
class RowLayout:
    label: str
    vertical: bool
    width: float
    height: float
    cells: tuple[CellLayout, ...]


@dataclass(frozen=True)
class PageLayout:
    title: str
    paper: str
    font_size: int
    rows: tuple[RowLayout, ...]


def validate_document(document):
    if not isinstance(document, dict) or set(document) != {"title", "rows"}:
        raise ProtocolError("invalid-input")
    title = document["title"]
    rows = document["rows"]
    if not isinstance(title, str) or len(title) > 4096 or any(ord(c) < 32 for c in title):
        raise ProtocolError("invalid-input")
    if not isinstance(rows, list) or not 1 <= len(rows) <= 6:
        raise ProtocolError("invalid-input")
    for row in rows:
        if not isinstance(row, dict) or set(row) not in ({"label", "cells"}, {"label", "cells", "slots", "positions"}):
            raise ProtocolError("invalid-input")
        if not isinstance(row["label"], str) or not row["label"].strip() or len(row["label"]) > 256 or any(ord(c) < 32 for c in row["label"]):
            raise ProtocolError("invalid-input")
        cells = row["cells"]
        if not isinstance(cells, list) or not 1 <= len(cells) <= 1000:
            raise ProtocolError("invalid-input")
        slots = row.get("slots", len(cells))
        positions = row.get("positions")
        if type(slots) is not int or not len(cells) <= slots <= 1000:
            raise ProtocolError("invalid-input")
        if positions is not None and (not isinstance(positions, list) or len(positions) != len(cells)
                                      or any(type(position) is not int or position < 0 or position >= slots for position in positions)
                                      or positions != sorted(set(positions))):
            raise ProtocolError("invalid-input")
        if positions is None and slots != len(cells):
            raise ProtocolError("invalid-input")
        expected = (5, 6) if len(cells) <= 5 else (2, 3)
        for cell in cells:
            if not isinstance(cell, list) or len(cell) not in expected:
                raise ProtocolError("invalid-input")
            if any(not isinstance(line, str) or not line or len(line) > 4096 or
                   any(ord(character) < 32 for character in line) for line in cell):
                raise ProtocolError("invalid-input")


def _wrap(text, width, font):
    """Ombryd uden at udelade tegn, også når et ord er længere end cellen."""
    result = []
    remaining = text
    while remaining:
        if font.getlength(remaining) <= width:
            result.append(remaining)
            break
        low, high = 0, len(remaining)
        while low < high:
            middle = (low + high + 1) // 2
            if font.getlength(remaining[:middle]) <= width:
                low = middle
            else:
                high = middle - 1
        if low == 0:
            raise ProtocolError("page-overflow")
        split = remaining.rfind(" ", 0, low)
        if split > 0:
            # Mellemrum bevares på linjen, så den samlede tekst kan genskabes.
            split += 1
        else:
            split = low
        result.append(remaining[:split])
        remaining = remaining[split:]
    return result or [""]


def _wrap_cell(cell, width, font, maximum_lines=None):
    if maximum_lines is not None:
        # Undgå at opdele meget lange værdier, som alligevel ikke kan være i
        # cellen. En lille margin tager højde for tegnpar med anden bredde.
        if sum(font.getlength(value) for value in cell) > width * maximum_lines * 1.1:
            return None
    lines = []
    for value in cell:
        lines.extend(_wrap(value, width, font))
        if maximum_lines is not None and len(lines) > maximum_lines:
            return None
    return tuple(lines)


def build_layout(document, paper, font_path):
    validate_document(document)
    if paper not in ("A4", "A3"):
        raise ProtocolError("invalid-input")
    try:
        from PIL import ImageFont
    except ImportError:
        raise ProtocolError("missing-tool") from None
    if not Path(font_path).is_file():
        raise ProtocolError("missing-tool")

    # Alle mål er i punkter. 10 mm margin på hver side og ekstra plads til
    # kontorprogrammernes afsluttende afsnit.
    page_width, page_height = ((841.89, 595.28) if paper == "A4" else (1190.55, 841.89))
    usable_width = page_width - 56.70
    usable_height = page_height - 56.70
    for size in range(10, 3, -1):
        font = ImageFont.truetype(str(font_path), size * 4)
        line_height = size * 1.3
        rows = []
        total_height = size * 3 + 18 if document["title"] else 0
        try:
            if font.getlength(document["title"]) > usable_width * 4:
                raise ProtocolError("page-overflow")
            for row in document["rows"]:
                cells = row["cells"]
                slots = row.get("slots", len(cells))
                positions = row.get("positions")
                width = usable_width / slots
                vertical = slots > 8
                if vertical:
                    maximum_lines = int((width - 4) // line_height)
                    display_cells = [
                        [cell[0], cell[1] + "  " + cell[2], cell[3] + "  " + cell[4]]
                        if len(cell) == 5 and maximum_lines < 5 else cell
                        for cell in cells
                    ]
                    if maximum_lines < max(len(cell) for cell in display_cells):
                        raise ProtocolError("page-overflow")
                    # Find den kortest mulige tekstlængde, der rummer linjerne.
                    low, high = 1.0, usable_height
                    for _ in range(14):
                        middle = (low + high) / 2
                        wrapped = [_wrap_cell(cell, middle * 4, font, maximum_lines) for cell in display_cells]
                        if all(lines is not None for lines in wrapped):
                            high = middle
                        else:
                            low = middle
                    wrapped = [_wrap_cell(cell, high * 4, font, maximum_lines) for cell in display_cells]
                    if any(lines is None for lines in wrapped):
                        raise ProtocolError("page-overflow")
                    height = high + 6
                else:
                    if width <= 6:
                        raise ProtocolError("page-overflow")
                    wrapped = [_wrap_cell(cell, (width - 6) * 4, font) for cell in cells]
                    if any(lines is None for lines in wrapped):
                        raise ProtocolError("page-overflow")
                    height = max(len(lines) for lines in wrapped) * line_height + 6
                total_height += height
                positioned = [CellLayout((), True) for _ in range(slots)]
                for position, lines in zip(positions if positions is not None else range(slots), wrapped):
                    positioned[position] = CellLayout(lines)
                rows.append(RowLayout(row["label"], vertical, width, height, tuple(positioned)))
        except ProtocolError as error:
            if error.code == "page-overflow":
                continue
            raise
        if total_height <= usable_height:
            return PageLayout(document["title"], paper, size, tuple(rows))
    raise ProtocolError("page-overflow")
