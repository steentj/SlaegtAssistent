using FluentAssertions;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GraphCameraTests
{
    [Fact]
    public void ZoomBevarerVerdenspunktetUnderMarkøren()
    {
        var camera = new GraphCamera(new GraphPoint(30, 40), 2);
        var cursor = new GraphPoint(100, 80);
        var world = camera.ToWorld(cursor);
        var zoomed = camera.ZoomAt(cursor, 4);
        zoomed.ToWorld(cursor).Should().Be(world);
        zoomed.ToScreen(world).Should().Be(cursor);
    }

    [Fact]
    public void ZoomOgPanoreringRespektererGrænserOgRetning()
    {
        var camera = new GraphCamera(new GraphPoint(0, 0), 1);
        camera.ZoomAt(new GraphPoint(0, 0), 100).Zoom.Should().Be(8);
        camera.ZoomAt(new GraphPoint(0, 0), 0.01).Zoom.Should().Be(0.1);
        camera.Pan(10, -20).Origin.Should().Be(new GraphPoint(-10, 20));
        var center = GraphCamera.Fit(new GraphBounds(100, 100), 200, 200).ToScreen(new GraphPoint(50, 50));
        center.X.Should().BeApproximately(100, 0.000001);
        center.Y.Should().BeApproximately(100, 0.000001);
    }
}
