using Avalonia.Controls;
using Avalonia.LogicalTree;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Tests;

public sealed class AvaloniaHeadlessRegressionTests
{
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
            buttons.Should().OnlyContain(button => button.Command != null);
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
