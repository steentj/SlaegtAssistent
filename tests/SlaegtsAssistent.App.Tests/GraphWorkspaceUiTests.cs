using Avalonia;
using Avalonia.Controls;
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
    public Task GraffanenSøgerAllePersonerOgViserProbanden() => HeadlessTestApplication.RunAsync(() =>
    {
        var vm = new MainWindowViewModel();
        vm.PersonGraph.SetGraph(new PersonGraphBuilder().Build(
            [new("A", "Anna", "F", null, null), new("B", "Bo", "M", null, null)], []), null, "A");
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
            window.GetVisualDescendants().OfType<TabControl>().Single().Items
                .Cast<TabItem>().Select(tab => tab.Header).Should().Equal("Graf", "Editor", "Preview");
            var search = window.GetVisualDescendants().OfType<TextBox>().Single(box => box.PlaceholderText == "Søg person");
            search.Text = "Bo";
            Dispatcher.UIThread.RunJobs();
            vm.PersonGraph.SearchResults.Select(item => item.RecordId).Should().Equal("B");
            vm.PersonGraph.SelectedPerson = vm.PersonGraph.SearchResults.Single();
            vm.PersonGraph.UseAsProbandCommand.Execute(null);
            vm.PersonGraph.ProbandText.Should().Be("Proband: Bo");
            vm.IsBiographyTab.Should().BeFalse();
            vm.IsGraphTab.Should().BeTrue();
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
