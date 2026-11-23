"""Dan redigerbare generationstabeller med Pandoc og fast sidelayout."""

from html import escape
import os
from pathlib import Path
import subprocess
import sys
import xml.etree.ElementTree as ET
import zipfile

from protocol import ProtocolError

WORD = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"


def _tag(namespace, local):
    return f"{{{namespace}}}{local}"


def _html(layout):
    parts = ["<html><body>"]
    if layout.title:
        parts.append(f"<p>{escape(layout.title)}</p>")
    for row in layout.rows:
        parts.append("<table><tr>")
        for cell in row.cells:
            text = "<br/>".join(escape(line) for line in cell.lines)
            parts.append(f"<td>{text}</td>")
        parts.append("</tr></table>")
    parts.append("</body></html>")
    return "".join(parts)


def _pandoc(layout, output_path, extension, tool_root):
    tool_root = Path(tool_root)
    pandoc = tool_root / ("pandoc.exe" if os.name == "nt" else "pandoc")
    reference = tool_root / ("reference." + extension)
    if not pandoc.is_file() or not reference.is_file():
        raise ProtocolError("missing-tool")
    try:
        result = subprocess.run(
            [str(pandoc), "--from=html", "--to=" + extension, "--sandbox",
             "--resource-path=" + str(output_path.parent), "--reference-doc=" + str(reference),
             "--output=" + str(output_path)],
            input=_html(layout), text=True, cwd=output_path.parent,
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=120, check=False)
    except subprocess.TimeoutExpired:
        raise ProtocolError("timeout") from None
    if result.returncode != 0:
        raise ProtocolError("conversion-failed")


def _word_element(parent, name, **attributes):
    return ET.SubElement(parent, _tag(WORD, name), {_tag(WORD, key): str(value) for key, value in attributes.items()})


def _replace_word_child(parent, name, **attributes):
    tag = _tag(WORD, name)
    for existing in parent.findall(tag):
        parent.remove(existing)
    order = {
        "tblPr": ("tblStyle", "tblpPr", "tblOverlap", "bidiVisual", "tblStyleRowBandSize",
                  "tblStyleColBandSize", "tblW", "jc", "tblCellSpacing", "tblInd", "tblBorders",
                  "shd", "tblLayout", "tblCellMar", "tblLook", "tblCaption", "tblDescription"),
        "tcPr": ("cnfStyle", "tcW", "gridSpan", "hMerge", "vMerge", "tcBorders", "shd",
                 "noWrap", "tcMar", "textDirection", "tcFitText", "vAlign", "hideMark"),
        "sectPr": ("headerReference", "footerReference", "footnotePr", "endnotePr", "type",
                   "pgSz", "pgMar", "paperSrc", "pgBorders", "lnNumType", "pgNumType",
                   "cols", "formProt", "vAlign", "noEndnote", "titlePg", "textDirection",
                   "bidi", "rtlGutter", "docGrid", "printerSettings", "sectPrChange"),
        "pPr": ("pStyle", "keepNext", "keepLines", "pageBreakBefore", "framePr", "widowControl",
                "numPr", "suppressLineNumbers", "pBdr", "shd", "tabs", "suppressAutoHyphens",
                "kinsoku", "wordWrap", "overflowPunct", "topLinePunct", "autoSpaceDE",
                "autoSpaceDN", "bidi", "adjustRightInd", "snapToGrid", "spacing", "ind",
                "contextualSpacing", "mirrorIndents", "suppressOverlap", "jc", "textDirection",
                "textAlignment", "textboxTightWrap", "outlineLvl", "divId", "cnfStyle", "rPr"),
        "rPr": ("rStyle", "rFonts", "b", "bCs", "i", "iCs", "caps", "smallCaps", "strike",
                "dstrike", "outline", "shadow", "emboss", "imprint", "noProof", "snapToGrid",
                "vanish", "webHidden", "color", "spacing", "w", "kern", "position", "sz",
                "szCs", "highlight", "u", "effect", "bdr", "shd", "vertAlign", "rtl", "lang"),
    }
    local_parent = parent.tag.rsplit("}", 1)[-1]
    sequence = order.get(local_parent, ())
    element = ET.Element(tag, {_tag(WORD, key): str(value) for key, value in attributes.items()})
    if name in sequence:
        rank = sequence.index(name)
        for index, child in enumerate(parent):
            child_name = child.tag.rsplit("}", 1)[-1]
            if child_name in sequence and sequence.index(child_name) > rank:
                parent.insert(index, element)
                return element
    parent.append(element)
    return element


