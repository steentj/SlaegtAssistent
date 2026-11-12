using System.Globalization;
using System.Text;

namespace SlaegtsAssistent.Core.Graph.Export;

public enum GraphExportScope { WholeGraph, CurrentViewport }

public sealed record GraphExportOptions(GraphExportScope Scope, bool ShowNamesAndDates, string? ProbandId, GraphBounds? Viewport = null, GraphPoint? ViewportOrigin = null);
public sealed record GraphExportNode(string RecordId, GraphPoint Position, double Radius, string Fill, string? Label, bool IsProband, GraphPoint? LabelPosition = null, double LabelWidth = 0, double LabelHeight = 0);
public sealed record GraphExportLine(GraphPoint Start, GraphPoint End);
public sealed record GraphExportScene(GraphBounds Bounds, IReadOnlyList<GraphExportNode> Nodes, IReadOnlyList<GraphExportLine> Lines);

public sealed class GraphExportSceneBuilder
{
    public GraphExportScene Build(PersonGraph graph, PersonGraphLayout layout, GraphExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(options);
        if (options.Scope == GraphExportScope.CurrentViewport && options.Viewport is not { } viewport)
            throw new ArgumentException("Eksport af aktuelt udsnit kræver udsnittets grænser.", nameof(options));

        var bounds = options.Scope == GraphExportScope.WholeGraph
            ? new GraphBounds(layout.Bounds.Width, layout.Bounds.Height)
            : options.Viewport!.Value;
        ValidateBounds(bounds);
        var origin = options.Scope == GraphExportScope.CurrentViewport ? options.ViewportOrigin ?? new GraphPoint(0, 0) : new GraphPoint(0, 0);
        if (!double.IsFinite(origin.X) || !double.IsFinite(origin.Y)) throw new ArgumentException("Udsnittets placering er ugyldig.", nameof(options));
        var nodes = new List<GraphExportNode>();
        var positions = new Dictionary<string, GraphPoint>(StringComparer.Ordinal);
        foreach (var person in graph.Nodes.OrderBy(node => node.RecordId, StringComparer.Ordinal))
        {
            if (!layout.Positions.TryGetValue(person.RecordId, out var world)) continue;
            var point = new GraphPoint(world.X - origin.X, world.Y - origin.Y);
            if (options.Scope == GraphExportScope.CurrentViewport && !Inside(point, bounds)) continue;
            var proband = string.Equals(person.RecordId, options.ProbandId, StringComparison.Ordinal);
            var label = options.ShowNamesAndDates ? FormatLabel(person) : null;
            var fill = person.Sex?.Trim().ToUpperInvariant() switch
            {
                "F" => "#DC3C4D",
                "M" => "#3478D4",
                _ => "#8A8F98"
            };
            var radius = proband ? 8 : 4;
            nodes.Add(new GraphExportNode(person.RecordId, point, radius, fill, label, proband));
            positions.Add(person.RecordId, point);
        }

        var lines = new List<GraphExportLine>();
        foreach (var edge in graph.Edges.OrderBy(edge => edge.FirstId, StringComparer.Ordinal).ThenBy(edge => edge.SecondId, StringComparer.Ordinal))
        {
            if (!layout.Positions.TryGetValue(edge.FirstId, out var a) || !layout.Positions.TryGetValue(edge.SecondId, out var b)) continue;
            a = new GraphPoint(a.X - origin.X, a.Y - origin.Y);
            b = new GraphPoint(b.X - origin.X, b.Y - origin.Y);
            if (options.Scope == GraphExportScope.WholeGraph)
            {
                if (positions.ContainsKey(edge.FirstId) && positions.ContainsKey(edge.SecondId)) lines.Add(new(a, b));
            }
            else if (Clip(a, b, bounds, out var start, out var end)) lines.Add(new(start, end));
        }

        var padded = new GraphBounds(bounds.Width + 32, bounds.Height + 32);
        var shiftedNodes = nodes.Select(node => node with { Position = new GraphPoint(node.Position.X - bounds.Width / 2 + padded.Width / 2, node.Position.Y - bounds.Height / 2 + padded.Height / 2) }).ToArray();
        var shiftedLines = lines.Select(line => new GraphExportLine(
            new GraphPoint(line.Start.X - bounds.Width / 2 + padded.Width / 2, line.Start.Y - bounds.Height / 2 + padded.Height / 2),
            new GraphPoint(line.End.X - bounds.Width / 2 + padded.Width / 2, line.End.Y - bounds.Height / 2 + padded.Height / 2))).ToArray();
        return PlaceLabels(padded, shiftedNodes, shiftedLines);
    }

