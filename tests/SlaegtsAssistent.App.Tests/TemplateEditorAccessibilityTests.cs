using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorAccessibilityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public Task EditorActions_ShouldStayInsideMinimumWindowAtRequestedScaling(double scaling) =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
            var window = new TemplateEditorWindow(viewModel) { Width = 760, Height = 560 };
            try
            {
                window.Show();
                window.SetRenderScaling(scaling);
                Dispatcher.UIThread.RunJobs();
                window.RenderScaling.Should().Be(scaling);
                AssertInsideWindow(window.FindControl<Button>("UseDraftButton")!, window);
                AssertInsideWindow(window.FindControl<Button>("CancelDraftButton")!, window);
                viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
                viewModel.SelectedBlockHeading = string.Empty;
                viewModel.SaveCommand.Execute(null);
                Dispatcher.UIThread.RunJobs();
                var error = window.FindControl<TextBlock>("EditorErrorText")!;
                error.Text.Should().Contain("mangler en overskrift");
                AssertInsideWindow(error, window);
                AssertInsideWindow(window.FindControl<Button>("UseDraftButton")!, window);
                AssertInsideWindow(window.FindControl<Button>("CancelDraftButton")!, window);
                foreach (var button in window.GetLogicalDescendants().OfType<Button>()
                             .Where(button => button.Name is "UseDraftButton" or "CancelDraftButton"))
                {
                    button.GetLogicalAncestors().OfType<ScrollViewer>().Should().BeEmpty();
                }
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public Task SettingsActions_ShouldStayInsideMinimumWindowAtRequestedScaling(double scaling) =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var window = new SettingsWindow
            {
                DataContext = new SettingsWindowViewModel(new AppSettings(), new EmptyFolderPicker()),
                Width = 560,
                Height = 480,
            };
            try
            {
                window.Show();
                window.SetRenderScaling(scaling);
                Dispatcher.UIThread.RunJobs();
                foreach (var button in window.GetLogicalDescendants().OfType<Button>()
                             .Where(button => button.Content as string is "Gem" or "Gem som…" or "Annuller"))
                {
                    AssertInsideWindow(button, window);
                }
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Fact]
    public Task Keyboard_ShouldMoveSectionsAndReachDraftActions() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
            viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Sources);
            var selected = viewModel.SelectedBlock;
            var window = new TemplateEditorWindow(viewModel);
            try
            {
                window.Show();
                var list = window.FindControl<ListBox>("BlockList")!;
                list.Focus();
                Press(window, Key.Up, RawInputModifiers.Alt);
                Press(window, Key.Up, RawInputModifiers.Alt);
                viewModel.SelectedBlock.Should().BeSameAs(selected);
                viewModel.Blocks.IndexOf(selected!).Should().BeLessThan(
                    viewModel.Blocks.IndexOf(viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Events)));
                var reached = new HashSet<string>();
                for (var index = 0; index < 70; index++)
                {
                    Press(window, Key.Tab);
                    if (window.FocusManager!.GetFocusedElement() is Control focused)
                    {
                        reached.Add(AutomationProperties.GetName(focused) ?? focused.Name ?? string.Empty);
                    }
                }
                reached.Should().Contain("Valgt sektions overskrift");
                reached.Should().Contain("Brug kladde i Indstillinger");
                reached.Should().Contain("Annuller skabelonændringer");
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    [Fact]
    public Task EditorControls_ShouldExposeNamesAndKeepFocusWhenChangingOptions() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
            viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
            var window = new TemplateEditorWindow(viewModel);
            try
            {
                window.Show();
                Dispatcher.UIThread.RunJobs();
                foreach (var control in window.GetLogicalDescendants().OfType<Control>()
                             .Where(control => control is Button or TextBox or ComboBox or ListBox or Expander))
                {
                    AutomationProperties.GetName(control).Should().NotBeNullOrWhiteSpace(control.GetType().Name);
                }
                var fields = window.FindControl<ListBox>("FieldList")!;
                var checkbox = fields.GetVisualDescendants().OfType<CheckBox>().First();
                var field = viewModel.Fields[0];
                checkbox.Focus();
                Press(window, Key.Space);
                checkbox.IsFocused.Should().BeTrue();
                field.IsSelected.Should().BeFalse();
                viewModel.SelectedField.Should().BeSameAs(field);
                Press(window, Key.Space);
                field.IsSelected.Should().BeTrue();
                checkbox.IsFocused.Should().BeTrue();
                viewModel.SelectedBlockHelp.Should().Contain("Manglende oplysninger");
                viewModel.SelectedBlockExample.Should().StartWith("Eksempel:");
                var visibility = window.FindControl<CheckBox>("SectionVisibilityCheckBox")!;
                visibility.Focus();
                Press(window, Key.Space);
                visibility.IsFocused.Should().BeTrue();
                viewModel.SelectedBlockIsVisible.Should().BeFalse();
                viewModel.SelectedBlock!.AccessibilityName.Should().Contain("skjult");
                Press(window, Key.Space);
                visibility.IsFocused.Should().BeTrue();
                viewModel.SelectedBlockIsVisible.Should().BeTrue();
                fields.Focus();
                Press(window, Key.Down, RawInputModifiers.Control | RawInputModifiers.Alt);
                viewModel.SelectedField.Should().BeSameAs(field);
                viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts)
                    .Fields[1].Should().Be(field.Field);
                Press(window, Key.Down, RawInputModifiers.Control | RawInputModifiers.Alt);
                viewModel.SelectedField.Should().BeSameAs(field);
                viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts)
                    .Fields[2].Should().Be(field.Field);
                var cancelled = false;
                viewModel.CloseRequested += (_, result) => cancelled = result is null;
                window.KeyPress(Key.Escape, RawInputModifiers.None, default, null);
                Dispatcher.UIThread.RunJobs();
                cancelled.Should().BeTrue();
                window.IsVisible.Should().BeFalse();
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });

    private static void Press(Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, default, null);
        window.KeyRelease(key, modifiers, default, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void AssertInsideWindow(Control control, Window window)
    {
        var origin = control.TranslatePoint(default, window)!.Value;
        control.IsEffectivelyVisible.Should().BeTrue();
        control.Bounds.Width.Should().BeGreaterThan(0);
        origin.X.Should().BeGreaterThanOrEqualTo(0);
        origin.Y.Should().BeGreaterThanOrEqualTo(0);
        (origin.X + control.Bounds.Width).Should().BeLessThanOrEqualTo(window.ClientSize.Width);
        (origin.Y + control.Bounds.Height).Should().BeLessThanOrEqualTo(window.ClientSize.Height);
    }

    private sealed class EmptyFolderPicker : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? initialDirectory) => Task.FromResult<string?>(null);
    }
}