def _word_document(layout, xml):
    try:
        root = ET.fromstring(xml)
        body = root.find(_tag(WORD, "body"))
        if body is None:
            raise ProtocolError("conversion-failed")
        tables = body.findall(_tag(WORD, "tbl"))
        if len(tables) != len(layout.rows):
            raise ProtocolError("conversion-failed")
        for table, row in zip(tables, layout.rows):
            table_properties = table.find(_tag(WORD, "tblPr"))
            if table_properties is None:
                raise ProtocolError("conversion-failed")
            _replace_word_child(table_properties, "tblW", w=str(round(row.width * len(row.cells) * 20)), type="dxa")
            margins = _replace_word_child(table_properties, "tblCellMar")
            for side in ("top", "left", "bottom", "right"):
                _word_element(margins, side, w="30", type="dxa")
            grid = table.find(_tag(WORD, "tblGrid"))
            columns = [] if grid is None else grid.findall(_tag(WORD, "gridCol"))
            table_row = table.find(_tag(WORD, "tr"))
            cells = [] if table_row is None else table_row.findall(_tag(WORD, "tc"))
            if len(cells) != len(row.cells) or len(columns) != len(row.cells):
                raise ProtocolError("conversion-failed")
            row_properties = table_row.find(_tag(WORD, "trPr"))
            if row_properties is None:
                row_properties = ET.Element(_tag(WORD, "trPr"))
                table_row.insert(0, row_properties)
            _replace_word_child(row_properties, "trHeight", val=str(round(row.height * 20)), hRule="atLeast")
            width = str(round(row.width * 20))
            for column in columns:
                column.set(_tag(WORD, "w"), width)
            for cell, cell_layout in zip(cells, row.cells):
                properties = cell.find(_tag(WORD, "tcPr"))
                if properties is None:
                    raise ProtocolError("conversion-failed")
                _replace_word_child(properties, "tcW", w=width, type="dxa")
                if cell_layout.spacer:
                    borders = _replace_word_child(properties, "tcBorders")
                    for side in ("top", "left", "bottom", "right"):
                        _word_element(borders, side, val="nil")
                elif row.vertical:
                    _replace_word_child(properties, "textDirection", val="tbRl")
        section = body.find(_tag(WORD, "sectPr"))
        if section is None:
            raise ProtocolError("conversion-failed")
        width, height = (("16838", "11906") if layout.paper == "A4" else ("23811", "16838"))
        _replace_word_child(section, "pgSz", w=width, h=height, orient="landscape")
        _replace_word_child(section, "pgMar", top="567", right="567", bottom="567", left="567",
                            header="0", footer="0", gutter="0")
        for paragraph in root.iter(_tag(WORD, "p")):
            properties = paragraph.find(_tag(WORD, "pPr"))
            if properties is None:
                properties = ET.Element(_tag(WORD, "pPr"))
                paragraph.insert(0, properties)
            _replace_word_child(properties, "spacing", before="0", after="0",
                                line=str(round(layout.font_size * 1.3 * 20)), lineRule="exact")
        for run in root.iter(_tag(WORD, "r")):
            properties = run.find(_tag(WORD, "rPr"))
            if properties is None:
                properties = ET.Element(_tag(WORD, "rPr"))
                run.insert(0, properties)
            _replace_word_child(properties, "sz", val=str(layout.font_size * 2))
            _replace_word_child(properties, "szCs", val=str(layout.font_size * 2))
        return ET.tostring(root, encoding="utf-8", xml_declaration=True)
    except ET.ParseError:
        raise ProtocolError("conversion-failed") from None


def render_docx(layout, output_path, tool_root=None):
    output_path = Path(output_path)
    _pandoc(layout, output_path, "docx", tool_root or Path(sys.executable).resolve().parent)
    try:
        with zipfile.ZipFile(output_path) as source:
            entries = [(info, source.read(info.filename)) for info in source.infolist()]
        target = "word/document.xml"
        xml = next((data for info, data in entries if info.filename == target), None)
        if xml is None:
            raise ProtocolError("conversion-failed")
        updated = _word_document(layout, xml)
        with zipfile.ZipFile(output_path, "w") as destination:
            for info, data in entries:
                destination.writestr(info, updated if info.filename == target else data)
    except (OSError, zipfile.BadZipFile):
        raise ProtocolError("conversion-failed") from None

OFFICE = "urn:oasis:names:tc:opendocument:xmlns:office:1.0"
STYLE = "urn:oasis:names:tc:opendocument:xmlns:style:1.0"
TABLE = "urn:oasis:names:tc:opendocument:xmlns:table:1.0"
TEXT = "urn:oasis:names:tc:opendocument:xmlns:text:1.0"
FO = "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"


def _odf_style(styles, name, family):
    return ET.SubElement(styles, _tag(STYLE, "style"),
                         {_tag(STYLE, "name"): name, _tag(STYLE, "family"): family})


