using FluentAssertions;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.Core.Tests;

public sealed class PersonGraphBuilderTests
{
    [Fact]
    public void Build_BevarerAllePersonerOgDirekteForbindelserUdenDubleredeKanter()
    {
        var people = "ABCDEFGHIJ".Select(id => new GraphPersonInput(id.ToString(), id.ToString(), null, null, null)).ToArray();
        var pairs = new[] { "AB", "AC", "AD", "BC", "BD", "EA", "FB", "AG", "IJ", "CH", "EF" };
        var relations = pairs.Select(pair => new GraphRelation(pair[..1], pair[1..], GraphRelationKind.Parent)).ToList();
        relations.Add(new GraphRelation("H", "C", GraphRelationKind.MarkdownLink));
        relations.Add(relations[0]);

        var graph = new PersonGraphBuilder().Build(people, relations);

        graph.Nodes.Select(node => node.RecordId).Should().Equal("A", "B", "C", "D", "E", "F", "G", "H", "I", "J");
        graph.Edges.Select(edge => edge.FirstId + edge.SecondId).Should().BeEquivalentTo("AB", "AC", "AD", "BC", "BD", "AE", "BF", "AG", "IJ", "CH", "EF");
        graph.Neighbours["A"].Should().Equal("B", "C", "D", "E", "G");
        graph.Edges.Single(edge => edge.FirstId == "C" && edge.SecondId == "H").Relations.Should().HaveCount(2);
    }

    [Fact]
    public void Build_DiagnosticererUkendtePersonerOgSelvlinks()
    {
        var graph = new PersonGraphBuilder().Build(
            [new("A", "Anna", "F", null, null)],
            [new("A", "A", GraphRelationKind.MarkdownLink), new("A", "B", GraphRelationKind.Parent)]);

        graph.Edges.Should().BeEmpty();
        graph.Neighbours["A"].Should().BeEmpty();
        graph.Diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public void Build_DubleretIdentitetBrugerIkkeModstridendeFakta()
    {
        var graph = new PersonGraphBuilder().Build(
            [new("A", "Anna", "F", null, null), new("A", "Anders", "M", null, null)], []);

        graph.Nodes.Should().ContainSingle().Which.Name.Should().BeNull();
        graph.Diagnostics.Should().ContainSingle();
    }
}
