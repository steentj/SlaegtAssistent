using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Tests;

public sealed class GraphWorkspaceUiTests
{
    [Fact]
    public Task GraffanenHarFællesPersonsøgningOgViserProbanden() => HeadlessTestApplication.RunAsync(async () =>
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow();
        var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
        ((Panel)webView.Parent!).Children.Remove(webView);
        try
        {
            window.DataContext = vm;
            vm.DocumentTabIndex = 0;
            window.Measure(new Size(1280, 800));
            window.Arrange(new Rect(0, 0, 1280, 800));
            Dispatcher.UIThread.RunJobs();
            // Lad grafens forsinkede opdatering køre færdigt, så den ikke overskriver testgrafen bagefter.
            await Task.Delay(800);
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(200);
            Dispatcher.UIThread.RunJobs();
            vm.PersonGraph.SetGraph(new PersonGraphBuilder().Build(
                [new("A", "Anna", "F", null, null), new("B", "Bo", "M", null, null)], []), null, "A");
            window.GetVisualDescendants().OfType<TabControl>().First().Items
                .Cast<TabItem>().Select(tab => tab.Header).Should().Equal("Graf", "Editor", "Forhåndsvisning");
            window.GetVisualDescendants().OfType<TextBox>().Count(box => box.PlaceholderText == "Søg navn eller person-id").Should().Be(1);
            window.GetVisualDescendants().OfType<TextBox>().Should().NotContain(box => box.PlaceholderText == "Søg person");
            vm.PersonGraph.Select("B");
            vm.PersonGraph.UseAsProbandCommand.Execute(null);
            vm.PersonGraph.ProbandText.Should().Be("Proband: Bo");
            vm.IsBiographyTab.Should().BeFalse();
            vm.IsGraphTab.Should().BeTrue();
        }
        finally { window.Close(); }
    });
    [Fact]
    public Task GrafensGenvejeVirkerIArbejdsbordetsFanekontrol() => HeadlessTestApplication.RunAsync(() =>
    {
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm, Width = 1280, Height = 800 };
        var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
        ((Panel)webView.Parent!).Children.Remove(webView);
        try
        {
            vm.DocumentTabIndex = 0;
            vm.GraphViewIndex = 1;
            window.Show();
            Dispatcher.UIThread.RunJobs();
            vm.PersonGraph.SetGraph(new PersonGraphBuilder().Build([new("A", "Anna", null, null, null)], []),
                new PersonGraphLayout(new GraphBounds(100, 100), new Dictionary<string, GraphPoint> { ["A"] = new(50, 50) }));
            var graph = window.FindControl<PersonGraphControl>("GraphControl")!;
            var graphTab = window.FindControl<TabItem>("GraphTab")!;
            graphTab.Focus().Should().BeTrue();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            graph.ShowAll();
            var before = graph.GetVisibleWorldBounds();
            window.KeyPress(Key.Right, RawInputModifiers.None, default, null);
            vm.DocumentTabIndex.Should().Be(0);
            graph.IsFocused.Should().BeTrue();
            graph.GetVisibleWorldBounds().Origin.X.Should().BeGreaterThan(before.Origin.X);
            window.KeyPress(Key.D0, RawInputModifiers.None, default, "+");
            graph.GetVisibleWorldBounds().Bounds.Width.Should().BeLessThan(before.Bounds.Width);
            window.KeyPress(Key.OemQuestion, RawInputModifiers.None, default, "-");
            graph.GetVisibleWorldBounds().Bounds.Width.Should().BeApproximately(before.Bounds.Width, 0.001);
            var search = window.GetVisualDescendants().OfType<TextBox>().Single(box => box.PlaceholderText == "Søg navn eller person-id");
            search.Focus();
            window.KeyPress(Key.Right, RawInputModifiers.None, default, null);
            graph.GetVisibleWorldBounds().Origin.X.Should().BeApproximately(before.Origin.X + 40 / (graph.Bounds.Width / before.Bounds.Width), 0.001);
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