def _odt_content(layout, xml):
    try:
        root = ET.fromstring(xml)
        styles = root.find(_tag(OFFICE, "automatic-styles"))
        body = root.find("./" + _tag(OFFICE, "body") + "/" + _tag(OFFICE, "text"))
        if styles is None or body is None:
            raise ProtocolError("conversion-failed")
        tables = body.findall(_tag(TABLE, "table"))
        if len(tables) != len(layout.rows):
            raise ProtocolError("conversion-failed")
        paragraph_style = _odf_style(styles, "SlaegtParagraph", "paragraph")
        ET.SubElement(paragraph_style, _tag(STYLE, "paragraph-properties"),
                      {_tag(FO, "margin-top"): "0pt", _tag(FO, "margin-bottom"): "0pt",
                       _tag(FO, "line-height"): f"{layout.font_size * 1.3:.2f}pt"})
        ET.SubElement(paragraph_style, _tag(STYLE, "text-properties"),
                      {_tag(FO, "font-size"): f"{layout.font_size}pt",
                       _tag(FO, "font-family"): "sans-serif"})
        for paragraph in body.iter(_tag(TEXT, "p")):
            paragraph.set(_tag(TEXT, "style-name"), "SlaegtParagraph")
        for index, (table, row) in enumerate(zip(tables, layout.rows)):
            columns = table.findall(_tag(TABLE, "table-column"))
            table_row = table.find(_tag(TABLE, "table-row"))
            cells = [] if table_row is None else table_row.findall(_tag(TABLE, "table-cell"))
            if len(columns) != len(row.cells) or len(cells) != len(row.cells):
                raise ProtocolError("conversion-failed")
            column_style_name = f"SlaegtColumn{index}"
            column_style = _odf_style(styles, column_style_name, "table-column")
            ET.SubElement(column_style, _tag(STYLE, "table-column-properties"),
                          {_tag(STYLE, "column-width"): f"{row.width:.3f}pt"})
            for column in columns:
                column.set(_tag(TABLE, "style-name"), column_style_name)
            row_style_name = f"SlaegtRow{index}"
            row_style = _odf_style(styles, row_style_name, "table-row")
            ET.SubElement(row_style, _tag(STYLE, "table-row-properties"),
                          {_tag(STYLE, "min-row-height"): f"{row.height:.3f}pt"})
            table_row.set(_tag(TABLE, "style-name"), row_style_name)
            cell_style_name = f"SlaegtCell{index}"
            cell_style = _odf_style(styles, cell_style_name, "table-cell")
            properties = {_tag(FO, "padding"): "1.5pt", _tag(FO, "border"): "0.5pt solid #666666"}
            if row.vertical:
                properties[_tag(STYLE, "writing-mode")] = "tb-rl"
            ET.SubElement(cell_style, _tag(STYLE, "table-cell-properties"), properties)
            spacer_name = f"SlaegtSpacer{index}"
            spacer_style = _odf_style(styles, spacer_name, "table-cell")
            ET.SubElement(spacer_style, _tag(STYLE, "table-cell-properties"), {_tag(FO, "border"): "none"})
            for cell, cell_layout in zip(cells, row.cells):
                cell.set(_tag(TABLE, "style-name"), spacer_name if cell_layout.spacer else cell_style_name)
        return ET.tostring(root, encoding="utf-8", xml_declaration=True)
    except ET.ParseError:
        raise ProtocolError("conversion-failed") from None


def _odt_styles(layout, xml):
    try:
        root = ET.fromstring(xml)
        page_layouts = root.findall(".//" + _tag(STYLE, "page-layout-properties"))
        if not page_layouts:
            raise ProtocolError("conversion-failed")
        width, height = (("29.7cm", "21cm") if layout.paper == "A4" else ("42cm", "29.7cm"))
        for page in page_layouts:
            page.set(_tag(FO, "page-width"), width)
            page.set(_tag(FO, "page-height"), height)
            page.set(_tag(STYLE, "print-orientation"), "landscape")
            for side in ("top", "right", "bottom", "left"):
                page.set(_tag(FO, "margin-" + side), "1cm")
        for master in root.iter(_tag(STYLE, "master-page")):
            for child in list(master):
                if child.tag in (_tag(STYLE, "header"), _tag(STYLE, "footer")):
                    master.remove(child)
        return ET.tostring(root, encoding="utf-8", xml_declaration=True)
    except ET.ParseError:
        raise ProtocolError("conversion-failed") from None


def render_odt(layout, output_path, tool_root=None):
    output_path = Path(output_path)
    _pandoc(layout, output_path, "odt", tool_root or Path(sys.executable).resolve().parent)
    try:
        with zipfile.ZipFile(output_path) as source:
            entries = [(info, source.read(info.filename)) for info in source.infolist()]
        files = {info.filename: data for info, data in entries}
        if "content.xml" not in files or "styles.xml" not in files or "mimetype" not in files:
            raise ProtocolError("conversion-failed")
        files["content.xml"] = _odt_content(layout, files["content.xml"])
        files["styles.xml"] = _odt_styles(layout, files["styles.xml"])
        with zipfile.ZipFile(output_path, "w") as destination:
            destination.writestr("mimetype", files["mimetype"], compress_type=zipfile.ZIP_STORED)
            for info, data in entries:
                if info.filename != "mimetype":
                    destination.writestr(info, files.get(info.filename, data))
    except (OSError, zipfile.BadZipFile):
        raise ProtocolError("conversion-failed") from None
