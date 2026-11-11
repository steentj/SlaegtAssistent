using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GraphWorkspaceBuilderTests
{
    [Fact]
    public void GodkendtTomForældrelisteForhindrerGenindførelseFraForældersBarneliste()
    {
        var parent = new Person("P") { FullName = "Forælder" };
        var child = new Person("B") { FullName = "Barn" };
        parent.Children.Add(child);
        var facts = new BiographyFactsSnapshot("Barn", null, null, null, null, null, []);
        var people = new[]
        {
            new GraphWorkspacePerson("P", null, CanonicalBiographySnapshot.Create(parent), null, true),
            new GraphWorkspacePerson("B", facts, CanonicalBiographySnapshot.Create(child), null, true),
        };

        var graph = new GraphWorkspaceBuilder().Build(people, []);

        graph.Edges.Should().BeEmpty();
        graph.Neighbours["B"].Should().BeEmpty();
    }

    [Fact]
    public void DokumentetsRepræsenteredeTommeVærdierOverskriverGodkendteFakta()
    {
        var person = new Person("A") { FullName = "Anna", Sex = "F", BirthDate = "1840" };
        var facts = new BiographyFactsSnapshot(null, null, null, null, null, null, []);
        var graph = new GraphWorkspaceBuilder().Build(
            [new("A", facts, CanonicalBiographySnapshot.Create(person), null, true)], []);

        graph.Nodes.Should().ContainSingle().Which.Should().Be(new PersonGraphNode("A", null, null, null, null));
    }

    [Fact]
    public void DokumentUdenBaselineMåIkkeBrugeNyeGedcomFakta()
    {
        var person = new Person("A") { FullName = "Importeret", Sex = "M" };
        var facts = new BiographyFactsSnapshot("Godkendt", null, null, null, null, null, [])
        { RepresentedFields = new HashSet<string> { "Navn" } };
        var graph = new GraphWorkspaceBuilder().Build([new("A", facts, null, CanonicalBiographySnapshot.Create(person), true)], []);

        graph.Nodes.Single().Name.Should().Be("Godkendt");
        graph.Nodes.Single().Sex.Should().BeNull();
        graph.Diagnostics.Should().Contain("Nogle relationer mangler et godkendt datagrundlag.");
    }
}
