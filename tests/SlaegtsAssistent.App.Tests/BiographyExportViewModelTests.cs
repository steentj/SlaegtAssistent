using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Tests;

public class BiographyExportViewModelTests
{
    [Fact]
    public void SoegningBevarerValgOgViserFejl()
    {
        var viewModel = new BiographyExportViewModel([new("a", "Anna", "/tmp/a.md"), new("b", "Bo", "/tmp/b.md", "Fejl i dokument")], "a");
        viewModel.SelectedIds.Should().Equal("a");
        viewModel.SelectAllCommand.Execute(null);
        viewModel.SearchText = "Bo";
        viewModel.VisiblePeople.Should().ContainSingle().Which.ErrorMessage.Should().Be("Fejl i dokument");
        viewModel.SelectedIds.Should().BeEquivalentTo(["a", "b"]);
        viewModel.SearchText = "";
        viewModel.VisiblePeople.Should().HaveCount(2);
    }

    [Fact]
    public Task DialogHarTilgaengeligeValgOgHandlinger() => HeadlessTestApplication.RunAsync(() =>
    {
        var window = new BiographyExportWindow(new MainWindowViewModel());
        try
        {
            window.Show();
            var buttons = window.GetVisualDescendants().OfType<Button>().ToArray();
            buttons.Should().Contain(button => button.Content as string == "Eksportér…" && button.Focusable);
            buttons.Should().Contain(button => button.Content as string == "Udskriv…" && button.Focusable);
            window.GetVisualDescendants().OfType<TextBox>().Should().Contain(box => box.PlaceholderText == "Søg navn eller person-id");
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
    [Fact]
    public Task AfkrydsningIListenOpdatererUdvalget() => HeadlessTestApplication.RunAsync(() =>
    {
        var model = new BiographyExportViewModel([new("a", "Anna", "/tmp/a.md")], null);
        var window = new BiographyExportWindow(new MainWindowViewModel()) { DataContext = model };
        try
        {
            window.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var check = window.GetVisualDescendants().OfType<CheckBox>().Single(box => box.Content as string == "Anna");
            check.IsChecked = true;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            model.SelectedIds.Should().Equal("a");
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
