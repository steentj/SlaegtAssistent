namespace SlaegtsAssistent.Core.Graph;

public sealed class GraphSpatialIndex
{
    private const double CellSize = 64;
    private readonly Dictionary<(long X, long Y), List<(string Id, GraphPoint Point)>> _cells = [];

    public GraphSpatialIndex(IReadOnlyDictionary<string, GraphPoint> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        foreach (var (id, point) in positions)
        {
            var cell = Cell(point);
            if (!_cells.TryGetValue(cell, out var entries)) _cells[cell] = entries = [];
            entries.Add((id, point));
        }
    }

    public string? FindNearest(GraphPoint point, double radius)
    {
        if (!double.IsFinite(radius) || radius < 0 || radius > 10000) throw new ArgumentOutOfRangeException(nameof(radius));
        var low = Cell(new GraphPoint(point.X - radius, point.Y - radius));
        var high = Cell(new GraphPoint(point.X + radius, point.Y + radius));
        string? best = null;
        var bestDistance = radius * radius;
        for (var x = low.X; x <= high.X; x++)
            for (var y = low.Y; y <= high.Y; y++)
                if (_cells.TryGetValue((x, y), out var entries))
                    foreach (var (id, candidate) in entries)
                    {
                        var dx = candidate.X - point.X;
                        var dy = candidate.Y - point.Y;
                        var distance = dx * dx + dy * dy;
                        if (distance < bestDistance || distance == bestDistance && (best is null || StringComparer.Ordinal.Compare(id, best) < 0))
                        {
                            best = id;
                            bestDistance = distance;
                        }
                    }
        return best;
    }

    private static (long X, long Y) Cell(GraphPoint point)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y) || Math.Abs(point.X) > 1_000_000_000 || Math.Abs(point.Y) > 1_000_000_000)
            throw new ArgumentOutOfRangeException(nameof(point));
        return ((long)Math.Floor(point.X / CellSize), (long)Math.Floor(point.Y / CellSize));
    }
}
