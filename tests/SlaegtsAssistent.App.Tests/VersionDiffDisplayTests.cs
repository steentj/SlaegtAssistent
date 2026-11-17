using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Tests;

public class VersionDiffDisplayTests
{
    [Fact]
    public Task AEndringerHarTekstligeMarkoererOgTemastyredeBaggrunde() => HeadlessTestApplication.RunAsync(() =>
    {
        var model = new VersionDiffViewModel();
        model.SetContent("Ændringer", [new DiffFile("test.md", WorkingTreeChangeKind.Aendret,
            [new(DiffLineKind.Added, "tilføjet"), new(DiffLineKind.Removed, "fjernet")])], null);
        var window = new VersionDiffWindow { DataContext = model, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var lines = window.GetVisualDescendants().OfType<TextBlock>().ToArray();
            var added = lines.Single(line => line.Text == "+ tilføjet");
            var removed = lines.Single(line => line.Text == "− fjernet");
            added.Background.Should().NotBeNull();
            removed.Background.Should().NotBeNull();
            var light = added.Background;
            window.RequestedThemeVariant = ThemeVariant.Dark;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            added.Background.Should().NotBe(light);
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
