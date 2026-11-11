using FluentAssertions;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GraphSpatialIndexTests
{
    [Fact]
    public void FinderNærmestePunktPåTværsAfCellegrænserOgBryderLighedMedId()
    {
        var index = new GraphSpatialIndex(new Dictionary<string, GraphPoint>
        {
            ["B"] = new(64, -2), ["A"] = new(64, -2), ["C"] = new(200, 0),
        });
        index.FindNearest(new GraphPoint(63, -2), 4).Should().Be("A");
        index.FindNearest(new GraphPoint(0, 0), 4).Should().BeNull();
    }
}
