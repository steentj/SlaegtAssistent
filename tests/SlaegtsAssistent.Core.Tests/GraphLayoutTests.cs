using FluentAssertions;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GraphLayoutTests
{
    private static GraphDotInput Input() => new GraphDotWriter().Write(new PersonGraphBuilder().Build(
        [new("@I2@", "Æ ø å", "F", null, null), new("@I1@", "Navn", "M", null, null)],
        [new("@I1@", "@I2@", GraphRelationKind.Partner)]));

    [Fact]
    public void DotBrugerStabileInterneIdOgIngenPersondata()
    {
        var input = Input();
        input.NodeIds["n0"].Should().Be("@I1@");
        input.Dot.Should().Contain("n0 -- n1").And.NotContain("Æ ø å").And.NotContain("@I1@");
    }

    [Fact]
    public void PlainKonvertererKoordinaterOgHaandtererCiteredeFelter()
    {
        const string output = "graph 1 4 3\nnode n0 1 2 0.12 0.12 \"\" solid circle black white\nnode n1 3 1 0.12 0.12 \"et navn\" solid circle black white\nedge n0 n1 2 1 2 3 1 solid black\nstop\n";
        var layout = new GraphPlainParser().Parse(output, Input());
        layout.Bounds.Should().Be(new GraphBounds(384, 288));
        layout.Positions["@I1@"].Should().Be(new GraphPoint(96, 96));
        layout.Positions["@I2@"].Should().Be(new GraphPoint(288, 192));
    }

    [Theory]
    [InlineData("graph 2 4 3\nstop\n")]
    [InlineData("graph 1 4 3\nnode n0 NaN 2 1 1 \"\" solid circle black white\nstop\n")]
    [InlineData("graph 1 4 3\nnode n9 1 2 1 1 \"\" solid circle black white\nstop\n")]
    [InlineData("graph 1 4 3\n")]
    public void PlainAfviserUgyldigtOutput(string output)
    {
        var action = () => new GraphPlainParser().Parse(output, Input());
        action.Should().Throw<FormatException>();
    }
}
