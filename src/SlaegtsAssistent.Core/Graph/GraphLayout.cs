using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace SlaegtsAssistent.Core.Graph;

public readonly record struct GraphPoint(double X, double Y);
public readonly record struct GraphBounds(double Width, double Height);
public sealed record GraphDotInput(string Dot, IReadOnlyDictionary<string, string> NodeIds);
public sealed record PersonGraphLayout(GraphBounds Bounds, IReadOnlyDictionary<string, GraphPoint> Positions);

public sealed class GraphDotWriter
{
    public GraphDotInput Write(PersonGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var nodes = graph.Nodes.OrderBy(node => node.RecordId, StringComparer.Ordinal).ToArray();
        var ids = nodes.Select((node, index) => (node.RecordId, Id: "n" + index.ToString(CultureInfo.InvariantCulture)))
            .ToDictionary(item => item.RecordId, item => item.Id, StringComparer.Ordinal);
        var text = new StringBuilder("graph {\n  graph [start=42, overlap=prism, splines=line];\n  node [shape=circle, label=\"\", fixedsize=true, width=0.12, height=0.12];\n");
        foreach (var node in nodes) text.Append("  ").Append(ids[node.RecordId]).Append(";\n");
        foreach (var edge in graph.Edges.OrderBy(edge => edge.FirstId, StringComparer.Ordinal).ThenBy(edge => edge.SecondId, StringComparer.Ordinal))
            text.Append("  ").Append(ids[edge.FirstId]).Append(" -- ").Append(ids[edge.SecondId]).Append(";\n");
        text.Append("}\n");
        var lookup = ids.ToDictionary(entry => entry.Value, entry => entry.Key, StringComparer.Ordinal);
        return new GraphDotInput(text.ToString(), new ReadOnlyDictionary<string, string>(lookup));
    }
}

public sealed class GraphPlainParser
{
    private const int MaximumLength = 32 * 1024 * 1024;

    public PersonGraphLayout Parse(string output, GraphDotInput input)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(input);
        if (output.Length > MaximumLength) throw new FormatException("Graphviz-output er for stort.");
        var positions = new Dictionary<string, GraphPoint>(StringComparer.Ordinal);
        double width = 0, height = 0;
        var graphSeen = false;
        var stopped = false;
        using var reader = new StringReader(output);
        while (reader.ReadLine() is { } line)
        {
            var tokens = Tokenize(line);
            if (tokens.Count == 0) continue;
            if (stopped) throw new FormatException("Graphviz-output indeholder data efter afslutningen.");
            switch (tokens[0])
            {
                case "graph":
                    if (graphSeen || tokens.Count != 4 || Number(tokens[1]) != 1) throw new FormatException("Ugyldige grafdimensioner.");
                    width = Number(tokens[2]); height = Number(tokens[3]);
                    if (width <= 0 || height <= 0) throw new FormatException("Grafens dimensioner skal være positive.");
                    graphSeen = true;
                    break;
                case "node":
                    if (!graphSeen || tokens.Count < 11 || !input.NodeIds.TryGetValue(tokens[1], out var id)) throw new FormatException("Ugyldig grafnode.");
                    var x = Number(tokens[2]); var y = Number(tokens[3]);
                    if (!positions.TryAdd(id, new GraphPoint(x * 96, (height - y) * 96))) throw new FormatException("En grafnode forekommer flere gange.");
                    break;
                case "edge":
                    if (!graphSeen || tokens.Count < 7 || !input.NodeIds.ContainsKey(tokens[1]) || !input.NodeIds.ContainsKey(tokens[2])) throw new FormatException("Ugyldig grafforbindelse.");
                    if (!int.TryParse(tokens[3], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 0 || count > (tokens.Count - 6) / 2 || tokens.Count != 6 + 2 * count)
                        throw new FormatException("Ugyldige forbindelseskoordinater.");
                    for (var index = 4; index < 4 + 2 * count; index++) _ = Number(tokens[index]);
                    break;
                case "stop":
                    if (!graphSeen || tokens.Count != 1) throw new FormatException("Ugyldig grafafslutning.");
                    stopped = true;
                    break;
                default: throw new FormatException("Ukendt linje i Graphviz-output.");
            }
        }
        if (!stopped || positions.Count != input.NodeIds.Count) throw new FormatException("Graphviz-output er ufuldstændigt.");
        return new PersonGraphLayout(new GraphBounds(width * 96, height * 96), new ReadOnlyDictionary<string, GraphPoint>(positions));
    }

    private static double Number(string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number) || Math.Abs(number) > 1_000_000)
            throw new FormatException("Ugyldigt tal i Graphviz-output.");
        return number;
    }

    private static List<string> Tokenize(string line)
    {
        var result = new List<string>();
        var index = 0;
        while (index < line.Length)
        {
            while (index < line.Length && char.IsWhiteSpace(line[index])) index++;
            if (index == line.Length) break;
            var token = new StringBuilder();
            var quoted = line[index] == '"';
            if (quoted) index++;
            var closed = !quoted;
            while (index < line.Length)
            {
                var ch = line[index++];
                if (quoted && ch == '"') { closed = true; break; }
                if (!quoted && char.IsWhiteSpace(ch)) break;
                if (quoted && ch == '\\' && index < line.Length) ch = line[index++];
                token.Append(ch);
            }
            if (!closed || (quoted && index < line.Length && !char.IsWhiteSpace(line[index]))) throw new FormatException("Ugyldigt citeret token i Graphviz-output.");
            result.Add(token.ToString());
        }
        return result;
    }
}
