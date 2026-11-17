using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomDifferenceReviewWindowTests
{
    [Fact]
    public Task Anvend_BeholderDialogAabenNaarEnValgtKandidatIkkeKanDannes() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var items = CreateDifferences();
            items[0] = items[0] with { CandidatePreviewFactory = _ => null };
            var window = new GedcomDifferenceReviewWindow(items);
            try
            {
                window.Show();
                var apply = window.GetLogicalDescendants().OfType<Button>().Single(control => control.Name == "ReviewApply");
                apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                window.IsVisible.Should().BeTrue();
                window.Decision.Should().BeNull();
                window.GetLogicalDescendants().OfType<TextBlock>().Should().Contain(text =>
                    text.Text != null && text.Text.Contains("Kandidaten kan ikke anvendes"));
            }
            finally { window.Close(); }
            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task PersonskiftOgFiltrering_BevarerValg_OgKunAnvendReturnererDem(bool apply) =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var window = new GedcomDifferenceReviewWindow(CreateDifferences()) { Width = 900, Height = 600 };
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                var people = window.GetLogicalDescendants().OfType<ListBox>().Single(control => control.Name == "ReviewPeople");
                var filter = window.GetLogicalDescendants().OfType<TextBox>().Single(control => control.Name == "ReviewFilter");
                var scroll = window.GetLogicalDescendants().OfType<ScrollViewer>().Single(control => control.Name == "ReviewDetails");
                scroll.HorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Disabled);
                var keep = window.GetLogicalDescendants().OfType<RadioButton>()
                    .Single(button => (string?)button.Content == "Bevar dokumentværdi");
                keep.IsChecked = true;
                filter.Text = "Bo";
                Dispatcher.UIThread.RunJobs();
                ((GedcomDifferencePerson)people.SelectedItem!).Name.Should().Be("Bo");
                var use = window.GetLogicalDescendants().OfType<RadioButton>()
                    .Single(button => (string?)button.Content == "Brug ny GEDCOM-værdi");
                use.IsChecked = true;
                filter.Text = "Anna";
                Dispatcher.UIThread.RunJobs();
                ((GedcomDifferencePerson)people.SelectedItem!).Name.Should().Be("Anna");
                window.GetLogicalDescendants().OfType<RadioButton>()
                    .Single(button => (string?)button.Content == "Bevar dokumentværdi").IsChecked.Should().BeTrue();
                var action = window.GetLogicalDescendants().OfType<Button>().Single(control => control.Name == (apply ? "ReviewApply" : "ReviewCancel"));
                action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                if (apply)
                    window.Decision.Should().BeEquivalentTo(new Dictionary<string, bool> { ["a"] = false, ["b"] = true });
                else
                    window.Decision.Should().BeNull();
            }
            finally { window.Close(); }
            return Task.CompletedTask;
        });

    private static GedcomDifferenceReviewItem[] CreateDifferences()
    {
        var document = new BiographyDocument(null, "Fri tekst", false);
        GedcomDifferenceReviewItem Item(string key, string name, bool initial) => new(
            key, name, $"/{key}.md", document,
            new BiographyFactsSnapshot(null, null, null, null, null, null, []),
            new BiographyDifference("Fødselsdato", "gammel", "ny"), initial)
        {
            CandidatePreviewFactory = choices => choices[key] ? $"Kandidat for {name}" : null,
        };
        return [Item("a", "Anna", true), Item("b", "Bo", false)];
    }
}
