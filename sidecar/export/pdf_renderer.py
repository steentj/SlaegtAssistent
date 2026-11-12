import re
import xml.etree.ElementTree as ET

from protocol import ProtocolError

SVG = "{http://www.w3.org/2000/svg}"
ALLOWED = {
    SVG + "svg": {"viewBox", "width", "height", "preserveAspectRatio"},
    SVG + "g": {"fill", "stroke", "stroke-width"},
    SVG + "rect": {"x", "y", "width", "height", "fill"},
    SVG + "line": {"x1", "y1", "x2", "y2", "stroke", "stroke-width"},
    SVG + "circle": {"cx", "cy", "r", "fill", "stroke", "stroke-width"},
    SVG + "text": {"x", "y", "fill", "font-size", "text-anchor", "font-family"},
}
VALUE = re.compile(r"[A-Za-z0-9#.,% +\-]+\Z")


def validate_html(content):
    if "<!" in content or "<?" in content or "&" in re.sub(r"&(?:amp|lt|gt|quot|apos|#[0-9]+|#x[0-9a-fA-F]+);", "", content):
        raise ProtocolError("resource-blocked")
    try:
        root = ET.fromstring(content)
    except ET.ParseError:
        raise ProtocolError("invalid-input") from None
    if root.tag != "html" or root.attrib or len(root) != 1 or root[0].tag != "body" or root[0].attrib or len(root[0]) != 1 or root[0][0].tag != SVG + "svg":
        raise ProtocolError("resource-blocked")
    for element in root[0][0].iter():
        allowed = ALLOWED.get(element.tag)
        if allowed is None or any(key not in allowed or not VALUE.fullmatch(value) for key, value in element.attrib.items()):
            raise ProtocolError("resource-blocked")
        if element.tag != SVG + "text" and element.text and element.text.strip():
            raise ProtocolError("invalid-input")
        if element.tail and element.tail.strip():
            raise ProtocolError("invalid-input")
    return content


def render_pdf(input_path, output_path, options):
    content = validate_html(input_path.read_text(encoding="utf-8"))
    try:
        from weasyprint import CSS, HTML
    except (ImportError, OSError):
        raise ProtocolError("missing-tool") from None

    def deny_resource(url, *args, **kwargs):
        raise ProtocolError("resource-blocked")

    css = f'@page {{ size: {options["paper"]} {options["orientation"]}; margin: 10mm; }} html, body {{ margin: 0; padding: 0; }} svg {{ width: 100%; height: 100%; }}'
    HTML(string=content, url_fetcher=deny_resource).write_pdf(str(output_path), stylesheets=[CSS(string=css, url_fetcher=deny_resource)])
