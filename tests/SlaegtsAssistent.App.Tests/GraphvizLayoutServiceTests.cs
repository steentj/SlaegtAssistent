using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Tests;

public sealed class GraphvizLayoutServiceTests
{
    [Fact]
    public async Task LayoutBrugerLokaleArgumenterOgRydderMidlertidigeFiler()
    {
        var runner = new FakeRunner();
        var graph = new PersonGraphBuilder().Build([new("A", "Anna", "F", null, null)], []);
        var result = await new GraphvizLayoutService(runner, "/værktøj/sfdp").LayoutAsync(graph, CancellationToken.None);

        result.Positions["A"].Should().Be(new GraphPoint(48, 48));
        runner.Request!.Arguments.Should().HaveCount(4);
        runner.Request.Arguments[0].Should().Be("-Tplain");
        Directory.Exists(runner.Request.WorkingDirectory).Should().BeFalse();
    }

    [Fact]
    public async Task TomGrafStarterIkkeEnProces()
    {
        var runner = new FakeRunner();
        var graph = new PersonGraphBuilder().Build([], []);
        var result = await new GraphvizLayoutService(runner, "/værktøj/sfdp").LayoutAsync(graph, CancellationToken.None);
        result.Positions.Should().BeEmpty();
        runner.Request.Should().BeNull();
    }

    [Fact]
    public async Task GraphvizFejlIStderrAfvisesSelvVedExitkodeNul()
    {
        var graph = new PersonGraphBuilder().Build([new("A", "Anna", "F", null, null)], []);
        var action = () => new GraphvizLayoutService(new ErrorRunner(), "/værktøj/sfdp").LayoutAsync(graph, CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class ErrorRunner : ILocalProcessRunner
    {
        public Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new LocalProcessResult(0, "", "Error: triangulation library missing"));
    }

    private sealed class FakeRunner : ILocalProcessRunner
    {
        public LocalProcessRequest? Request { get; private set; }
        public async Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            await File.WriteAllTextAsync(request.Arguments[3], "graph 1 1 1\nnode n0 0.5 0.5 0.12 0.12 \"\" solid circle black white\nstop\n", cancellationToken);
            return new LocalProcessResult(0, "", "");
        }
    }
}
