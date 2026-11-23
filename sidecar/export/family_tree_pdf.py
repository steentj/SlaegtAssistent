"""Tegn et generationslayout som en enkelt PDF-side."""

from html import escape

from protocol import ProtocolError


def _text(value):
    return escape(value)


def render_pdf(layout, output_path):
    try:
        from weasyprint import CSS, HTML
    except (ImportError, OSError):
        raise ProtocolError("missing-tool") from None

    page_width, page_height = ((841.89, 595.28) if layout.paper == "A4" else (1190.55, 841.89))
    margin = 28.35
    font_size = layout.font_size
    line_height = font_size * 1.3
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {page_width} {page_height}" '
        f'width="{page_width}pt" height="{page_height}pt">',
        f'<rect x="0" y="0" width="{page_width}" height="{page_height}" fill="white"/>',
        f'<g font-family="sans-serif" font-size="{font_size}" fill="#000">',
        f'<text x="{margin}" y="{margin + font_size}">{_text(layout.title)}</text>',
    ]
    y = margin + font_size * 3 + 18
    for row in layout.rows:
        parts.append(f'<text x="{margin}" y="{y + font_size}">{_text(row.label)}</text>')
        y += line_height + 5
        for index, cell in enumerate(row.cells):
            x = margin + index * row.width
            parts.append(f'<rect x="{x:.3f}" y="{y:.3f}" width="{row.width:.3f}" height="{row.height:.3f}" fill="none" stroke="#666" stroke-width="0.5"/>')
            if row.vertical:
                parts.append(f'<g transform="translate({x + row.width - 3:.3f} {y + 3:.3f}) rotate(90)">')
                for line_index, line in enumerate(cell.lines):
                    parts.append(f'<text x="0" y="{font_size + line_index * line_height:.3f}" xml:space="preserve">{_text(line)}</text>')
                parts.append('</g>')
            else:
                for line_index, line in enumerate(cell.lines):
                    baseline = y + 3 + font_size + line_index * line_height
                    parts.append(f'<text x="{x + 3:.3f}" y="{baseline:.3f}" xml:space="preserve">{_text(line)}</text>')
        y += row.height
    parts.append('</g></svg>')
    html = '<html><body>' + ''.join(parts) + '</body></html>'

    def deny_resource(url, *args, **kwargs):
        raise ProtocolError("resource-blocked")

    css = f'@page {{ size: {layout.paper} landscape; margin: 0; }} html, body {{ margin: 0; padding: 0; }} svg {{ display: block; }}'
    document = HTML(string=html, url_fetcher=deny_resource).render(
        stylesheets=[CSS(string=css, url_fetcher=deny_resource)])
    if len(document.pages) != 1:
        raise ProtocolError("page-overflow")
    document.write_pdf(str(output_path))
