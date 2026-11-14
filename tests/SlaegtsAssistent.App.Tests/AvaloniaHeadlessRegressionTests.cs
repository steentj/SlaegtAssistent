using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class AvaloniaHeadlessRegressionTests
{
    [Fact]
    public Task TemplateHeading_ShouldKeepKeyboardFocusAndCaretWhileTyping() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
            viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
            var window = new TemplateEditorWindow(viewModel);
            try
            {
                window.Show();
                var heading = window.FindControl<TextBox>("HeadingTextBox")!;
                heading.Focus();
                heading.SelectAll();
                foreach (var text in new[] { "M", "i", "n", "e", " ", "f", "a", "k", "t", "a" })
                {
                    window.KeyTextInput(text);
                    Dispatcher.UIThread.RunJobs();
                    heading.IsFocused.Should().BeTrue();
                    heading.CaretIndex.Should().Be(heading.Text!.Length);
                }
                heading.Text.Should().Be("Mine fakta");
                viewModel.SelectedBlockHeading.Should().Be("Mine fakta");
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Fact]
    public Task MainWindow_ShouldLoadCriticalBindingsAndMinimumLayoutWithoutRenderer() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new MainWindowViewModel();
            var window = new MainWindow
            {
                DataContext = viewModel,
            };

            window.MinWidth.Should().Be(960);
            window.MinHeight.Should().Be(600);
            window.FindControl<NativeWebView>("PreviewWebView").Should().NotBeNull();
            window.DataContext.Should().BeSameAs(viewModel);
            Descendants<Button>(window).Should().Contain(button =>
                button.Content as string == "Anvend skabelon" && button.Command == viewModel.ApplyBiographyTemplateCommand);
            Descendants<Button>(window).Should().Contain(button =>
                button.Content as string == "Commit" && button.Command == viewModel.CommitCommand);
            Descendants<Button>(window).Should().Contain(button =>
                button.Content as string == "Versionshistorik" && button.Command == viewModel.ShowVersionHistoryCommand);
            return Task.CompletedTask;
        });

    [Fact]
    public Task SettingsWindow_ShouldBindCommandsAndExposeKeyboardAccessibleButtons() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var window = new SettingsWindow
            {
                DataContext = new SettingsWindowViewModel(new AppSettings(), new EmptyFolderPicker()),
            };
            var buttons = Descendants<Button>(window).ToArray();

            buttons.Should().Contain(button => button.Content as string == "Gem" && button.Focusable);
            buttons.Should().Contain(button => button.Content as string == "Annuller" && button.Focusable);
            buttons.Should().Contain(button => button.Content as string == "Rediger skabelon…" && button.Focusable);
            buttons.Should().Contain(button => button.Content as string == "Gem som…" && button.Focusable);
            buttons.Where(button => button.Content as string is "Gem" or "Gem som…" or "Annuller")
                .Should().OnlyContain(button => !button.GetLogicalAncestors().OfType<ScrollViewer>().Any());
            buttons.Should().OnlyContain(button => button.Command != null);
            return Task.CompletedTask;
        });

    [Fact]
    public Task TemplateEditorWindow_ShouldExposeGuidedKeyboardAccessibleControlsWithoutSyntax() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
            var window = new TemplateEditorWindow
            {
                DataContext = viewModel,
            };
            var buttons = Descendants<Button>(window).ToArray();
            var visibleText = string.Join(" ", Descendants<TextBlock>(window).Select(item => item.Text));

            window.MinWidth.Should().Be(760);
            window.MinHeight.Should().Be(560);
            window.FindControl<ListBox>("BlockList").Should().NotBeNull();
            window.FindControl<ListBox>("FieldList").Should().NotBeNull();
            window.FindControl<NativeWebView>("TemplatePreviewWebView").Should().NotBeNull();
            Descendants<TabItem>(window).Select(item => item.Header as string)
                .Should().Contain(["Indstillinger", "Forhåndsvisning"]);
            window.FindControl<TextBlock>("EditorErrorText").Should().NotBeNull();
            buttons.Should().Contain(button => button.Content as string == "Brug kladde" && button.IsDefault);
            buttons.Should().Contain(button => button.Content as string == "Annuller" && button.IsCancel);
            buttons.Should().Contain(button => button.Content as string == "Nulstil" && button.Focusable);
            window.KeyBindings.Should().HaveCountGreaterThanOrEqualTo(6);
            visibleText.Should().NotContain("{{");
            visibleText.Should().NotContain("#each");
            visibleText.Should().NotContain("person.");
            return Task.CompletedTask;
        });

    [Fact]
    public Task TemplateHelpWindow_ShouldOnlyShowFormattedHelpAndSearch() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var window = new TemplateCheatSheetWindow
            {
                DataContext = new TemplateCheatSheetViewModel(),
            };
            try
            {
                var search = window.FindControl<TextBox>("SearchBox")!;
                Descendants<TextBox>(window).Should().ContainSingle().Which.Should().BeSameAs(search);
                search.Focusable.Should().BeTrue();
                var preview = window.FindControl<NativeWebView>("PreviewWebView")!;
                preview.Should().NotBeNull();
                var panel = preview.Parent.Should().BeOfType<Border>().Subject;
                Grid.GetRow(panel).Should().Be(1);
                var layout = panel.Parent.Should().BeOfType<Grid>().Subject;
                layout.ColumnDefinitions.Should().BeEmpty();
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Fact]
    public Task HelpWindow_ShouldBeReusableAfterCloseAndKeepSearchKeyboardFocusable() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var first = new MarkdownCheatSheetWindow
            {
                DataContext = new MarkdownCheatSheetViewModel(),
            };
            var search = Descendants<TextBox>(first).First(textBox => textBox.PlaceholderText != null);

            search.Focusable.Should().BeTrue();
            first.Close();

            var reopened = new MarkdownCheatSheetWindow
            {
                DataContext = new MarkdownCheatSheetViewModel(),
            };
            Descendants<TextBox>(reopened).Should().Contain(textBox => textBox.PlaceholderText != null);
            return Task.CompletedTask;
        });

    [Fact]
    public Task GedcomHelpWindow_ShouldShowIntroductionAndLoadInfographic() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var window = new GedcomHelpWindow
            {
                DataContext = new GedcomHelpViewModel(),
            };

            var tabs = Descendants<TabItem>(window).Select(tab => tab.Header as string).ToArray();
            tabs.Should().Contain("Introduktion og reference");
            tabs.Should().Contain("Oversigt (infografik)");
            window.Title.Should().Contain("GEDCOM");

            var infographic = Avalonia.Platform.AssetLoader.Open(
                new Uri("avares://SlaegtsAssistent.App/Assets/Help/Guide_til_GEDCOM-filformatet.png"));
            infographic.Length.Should().BeGreaterThan(0);
            window.Close();
            return Task.CompletedTask;
        });

    [Fact]
    public Task UnsavedChangesDialog_ShouldExposeKeyboardChoicesAndReturnClickedDecision() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var state = AvaloniaUnsavedChangesDialogService.CreateDialog();
            var buttons = Descendants<Button>(state.Dialog).ToArray();
            var save = buttons.Single(button => button.Content as string == "Gem");
            var cancel = buttons.Single(button => button.Content as string == "Annullér");

            save.IsDefault.Should().BeTrue();
            cancel.IsCancel.Should().BeTrue();
            buttons.Should().OnlyContain(button => button.Focusable && button.IsTabStop);

            save.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

            state.Decision.Should().Be(UnsavedChangesDecision.Gem);
            return Task.CompletedTask;
        });

    private static IEnumerable<T> Descendants<T>(Control root) where T : Control
        => root.GetLogicalDescendants().OfType<T>();

    private sealed class EmptyFolderPicker : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder) =>
            Task.FromResult<string?>(null);
    }
}
