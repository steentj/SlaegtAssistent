using System.Buffers.Binary;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Graph;
using SlaegtsAssistent.Core.Graph.Export;
using SkiaSharp;

namespace SlaegtsAssistent.App.Tests;

public sealed class AvaloniaGraphRasterizerTests
{
    [Theory]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(8192)]
    public Task EksportererPngMedValgtStoerrelseOgAspektforhold(int longSide) => HeadlessTestApplication.RunAsync(() =>
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Åse", "F", "1900", null), new("B", "Bo", "M", null, null)], [new("A", "B", GraphRelationKind.Partner)]);
        var layout = new PersonGraphLayout(new GraphBounds(2000, 100), new Dictionary<string, GraphPoint> { ["A"] = new(50, 50), ["B"] = new(150, 50) });
        var scene = new GraphExportSceneBuilder().Build(graph, layout, new(GraphExportScope.WholeGraph, false, "A"));
        var png = new AvaloniaGraphRasterizer().Render(scene, longSide);
        png.Take(8).Should().Equal(137, 80, 78, 71, 13, 10, 26, 10);
        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        width.Should().Be(longSide);
        ((double)height).Should().BeApproximately(width * scene.Bounds.Height / scene.Bounds.Width, 1);
        using var image = SKBitmap.Decode(png);
        image.Should().NotBeNull();
        var scale = width / scene.Bounds.Width;
        var first = scene.Nodes.Single(node => node.RecordId == "A");
        var second = scene.Nodes.Single(node => node.RecordId == "B");
        image.GetPixel((int)(first.Position.X * scale), (int)(first.Position.Y * scale)).Should().Be(new SKColor(0xDC, 0x3C, 0x4D));
        image.GetPixel((int)(second.Position.X * scale), (int)(second.Position.Y * scale)).Should().Be(new SKColor(0x34, 0x78, 0xD4));
        return Task.CompletedTask;
    });
}
