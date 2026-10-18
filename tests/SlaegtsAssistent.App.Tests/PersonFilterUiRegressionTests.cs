using Avalonia.Controls;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

// Disse tests kører mod et rigtigt vindue, fordi fejlene kun opstår, når ListBox og
// TextBox-bindings er aktive sammen med personfilteret.
public sealed class PersonFilterUiRegressionTests
{
    [Fact]
    public Task FilteringPerson_ShouldKeepSelectionPreviewHtmlAndRawGedcom_InRealWindow() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                workspace.ViewModel.PersonFilterText = "anna";
                Layout(window);

                workspace.ViewModel.SelectedPerson.Should().NotBeNull();
                workspace.ViewModel.SelectedPerson!.DisplayName.Should().Be("Anna Jensen");
                workspace.ViewModel.Editor.Should().NotBeNull();
                workspace.ViewModel.Editor!.PreviewHtml.Should().NotBeNullOrWhiteSpace();
                workspace.ViewModel.SelectedPerson.RawGedcom.Should().Contain("Anna /Jensen/");
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public Task ClearingFilter_ShouldNotMarkEditorsDirty_InRealWindow() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                workspace.ViewModel.PersonFilterText = "anna";
                Layout(window);
                workspace.ViewModel.PersonFilterText = string.Empty;
                Layout(window);

                workspace.ViewModel.HasDirtyEditors.Should().BeFalse();
                workspace.ViewModel.SaveStatusText.Should().Be("Gemt");
            }
            finally
            {
                window.Close();
            }
        });

    [Fact]
    public Task Filtering_ShouldKeepSelectedPersonInListBox_WhenPersonStillMatches() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                var bo = workspace.ViewModel.People.Single(person => person.DisplayName == "Bo Jensen");
                workspace.ViewModel.SelectedPerson = bo;
                Layout(window);

                workspace.ViewModel.PersonFilterText = "jensen";
                Layout(window);

                var listBox = window.GetVisualDescendants().OfType<ListBox>().First();
                workspace.ViewModel.SelectedPerson.Should().BeSameAs(bo);
                listBox.SelectedItem.Should().BeSameAs(bo);
                workspace.ViewModel.Editor.Should().NotBeNull();
            }
            finally
            {
                window.Close();
            }
        });

    private static MainWindow ShowWindow(MainWindowViewModel viewModel)
    {
        var window = new MainWindow();
        // En ægte native webview blokerer i headless-tilstand, så den fjernes før layoutet køres.
        var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
        ((Panel)webView.Parent!).Children.Remove(webView);
        window.Measure(new Avalonia.Size(1280, 800));
        window.DataContext = viewModel;
        Layout(window);
        window.GetVisualDescendants().OfType<ListBox>().Should().NotBeEmpty();
        return window;
    }

    private static void Layout(Window window)
    {
        window.Measure(new Avalonia.Size(1280, 800));
        Flush();
    }

    private static void Flush() => Avalonia.Threading.Dispatcher.UIThread.RunJobs();

    private static async Task<Workspace> CreateWorkspaceAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var gedcomPath = Path.Combine(root, "test.ged");
        File.WriteAllLines(gedcomPath,
        [
            "0 HEAD",
            "1 SOUR SlaegtsAssistentTests",
            "1 GEDC",
            "2 VERS 5.5.1",
            "1 CHAR UTF-8",
            "0 @I2@ INDI",
            "1 NAME Bo /Jensen/",
            "0 @I1@ INDI",
            "1 NAME Anna /Jensen/",
            "1 SEX F",
            "0 TRLR",
        ]);

        var settings = new FixedSettingsService(new AppSettings
        {
            DefaultMarkdownOutputFolder = Path.Combine(root, "md"),
        });
        var unsavedDialog = new CountingUnsavedChangesDialogService();
        var viewModel = new MainWindowViewModel(
            new GedcomLoader(),
            new FixedGedcomFilePicker(gedcomPath),
            new NoFolderPicker(),
            settings,
            new NoSettingsDialog(),
            new NoUserDialog(),
            unsavedDialog,
            new NoApplicationControl(),
            new MarkdownBiographyExportService(settings),
            new FileSystemMarkdownFileStore());
        await viewModel.SelectGedcomFileCommand.ExecuteAsync(null);
        return new Workspace(root, viewModel, unsavedDialog);
    }

    private sealed class Workspace(string root, MainWindowViewModel viewModel, CountingUnsavedChangesDialogService unsavedDialog)
        : IDisposable
    {
        public MainWindowViewModel ViewModel { get; } = viewModel;

        public CountingUnsavedChangesDialogService UnsavedDialog { get; } = unsavedDialog;

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FixedSettingsService(AppSettings settings) : IApplicationSettingsService
    {
        public AppSettings Load() => new()
        {
            DefaultMarkdownOutputFolder = settings.DefaultMarkdownOutputFolder,
        };

        public void Save(AppSettings settings)
        {
        }
    }

    private sealed class FixedGedcomFilePicker(string path) : IGedcomFilePickerService
    {
        public Task<string?> PickGedcomFileAsync(string? suggestedStartFolder) => Task.FromResult<string?>(path);
    }

    private sealed class NoFolderPicker : IFolderPickerService
    {
        public Task<string?> PickFolderAsync(string title, string? suggestedStartFolder) => Task.FromResult<string?>(null);
    }

    private sealed class NoSettingsDialog : ISettingsDialogService
    {
        public Task<AppSettings?> EditSettingsAsync(AppSettings currentSettings) => Task.FromResult<AppSettings?>(null);
    }

    private sealed class NoUserDialog : IUserDialogService
    {
        public Task ShowInformationAsync(string title, string message) => Task.CompletedTask;
    }

    private sealed class NoApplicationControl : IApplicationControlService
    {
        public void Exit()
        {
        }
    }

    private sealed class CountingUnsavedChangesDialogService : IUnsavedChangesDialogService
    {
        public int AskCount { get; private set; }

        public Task<UnsavedChangesDecision> AskAsync()
        {
            AskCount++;
            return Task.FromResult(UnsavedChangesDecision.Annullér);
        }
    }
}