    private static GraphExportScene PlaceLabels(GraphBounds bounds, GraphExportNode[] nodes, GraphExportLine[] lines)
    {
        var placed = new List<(double X, double Y, double Width, double Height)>();
        var minX = 0d; var maxX = bounds.Width; var maxY = bounds.Height;
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node.Label is null) continue;
            var labelLines = node.Label.Split('\n');
            var width = Math.Max(12, labelLines.Max(line => line.Length) * 12d);
            var height = labelLines.Length * 16d;
            var x = node.Position.X - width / 2;
            var y = node.Position.Y + node.Radius + 4;
            foreach (var prior in placed.OrderBy(box => box.Y))
            {
                if (x < prior.X + prior.Width + 4 && x + width + 4 > prior.X && y < prior.Y + prior.Height + 4 && y + height + 4 > prior.Y)
                    y = prior.Y + prior.Height + 4;
            }
            placed.Add((x, y, width, height));
            nodes[i] = node with { LabelPosition = new GraphPoint(x, y), LabelWidth = width, LabelHeight = height };
            minX = Math.Min(minX, x - 8);
            maxX = Math.Max(maxX, x + width + 8);
            maxY = Math.Max(maxY, y + height + 8);
        }
        if (minX < 0)
        {
            var shift = -minX;
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                nodes[i] = node with { Position = new GraphPoint(node.Position.X + shift, node.Position.Y), LabelPosition = node.LabelPosition is { } label ? new GraphPoint(label.X + shift, label.Y) : null };
            }
            for (var i = 0; i < lines.Length; i++) lines[i] = new(new GraphPoint(lines[i].Start.X + shift, lines[i].Start.Y), new GraphPoint(lines[i].End.X + shift, lines[i].End.Y));
            maxX += shift;
        }
        return new GraphExportScene(new GraphBounds(maxX, maxY), Array.AsReadOnly(nodes), Array.AsReadOnly(lines));
    }

    private static string FormatLabel(PersonGraphNode person)
    {
        var name = string.IsNullOrWhiteSpace(person.Name) ? $"Unavngiven ({person.RecordId})" : person.Name.Trim();
        var dates = new List<string>();
        if (!string.IsNullOrWhiteSpace(person.BirthDate)) dates.Add($"Født: {person.BirthDate.Trim()}");
        if (!string.IsNullOrWhiteSpace(person.DeathDate)) dates.Add($"Død: {person.DeathDate.Trim()}");
        return dates.Count == 0 ? name : name + "\n" + string.Join(" · ", dates);
    }

    private static bool Inside(GraphPoint point, GraphBounds bounds) => point.X >= 0 && point.Y >= 0 && point.X <= bounds.Width && point.Y <= bounds.Height;

    private static void ValidateBounds(GraphBounds bounds)
    {
        if (!double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) || bounds.Width <= 0 || bounds.Height <= 0 || bounds.Width > 1_000_000 || bounds.Height > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Grafens udsnit er ugyldigt.");
    }

    private static bool Clip(GraphPoint a, GraphPoint b, GraphBounds bounds, out GraphPoint start, out GraphPoint end)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y; var low = 0d; var high = 1d;
        if (!Test(-dx, a.X, ref low, ref high) || !Test(dx, bounds.Width - a.X, ref low, ref high) ||
            !Test(-dy, a.Y, ref low, ref high) || !Test(dy, bounds.Height - a.Y, ref low, ref high))
        { start = default; end = default; return false; }
        start = new GraphPoint(a.X + low * dx, a.Y + low * dy);
        end = new GraphPoint(a.X + high * dx, a.Y + high * dy);
        return true;
    }

    private static bool Test(double p, double q, ref double low, ref double high)
    {
        if (p == 0) return q >= 0;
        var ratio = q / p;
        if (p < 0) { if (ratio > high) return false; low = Math.Max(low, ratio); }
        else { if (ratio < low) return false; high = Math.Min(high, ratio); }
        return true;
    }
}

public sealed class GraphSvgWriter
{
    public string Write(GraphExportScene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!double.IsFinite(scene.Bounds.Width) || !double.IsFinite(scene.Bounds.Height) || scene.Bounds.Width <= 0 || scene.Bounds.Height <= 0)
            throw new ArgumentException("Grafens størrelse er ugyldig.", nameof(scene));
        var culture = CultureInfo.InvariantCulture;
        var text = new StringBuilder();
        text.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
            .Append(scene.Bounds.Width.ToString("0.###", culture)).Append(' ').Append(scene.Bounds.Height.ToString("0.###", culture)).Append("\">\n")
            .Append("<rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"#FFFFFF\"/>\n")
            .Append("<g fill=\"none\" stroke=\"#777777\" stroke-width=\"1.5\">\n");
        foreach (var line in scene.Lines)
            text.Append("<line x1=\"").Append(F(line.Start.X)).Append("\" y1=\"").Append(F(line.Start.Y)).Append("\" x2=\"").Append(F(line.End.X)).Append("\" y2=\"").Append(F(line.End.Y)).Append("\"/>\n");
        text.Append("</g>\n");
        foreach (var node in scene.Nodes)
        {
            text.Append("<circle cx=\"").Append(F(node.Position.X)).Append("\" cy=\"").Append(F(node.Position.Y)).Append("\" r=\"").Append(F(node.Radius)).Append("\" fill=\"").Append(node.Fill).Append('"');
            if (node.IsProband) text.Append(" stroke=\"#000000\" stroke-width=\"2\"");
            text.Append("/>\n");
            if (node.Label is { } label)
            {
                var lines = label.Split('\n');
                var position = node.LabelPosition ?? new GraphPoint(node.Position.X, node.Position.Y + node.Radius + 4);
                for (var i = 0; i < lines.Length; i++)
                    text.Append("<text x=\"").Append(F(position.X)).Append("\" y=\"").Append(F(position.Y + 12 + i * 16)).Append("\" text-anchor=\"start\" fill=\"#202124\" font-family=\"sans-serif\" font-size=\"12\">").Append(XmlEscape(lines[i])).Append("</text>\n");
            }
        }
        text.Append("</svg>\n");
        return text.ToString();
    }

    private static string F(double value) => double.IsFinite(value) ? value.ToString("0.###", CultureInfo.InvariantCulture) : throw new ArgumentOutOfRangeException(nameof(value));
    private static string XmlEscape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;
}
