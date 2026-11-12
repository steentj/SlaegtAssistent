using System.Xml.Linq;
using FluentAssertions;
using SlaegtsAssistent.Core.Graph;
using SlaegtsAssistent.Core.Graph.Export;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GraphExportSceneTests
{
    [Fact]
    public void UdsnitKlipperForbindelserVedDenValgtePlacering()
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Åse & <Bo>", "F", "1900", null), new("B", "Bo", "M", null, null)], [new("A", "B", GraphRelationKind.Partner)]);
        var layout = new PersonGraphLayout(new GraphBounds(300, 100), new Dictionary<string, GraphPoint> { ["A"] = new(100, 50), ["B"] = new(250, 50) });
        var scene = new GraphExportSceneBuilder().Build(graph, layout, new(GraphExportScope.CurrentViewport, true, "A", new GraphBounds(100, 100), new GraphPoint(75, 0)));
        scene.Nodes.Should().ContainSingle().Which.IsProband.Should().BeTrue();
        scene.Nodes[0].Position.Y.Should().Be(66);
        scene.Lines.Should().ContainSingle().Which.End.X.Should().Be(scene.Nodes[0].Position.X + 75);
        var svg = new GraphSvgWriter().Write(scene);
        XDocument.Parse(svg).Root!.Value.Should().Contain("Åse & <Bo>");
    }

    [Fact]
    public void TættePersonerFaarAdskilteTekstfelter()
    {
        var people = Enumerable.Range(0, 20).Select(i => new GraphPersonInput(i.ToString(), $"Person nummer {i}", "F", "1900", "1970")).ToArray();
        var graph = new PersonGraphBuilder().Build(people, []);
        var positions = people.ToDictionary(person => person.RecordId, _ => new GraphPoint(50, 50));
        var scene = new GraphExportSceneBuilder().Build(graph, new PersonGraphLayout(new GraphBounds(100, 100), positions), new(GraphExportScope.WholeGraph, true, "0"));
        foreach (var node in scene.Nodes)
        {
            var point = node.LabelPosition!.Value;
            point.X.Should().BeGreaterThanOrEqualTo(0);
            (point.Y + node.LabelHeight).Should().BeLessThanOrEqualTo(scene.Bounds.Height);
            foreach (var other in scene.Nodes.Where(other => other.RecordId != node.RecordId))
            {
                var otherPoint = other.LabelPosition!.Value;
                var overlap = point.X < otherPoint.X + other.LabelWidth && point.X + node.LabelWidth > otherPoint.X && point.Y < otherPoint.Y + other.LabelHeight && point.Y + node.LabelHeight > otherPoint.Y;
                overlap.Should().BeFalse();
            }
        }
    }

    [Fact]
    public void SvgHarHvidBaggrundOgAllePersonerOgForbindelser()
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Åse & Bo", "F", null, null), new("B", "Bo", "M", null, null)], [new("A", "B", GraphRelationKind.Partner)]);
        var layout = new PersonGraphLayout(new GraphBounds(100, 100), new Dictionary<string, GraphPoint> { ["A"] = new(20, 30), ["B"] = new(80, 70) });
        var scene = new GraphExportSceneBuilder().Build(graph, layout, new(GraphExportScope.WholeGraph, true, "A"));
        var root = XDocument.Parse(new GraphSvgWriter().Write(scene)).Root!;
        XNamespace ns = "http://www.w3.org/2000/svg";
        root.Elements(ns + "rect").Should().ContainSingle().Which.Attribute("fill")!.Value.Should().Be("#FFFFFF");
        root.Elements(ns + "circle").Should().HaveCount(2);
        root.Descendants(ns + "line").Should().HaveCount(1);
        root.Value.Should().Contain("Åse & Bo");
    }

    [Fact]
    public void HeleGrafenFastholdesNaarLayoutAendresEfterEksportstart()
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Åse", "F", null, null), new("B", "Bo", "M", null, null)], [new("A", "B", GraphRelationKind.Partner)]);
        var positions = new Dictionary<string, GraphPoint> { ["A"] = new(10, 20), ["B"] = new(90, 80) };
        var scene = new GraphExportSceneBuilder().Build(graph, new PersonGraphLayout(new GraphBounds(100, 100), positions), new(GraphExportScope.WholeGraph, true, "A"));
        var before = new GraphSvgWriter().Write(scene);
        positions["A"] = new(500, 500);
        new GraphSvgWriter().Write(scene).Should().Be(before);
        scene.Nodes.Select(node => node.RecordId).Should().BeEquivalentTo("A", "B");
        scene.Lines.Should().ContainSingle();
    }

    [Fact]
    public void UdsnitBevarerForbindelseSomKrydserBeggeKanter()
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Åse", "F", null, null), new("B", "Bo", "M", null, null)], [new("A", "B", GraphRelationKind.Partner)]);
        var positions = new Dictionary<string, GraphPoint> { ["A"] = new(-20, 50), ["B"] = new(120, 50) };
        var scene = new GraphExportSceneBuilder().Build(graph, new PersonGraphLayout(new GraphBounds(140, 100), positions), new(GraphExportScope.CurrentViewport, false, "A", new GraphBounds(100, 100), new GraphPoint(0, 0)));
        scene.Nodes.Should().BeEmpty();
        scene.Lines.Should().ContainSingle().Which.Should().Be(new GraphExportLine(new GraphPoint(16, 66), new GraphPoint(116, 66)));
    }
}
