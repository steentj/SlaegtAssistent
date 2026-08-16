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
    public void MainWindow_ShouldLoadCriticalBindingsAndMinimumLayoutWithoutRenderer()
    {
        HeadlessTestApplication.EnsureInitialized();
        var viewModel = new MainWindowViewModel();
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        window.MinWidth.Should().Be(960);
        window.MinHeight.Should().Be(600);
        window.FindControl<NativeWebView>("PreviewWebView").Should().NotBeNull();
        window.DataContext.Should().BeSameAs(viewModel);
    }

    [Fact]
    public void SettingsWindow_ShouldBindCommandsAndExposeKeyboardAccessibleButtons()
    {
        HeadlessTestApplication.EnsureInitialized();
        var window = new SettingsWindow
        {
            DataContext = new SettingsWindowViewModel(new AppSettings(), new EmptyFolderPicker()),
        };
        var buttons = Descendants<Button>(window).ToArray();

        buttons.Should().Contain(button => button.Content as string == "Gem" && button.Focusable);
        buttons.Should().Contain(button => button.Content as string == "Annuller" && button.Focusable);
        buttons.Should().OnlyContain(button => button.Command != null);
    }

    [Fact]
    public void HelpWindow_ShouldBeReusableAfterCloseAndKeepSearchKeyboardFocusable()
    {
        HeadlessTestApplication.EnsureInitialized();
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
    }

    [Fact]
    public void UnsavedChangesDialog_ShouldExposeKeyboardChoicesAndReturnClickedDecision()
    {
        HeadlessTestApplication.EnsureInitialized();
        var state = AvaloniaUnsavedChangesDialogService.CreateDialog();
        var buttons = Descendants<Button>(state.Dialog).ToArray();
        var save = buttons.Single(button => button.Content as string == "Gem");
        var cancel = buttons.Single(button => button.Content as string == "Annullér");

        save.IsDefault.Should().BeTrue();
        cancel.IsCancel.Should().BeTrue();
        buttons.Should().OnlyContain(button => button.Focusable && button.IsTabStop);

        save.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        state.Decision.Should().Be(UnsavedChangesDecision.Gem);
    }

    private static IEnumerable<T> Descendants<T>(Control root) where T : Control
        => root.GetLogicalDescendants().OfType<T>();

    private sealed class EmptyFolderPicker : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder) =>
            Task.FromResult<string?>(null);
    }
}
