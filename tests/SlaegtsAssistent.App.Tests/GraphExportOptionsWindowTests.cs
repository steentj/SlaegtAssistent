using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Interactivity;
using FluentAssertions;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Tests;

public sealed class GraphExportOptionsWindowTests
{
    [Fact]
    public Task KunValgtFormatsIndstillingerVises() => HeadlessTestApplication.RunAsync(() =>
    {
        var window = new GraphExportOptionsWindow();
        var controls = window.GetLogicalDescendants().OfType<Control>().ToArray();
        var format = controls.OfType<ComboBox>().Single(item => item.Name == "Format");
        var resolution = controls.Single(item => item.Name == "ResolutionPanel");
        var paper = controls.Single(item => item.Name == "PaperPanel");
        var button = controls.OfType<Button>().Single(item => item.Name == "Submit");
        resolution.IsVisible.Should().BeTrue();
        paper.IsVisible.Should().BeFalse();
        format.SelectedIndex = (int)GraphExportFormat.Svg;
        resolution.IsVisible.Should().BeFalse();
        paper.IsVisible.Should().BeFalse();
        format.SelectedIndex = (int)GraphExportFormat.Pdf;
        resolution.IsVisible.Should().BeFalse();
        paper.IsVisible.Should().BeTrue();
        format.SelectedIndex = (int)GraphExportFormat.Print;
        paper.IsVisible.Should().BeTrue();
        button.Content.Should().Be("Udskriv…");
        window.Close();
        return Task.CompletedTask;
    });
    [Fact]
    public Task DialogenReturnererDeValgteEksportindstillingerOgKanAnnulleres() => HeadlessTestApplication.RunAsync(async () =>
    {
        var owner = new Window();
        owner.Show();
        try
        {
            var window = new GraphExportOptionsWindow();
            var result = window.ShowDialog<GraphExportSelection?>(owner);
            var controls = window.GetLogicalDescendants().OfType<Control>().ToArray();
            controls.OfType<ComboBox>().Single(item => item.Name == "Format").SelectedIndex = (int)GraphExportFormat.Pdf;
            controls.OfType<ComboBox>().Single(item => item.Name == "Paper").SelectedIndex = 3;
            controls.OfType<CheckBox>().Single(item => item.Name == "Viewport").IsChecked = true;
            controls.OfType<CheckBox>().Single(item => item.Name == "Labels").IsChecked = false;
            controls.OfType<Button>().Single(item => item.Name == "Submit").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var selection = await result;
            selection.Should().Be(new GraphExportSelection(GraphExportFormat.Pdf, true, false, 4096, 3));

            var cancelled = new GraphExportOptionsWindow(GraphExportFormat.Print);
            var cancelledResult = cancelled.ShowDialog<GraphExportSelection?>(owner);
            cancelled.GetLogicalDescendants().OfType<Button>().Single(item => item.Content as string == "Annullér")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            (await cancelledResult).Should().BeNull();
        }
        finally { owner.Close(); }
    });
}
