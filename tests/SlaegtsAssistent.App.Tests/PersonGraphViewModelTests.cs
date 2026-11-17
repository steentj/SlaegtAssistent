using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Tests;

public sealed class PersonGraphViewModelTests
{
    [Fact]
    public void HoverViserPersonOgAlleDirekteNaboerMedTilgængeligeDatoer()
    {
        var graph = new PersonGraphBuilder().Build(
            [new("A", "Anna", "F", "ABT 1840", null), new("B", "Bo", "M", null, "1920"), new("C", "Cecilie", null, null, null)],
            [new("A", "B", GraphRelationKind.Parent), new("B", "C", GraphRelationKind.MarkdownLink)]);
        var vm = new PersonGraphViewModel(_ => { });
        vm.SetGraph(graph, null, "C");
        vm.Hover("B");

        vm.DirectConnections.Select(item => item.RecordId).Should().Equal("B", "A", "C");
        vm.DirectConnections.Single(item => item.RecordId == "A").Details.Should().Contain("Født: ABT 1840").And.NotContain("Død:");
        vm.SearchText = "Anna";
        vm.SearchResults.Should().ContainSingle();
        vm.Graph.Nodes.Should().HaveCount(3);
    }

    [Fact]
    public void ÅbningÆndrerIkkeProband()
    {
        string? opened = null;
        var vm = new PersonGraphViewModel(id => opened = id);
        vm.SetGraph(new PersonGraphBuilder().Build([new("A", "Anna", null, null, null), new("B", "Bo", null, null, null)], []), null, "A");
        vm.Select("B");
        vm.OpenBiographyCommand.Execute(null);
        opened.Should().Be("B");
        vm.ProbandId.Should().Be("A");
        vm.UseAsProbandCommand.Execute(null);
        vm.ProbandId.Should().Be("B");
    }
    [Fact]
    public void ValgtPersonOgForbindelserForbliverStabileUnderHover()
    {
        var vm = new PersonGraphViewModel(_ => { });
        vm.SetGraph(new PersonGraphBuilder().Build(
            [new("A", "Anna", null, null, null), new("B", "Bo", null, null, null), new("C", "Clara", null, null, null)],
            [new("A", "B", GraphRelationKind.Parent), new("B", "C", GraphRelationKind.Partner)]), null);
        vm.OpenBiographyCommand.CanExecute(null).Should().BeFalse();
        vm.Select("B");
        vm.Hover("C");
        vm.DirectConnections.Select(item => item.RecordId).Should().Equal("B", "A", "C");
        vm.ConnectionSummary.Should().Contain("Valgt person: Bo").And.Contain("Anna (forælder)").And.Contain("Clara (partner)");
        vm.OpenBiographyCommand.CanExecute(null).Should().BeTrue();
        vm.Select(null);
        vm.ConnectionSummary.Should().Contain("Person under markøren: Clara").And.Contain("Bo (partner)");
        vm.Hover(null);
        vm.ConnectionSummary.Should().Be("Ingen person valgt.");
        vm.UseAsProbandCommand.CanExecute(null).Should().BeFalse();
    }
}
