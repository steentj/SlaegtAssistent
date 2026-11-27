using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Views.Markdown;

internal readonly record struct MarkdownRange(int Start, int End)
{
    public int Length => End - Start;

    public bool Contains(int offset) => Start <= offset && offset < End;

    public bool Overlaps(int start, int end) => Start < end && start < End;
}

internal enum MarkdownAppearance
{
    Plain, Bold, Italic, Strike, Highlight, Inserted, Superscript, Subscript, Code, Link, Muted, Heading, Table
}

internal sealed record MarkdownImage(string Source, string AlternativeText, double WidthFraction);

internal sealed record MarkdownDecoration(
    MarkdownRange Range, MarkdownAppearance Appearance, string? Replacement, double Scale = 1);

internal sealed record MarkdownElement(
    MarkdownRange Range, IReadOnlyList<MarkdownDecoration> Decorations, MarkdownImage? Image = null);

internal sealed class MarkdownEditorProjection(string source)
{
    private readonly List<MarkdownElement> _elements = [];

    public static IReadOnlyList<MarkdownElement> Parse(string source)
    {
        var projection = new MarkdownEditorProjection(source);
        foreach (var block in Markdig.Markdown.Parse(source, SafeMarkdownPreviewService.Pipeline))
            projection.VisitBlock(block);
        return projection._elements;
    }

    private void VisitBlock(Block block)
    {
        var range = RangeOf(block);
        switch (block)
        {
            case HeadingBlock { Inline: { } title } heading:
                var text = RangeOf(title);
                var end = heading.IsSetext ? range.End : EndOfLine(range.End);
                Add(new(range.Start, end),
                    Replace(range.Start, text.Start),
                    Format(text, MarkdownAppearance.Heading, 1 + (7 - heading.Level) * 0.13),
                    Replace(text.End, end));
                break;
            case ListBlock list:
                var number = int.TryParse(list.OrderedStart, CultureInfo.InvariantCulture, out var start) ? start : 1;
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    var marker = item.Span.Start;
                    var endOfLine = EndOfLine(marker);
                    var afterMarker = marker;
                    while (afterMarker < endOfLine && !char.IsWhiteSpace(source[afterMarker]))
                        afterMarker++;
                    while (afterMarker < endOfLine && IsHorizontalSpace(source[afterMarker]))
                        afterMarker++;
                    var bullet = list.IsOrdered ? (number++).ToString(CultureInfo.InvariantCulture) + ".  " : "•  ";
                    Add(new(marker, endOfLine), Replace(marker, afterMarker, bullet, MarkdownAppearance.Muted));
                }
                break;
            case QuoteBlock { Parent: not QuoteBlock }:
                for (var offset = range.Start; offset < range.End; offset = NextLine(EndOfLine(offset)))
                {
                    var markerEnd = offset;
                    while (markerEnd < range.End && IsHorizontalSpace(source[markerEnd])) markerEnd++;
                    var firstMarker = markerEnd;
                    var depth = 0;
                    while (markerEnd < range.End && source[markerEnd] == '>')
                    {
                        depth++;
                        markerEnd++;
                        while (markerEnd < range.End && IsHorizontalSpace(source[markerEnd])) markerEnd++;
                    }
                    var endOfLine = EndOfLine(offset);
                    if (depth > 0)
                        Add(new(firstMarker, endOfLine), Replace(firstMarker, markerEnd, string.Concat(Enumerable.Repeat("▏ ", depth)), MarkdownAppearance.Muted));
                }
                break;
            case FencedCodeBlock fence:
                var openingEnd = EndOfLine(range.Start);
                var close = range.End;
                if (fence.ClosingFencedCharCount > 0)
                {
                    while (close > range.Start && IsHorizontalSpace(source[close - 1])) close--;
                    close -= fence.ClosingFencedCharCount;
                }
                Add(range, Replace(range.Start, openingEnd), Format(new(NextLine(openingEnd), close), MarkdownAppearance.Code),
                    Replace(close, range.End));
                break;
            case CodeBlock:
                Add(range, Format(range, MarkdownAppearance.Code));
                break;
            case ThematicBreakBlock:
                Add(range, Replace(range.Start, range.End, "━━━━━━━━━━━━━━━━━━━━", MarkdownAppearance.Muted));
                break;
            case LinkReferenceDefinition:
                Add(range, Replace(range.Start, range.End));
                break;
            case Table table:
                AddTable(table);
                return;
            case ParagraphBlock:
                AddDocumentMarkers(range);
                break;
        }

