using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Tests;

public sealed class PersonGraphControlTests
{
    [Fact]
    public Task HoverOgKlikPåPersonÅbnerDenUdenAtÆndreProband() => HeadlessTestApplication.RunAsync(() =>
    {
        string? opened = null;
        var model = new PersonGraphViewModel(id => opened = id);
        var graph = new PersonGraphBuilder().Build(
            [new("A", "Anna", "F", "1840", null), new("B", "Bo", "M", null, "1910")],
            [new("A", "B", GraphRelationKind.Parent)]);
        var layout = new PersonGraphLayout(new GraphBounds(100, 100), new Dictionary<string, GraphPoint>
        {
            ["A"] = new(50, 50), ["B"] = new(70, 50),
        });
        model.SetGraph(graph, layout, "B");
        var control = new PersonGraphControl { DataContext = model };
        var window = new Window { Width = 400, Height = 400, Content = control };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            control.ShowAll();
            var center = new Point(control.Bounds.Width / 2, control.Bounds.Height / 2);
            window.MouseMove(center, RawInputModifiers.None);
            model.HoveredPerson?.RecordId.Should().Be("A");
            model.DirectConnections.Select(item => item.RecordId).Should().Equal("A", "B");
            window.MouseDown(center, MouseButton.Left, RawInputModifiers.None);
            window.MouseUp(center, MouseButton.Left, RawInputModifiers.None);
            opened.Should().Be("A");
            model.ProbandId.Should().Be("B");
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });

    [Fact]
    public Task TrækPåBaggrundÅbnerIngenPerson() => HeadlessTestApplication.RunAsync(() =>
    {
        string? opened = null;
        var model = new PersonGraphViewModel(id => opened = id);
        model.SetGraph(new PersonGraphBuilder().Build([new("A", "Anna", null, null, null)], []),
            new PersonGraphLayout(new GraphBounds(100, 100), new Dictionary<string, GraphPoint> { ["A"] = new(50, 50) }));
        var window = new Window { Width = 400, Height = 400, Content = new PersonGraphControl { DataContext = model } };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.None);
            window.MouseMove(new Point(100, 100), RawInputModifiers.LeftMouseButton);
            window.MouseUp(new Point(100, 100), MouseButton.Left, RawInputModifiers.None);
            opened.Should().BeNull();
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