        if (block is ContainerBlock container)
            foreach (var child in container)
                VisitBlock(child);

        if (block is LeafBlock { Inline: { } inline })
            foreach (var child in inline)
                VisitInline(child);
    }

    private void VisitInline(Inline inline)
    {
        var range = RangeOf(inline);
        if (!IsValid(range)) return;

        switch (inline)
        {
            case EmphasisInline emphasis:
                var appearance = emphasis.DelimiterChar switch
                {
                    '*' or '_' => emphasis.DelimiterCount == 2 ? MarkdownAppearance.Bold : MarkdownAppearance.Italic,
                    '~' => emphasis.DelimiterCount == 2 ? MarkdownAppearance.Strike : MarkdownAppearance.Subscript,
                    '^' => MarkdownAppearance.Superscript,
                    '+' => MarkdownAppearance.Inserted,
                    '=' => MarkdownAppearance.Highlight,
                    _ => MarkdownAppearance.Plain
                };
                if (appearance != MarkdownAppearance.Plain)
                    AddDelimited(range, emphasis.DelimiterCount, appearance);
                break;
            case CodeInline code:
                var open = range.Start + code.DelimiterCount;
                var close = range.End - code.DelimiterCount;
                if (open < close && source[open] == ' ' && source[close - 1] == ' ' &&
                    source.AsSpan(open, close - open).Trim(' ').Length > 0)
                {
                    open++;
                    close--;
                }
                Add(range, Replace(range.Start, open), Format(new(open, close), MarkdownAppearance.Code), Replace(close, range.End));
                break;
            case LinkInline { IsImage: true } image:
                if ((image.GetDynamicUrl?.Invoke() ?? image.Url) is { Length: > 0 } imageSource)
                {
                    var alternative = string.Concat(image.Descendants().Select(child => child switch
                    {
                        LiteralInline literal => literal.Content.ToString(),
                        CodeInline codeText => codeText.Content,
                        HtmlEntityInline entity => entity.Transcoded.ToString(),
                        LineBreakInline => " ",
                        _ => string.Empty
                    }));
                    var width = image.Title switch
                    {
                        "SlaegtsAssistent:width=25" => 0.25,
                        "SlaegtsAssistent:width=50" => 0.5,
                        _ => 1.0
                    };
                    _elements.Add(new(range, [], new MarkdownImage(imageSource, alternative, width)));
                }
                return;
            case LinkInline { IsAutoLink: false, FirstChild: { } first, LastChild: { } last }
                when source[range.Start] == '[' && first.Span.Start > range.Start && last.Span.End < range.End:
                var label = new MarkdownRange(first.Span.Start, last.Span.End + 1);
                Add(range, Replace(range.Start, label.Start), Format(label, MarkdownAppearance.Link), Replace(label.End, range.End));
                break;
            case LinkInline:
                Add(range, Format(range, MarkdownAppearance.Link));
                break;
            case AutolinkInline:
                AddDelimited(range, 1, MarkdownAppearance.Link);
                break;
            case HtmlEntityInline entity:
                Add(range, Replace(range.Start, range.End, entity.Transcoded.ToString()));
                break;
            case LiteralInline literal when source[range.Start] == '\\' && range.Length == 2 && literal.Content.Length == 1:
                Add(range, Replace(range.Start, range.Start + 1));
                break;
        }

        if (inline is ContainerInline container)
            foreach (var child in container)
                VisitInline(child);
    }

    private void AddTable(Table table)
    {
        var rows = new List<(MarkdownRange Range, bool IsHeader, MarkdownRange[] Cells, int[] Widths)>();
        var widths = new int[table.ColumnDefinitions.Count];
        foreach (var row in table.OfType<TableRow>())
        {
            var rowRange = RangeOf(row);
            if (!IsValid(rowRange)) continue;

            var cells = new List<MarkdownRange>();
            var cellWidths = new List<int>();
            foreach (var cell in row.OfType<TableCell>())
            {
                var firstElement = _elements.Count;
                VisitBlock(cell);
                var range = RangeOf(cell);
                if (range.Start < rowRange.Start || range.End > rowRange.End || range.Length < 0) continue;

                var start = range.Start;
                var end = range.End;
                while (start < end && IsHorizontalSpace(source[start])) start++;
                while (end > start && IsHorizontalSpace(source[end - 1])) end--;
                var text = new MarkdownRange(start, end);
                var width = text.Length;
                for (var i = firstElement; i < _elements.Count; i++)
                    foreach (var part in _elements[i].Decorations)
                        if (part.Replacement is { } replacement && part.Range.Start >= start && part.Range.End <= end)
                            width += replacement.Length - part.Range.Length;
                if (cells.Count < widths.Length)
                    widths[cells.Count] = Math.Max(widths[cells.Count], width);
                cells.Add(text);
                cellWidths.Add(width);
            }
            if (cells.Count == 0) continue;
            var rowStart = rowRange.Start;
            if (rowStart > 0 && source[rowStart - 1] == '|') rowStart--;
            rows.Add((new(rowStart, EndOfLine(rowRange.End)), row.IsHeader, cells.ToArray(), cellWidths.ToArray()));
        }

        foreach (var row in rows)
        {
            if (row.IsHeader)
            {
                var separatorStart = NextLine(row.Range.End);
                var separatorEnd = EndOfLine(separatorStart);
                var separator = new MarkdownRange(separatorStart, separatorEnd);
                if (separator.Length > 0)
                    Add(separator, Replace(separatorStart, separatorEnd, new string('─', Math.Clamp(widths.Sum() + (widths.Length - 1) * 3, 3, 120)), MarkdownAppearance.Muted));
            }

            var decorations = new List<MarkdownDecoration> { Format(row.Range, MarkdownAppearance.Table) };
            var previous = row.Range.Start;
            for (var index = 0; index < row.Cells.Length; index++)
            {
                var cell = row.Cells[index];
                var separator = string.Empty;
                if (index > 0)
                {
                    var padding = index <= widths.Length ? Math.Clamp(widths[index - 1] - row.Widths[index - 1], 0, 60) : 0;
                    separator = new string(' ', padding + 1) + "│ ";
                }
                decorations.Add(Replace(previous, cell.Start, separator, MarkdownAppearance.Muted));
                if (row.IsHeader) decorations.Add(Format(cell, MarkdownAppearance.Bold));
                previous = cell.End;
            }
            decorations.Add(Replace(previous, row.Range.End));
            Add(row.Range, decorations.ToArray());
        }
    }

    private void AddDocumentMarkers(MarkdownRange range)
    {
        for (var offset = StartOfLine(range.Start); offset < range.End; offset = NextLine(EndOfLine(offset)))
        {
            var end = EndOfLine(offset);
            var line = source.AsSpan(offset, end - offset).TrimEnd();
            if (line.SequenceEqual(BiographyImageEditService.PortraitStartMarker) ||
                line.SequenceEqual(BiographyImageEditService.PortraitEndMarker) ||
                line.SequenceEqual(BiographyGeneratedSectionMerger.StartMarker) ||
                line.SequenceEqual(BiographyGeneratedSectionMerger.EndMarker))
                Add(new(offset, end), Replace(offset, end));
        }
    }

    private void AddDelimited(MarkdownRange range, int count, MarkdownAppearance appearance) =>
        Add(range, Replace(range.Start, range.Start + count), Format(new(range.Start + count, range.End - count), appearance),
            Replace(range.End - count, range.End));

    private void Add(MarkdownRange range, params MarkdownDecoration[] parts)
    {
        if (!IsValid(range)) return;
        var valid = parts.Where(part => IsValid(part.Range)).ToArray();
        if (valid.Length > 0) _elements.Add(new(range, valid));
    }

    private bool IsValid(MarkdownRange range) => range.Start >= 0 && range.End <= source.Length && range.Length > 0;

    private static MarkdownRange RangeOf(MarkdownObject node) => new(node.Span.Start, node.Span.End + 1);

    private static MarkdownDecoration Format(MarkdownRange range, MarkdownAppearance appearance, double scale = 1) =>
        new(range, appearance, null, scale);

    private static MarkdownDecoration Replace(int start, int end, string text = "", MarkdownAppearance appearance = MarkdownAppearance.Plain) =>
        new(new(start, end), appearance, text);

    private int StartOfLine(int offset)
    {
        while (offset > 0 && source[offset - 1] is not ('\r' or '\n')) offset--;
        return offset;
    }

    private int EndOfLine(int offset)
    {
        while (offset < source.Length && source[offset] is not ('\r' or '\n')) offset++;
        return offset;
    }

    private int NextLine(int offset)
    {
        if (offset < source.Length && source[offset] == '\r') offset++;
        if (offset < source.Length && source[offset] == '\n') offset++;
        return offset;
    }

    private static bool IsHorizontalSpace(char character) => character is ' ' or '\t';
}
