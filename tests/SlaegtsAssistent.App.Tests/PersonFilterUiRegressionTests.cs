using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.IO.Compression;
using SlaegtsAssistent.Core.Export;
using SlaegtsAssistent.Core.Biography;
using Avalonia.VisualTree;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.App.Views.Markdown;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

// Disse tests kører mod et rigtigt vindue, fordi fejlene kun opstår, når ListBox og
// TextBox-bindings er aktive sammen med personfilteret.
public sealed class PersonFilterUiRegressionTests
{
    [Fact]
    public Task MediekatalogGenindlaesesFraGodkendteSnapshotsUdenNyImport() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync(withMedia: true);
            var settings = new FixedSettingsService(new AppSettings { DefaultMarkdownOutputFolder = Path.Combine(workspace.Root, "md") });
            var restarted = new MainWindowViewModel(new GedcomLoader(), new FixedGedcomFilePicker(Path.Combine(workspace.Root, "test.ged")),
                new NoFolderPicker(), settings, new NoSettingsDialog(), new NoUserDialog(), new CountingUnsavedChangesDialogService(),
                new NoApplicationControl(), new MarkdownBiographyExportService(settings), new FileSystemMarkdownFileStore());
            var items = restarted.CreateMediaCatalog();
            items.Should().ContainSingle(item => item.OwnerId == "@I1@" && item.Reference == "foto.png");
        });

    [Fact]
    public Task AendretEditorMensVaelgerErAabenAfviserBilledet() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync(withMedia: true);
            var vm = workspace.ViewModel;
            var window = ShowWindow(vm);
            window.Show();
            try
            {
                vm.SelectedPerson = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                vm.DocumentTabIndex = 1;
                var editor = vm.Editor!;
                var button = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Vælg eller skift topbillede…");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                MediaPickerWindow? picker = null;
                for (var i = 0; i < 250 && picker is null; i++)
                {
                    await Task.Delay(20);
                    picker = window.OwnedWindows.OfType<MediaPickerWindow>().FirstOrDefault();
                }
                picker.Should().NotBeNull();
                var gallery = picker!.FindControl<ListBox>("Gallery")!;
                for (var i = 0; i < 250 && gallery.ItemCount == 0; i++) await Task.Delay(20);
                gallery.ItemCount.Should().Be(1);
                editor.MarkdownText += "\nNyere brugertekst";
                var current = editor.MarkdownText;
                gallery.SelectedIndex = 0;
                picker.FindControl<Button>("ConfirmButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for (var i = 0; i < 250 && string.IsNullOrEmpty(vm.ErrorMessage); i++) await Task.Delay(20);
                vm.ErrorMessage.Should().Contain("ændret");
                editor.MarkdownText.Should().Be(current).And.NotContain(BiographyImageEditService.PortraitStartMarker);
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task BilledeIndsaettesVedMarkoerenOgKanFortrydes() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync(withMedia: true);
            var vm = workspace.ViewModel;
            var window = ShowWindow(vm);
            window.Show();
            try
            {
                vm.SelectedPerson = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                vm.DocumentTabIndex = 1;
                var editor = vm.Editor!;
                var textbox = window.FindControl<BiographyMarkdownEditor>("BiographyEditor")!;
                var before = editor.MarkdownText;
                textbox.Select(before.Length, 0);
                var button = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Indsæt billede…");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                MediaPickerWindow? picker = null;
                for (var i = 0; i < 250 && picker is null; i++)
                {
                    await Task.Delay(20);
                    picker = window.OwnedWindows.OfType<MediaPickerWindow>().FirstOrDefault();
                }
                picker.Should().NotBeNull();
                var gallery = picker!.FindControl<ListBox>("Gallery")!;
                for (var i = 0; i < 250 && gallery.ItemCount == 0; i++) await Task.Delay(20);
                gallery.ItemCount.Should().Be(1);
                gallery.SelectedIndex = 0;
                picker.FindControl<Button>("ConfirmButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for (var i = 0; i < 250 && editor.MarkdownText == before; i++) await Task.Delay(20);
                editor.MarkdownText.Should().StartWith(before).And.Contain("![");
                editor.IsDirty.Should().BeTrue();
                textbox.Undo();
                editor.MarkdownText.Should().Be(before);
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task TopbilledeKanVaelgesOgFortrydesIEditoren() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync(withMedia: true);
            var vm = workspace.ViewModel;
            var window = ShowWindow(vm);
            window.Show();
            try
            {
                vm.SelectedPerson = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                vm.DocumentTabIndex = 1;
                var editor = vm.Editor!;
                var before = editor.MarkdownText;
                var button = window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Vælg eller skift topbillede…");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                MediaPickerWindow? picker = null;
                for (var i = 0; i < 250 && picker is null; i++)
                {
                    await Task.Delay(20);
                    picker = window.OwnedWindows.OfType<MediaPickerWindow>().FirstOrDefault();
                }
                picker.Should().NotBeNull();
                var gallery = picker!.FindControl<ListBox>("Gallery")!;
                for (var i = 0; i < 250 && gallery.ItemCount == 0; i++) await Task.Delay(20);
                gallery.ItemCount.Should().Be(1);
                gallery.SelectedIndex = 0;
                picker.FindControl<ComboBox>("ImageWidthBox")!.SelectedIndex = 1;
                var confirm = picker.FindControl<Button>("ConfirmButton")!;
                confirm.IsEnabled.Should().BeTrue();
                confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                for (var i = 0; i < 250 && editor.MarkdownText == before; i++) await Task.Delay(20);
                editor.MarkdownText.Should().StartWith(BiographyImageEditService.PortraitStartMarker)
                    .And.Contain("SlaegtsAssistent:width=50");
                editor.IsDirty.Should().BeTrue();
                var textbox = window.FindControl<BiographyMarkdownEditor>("BiographyEditor")!;
                textbox.Undo();
                editor.MarkdownText.Should().Be(before);
                textbox.Redo();
                editor.MarkdownText.Should().StartWith(BiographyImageEditService.PortraitStartMarker);
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task FællesPersonvalgStyrerGrafenUdenFaneskiftOgBevarerUgemtTekst() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var vm = workspace.ViewModel;
            var window = ShowWindow(vm);
            try
            {
                var anna = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                var bo = vm.People.Single(person => person.DisplayName == "Bo Jensen");
                vm.SelectedPerson = anna;
                vm.Editor!.MarkdownText += "\nUgemt tekst";
                vm.DocumentTabIndex = 0;
                vm.PersonGraph.SetGraph(new SlaegtsAssistent.Core.Graph.PersonGraphBuilder().Build(
                    [new(anna.RecordId, anna.DisplayName, null, null, null), new(bo.RecordId, bo.DisplayName, null, null, null)], []), null);
                vm.PersonFilterText = "Bo";
                vm.SelectedPerson!.RecordId.Should().Be(bo.RecordId);
                vm.PersonGraph.SelectedPerson!.RecordId.Should().Be(bo.RecordId);
                vm.DocumentTabIndex.Should().Be(0);
                vm.PersonGraph.Select(anna.RecordId);
                vm.SelectedPerson!.RecordId.Should().Be(anna.RecordId);
                vm.PersonFilterText.Should().BeEmpty();
                vm.Editor!.MarkdownText.Should().Contain("Ugemt tekst");
                var list = window.FindControl<ListBox>("PeopleList")!;
                list.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
                vm.DocumentTabIndex.Should().Be(1);
                vm.Editor.MarkdownText.Should().Contain("Ugemt tekst");
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task ÅbningFraGrafenSkifterTilEditorOgBevarerUgemtTekst() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                var vm = workspace.ViewModel;
                var anna = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                var bo = vm.People.Single(person => person.DisplayName == "Bo Jensen");
                vm.SelectedPerson = anna;
                vm.Editor!.MarkdownText += "\nUgemt tekst";
                vm.PersonGraph.SetGraph(new SlaegtsAssistent.Core.Graph.PersonGraphBuilder().Build(
                    [new(anna.RecordId, anna.DisplayName, "F", null, null), new(bo.RecordId, bo.DisplayName, "M", null, null)], []), null, anna.RecordId);
                vm.DocumentTabIndex = 0;
                vm.PersonGraph.Select(bo.RecordId);
                vm.PersonGraph.OpenBiographyCommand.Execute(null);

                vm.SelectedPerson.Should().BeSameAs(bo);
                vm.DocumentTabIndex.Should().Be(1);
                vm.SelectedPerson = anna;
                vm.Editor!.MarkdownText.Should().Contain("Ugemt tekst");
                vm.Editor.IsDirty.Should().BeTrue();
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task EksportsnapshotBrugerAabenUgemtEditorUdenAtGemmeAendringer() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                var vm = workspace.ViewModel;
                var anna = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                vm.SelectedPerson = anna;
                var original = await File.ReadAllBytesAsync(anna.MarkdownFilePath);
                vm.Editor!.MarkdownText += "\nUgemt fri biografi æøå";
                var snapshot = vm.CreateBiographyExportSnapshot([anna.RecordId]);
                snapshot.Errors.Should().BeEmpty();
                snapshot.Items.Should().ContainSingle().Which.Body.Should().Contain("Ugemt fri biografi æøå");
                (await File.ReadAllBytesAsync(anna.MarkdownFilePath)).Should().Equal(original);
                vm.Editor.IsDirty.Should().BeTrue();
            }
            finally { window.Close(); }
        });

    [BundledExportIntegrationFact]
    public Task UgemteRedigeringerEksporteresUdenAtAEndreMarkdown() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = ShowWindow(workspace.ViewModel);
            try
            {
                var vm = workspace.ViewModel;
                var anna = vm.People.Single(person => person.DisplayName == "Anna Jensen");
                vm.SelectedPerson = anna;
                var editor = vm.Editor!;
                var originalBytes = await File.ReadAllBytesAsync(anna.MarkdownFilePath);
                editor.MarkdownText.Should().Contain(BiographyGeneratedSectionMerger.StartMarker);
                editor.MarkdownText = editor.MarkdownText.Replace(
                    BiographyGeneratedSectionMerger.StartMarker,
                    BiographyGeneratedSectionMerger.StartMarker + "\n\nÆndret genereret tekst æøå.", StringComparison.Ordinal)
                    + "\n\nUgemt fri biografi æøå.";
                var snapshot = vm.CreateBiographyExportSnapshot([anna.RecordId]);
                snapshot.Errors.Should().BeEmpty();
                var roots = new[] { vm.StandardMarkdownOutputFolder! };
                var preflight = new BiographyExportPreflightService().Prepare(snapshot.Items, BiographyExportMode.Combined, roots, CancellationToken.None);
                var sidecar = new LocalExportSidecar(new LocalProcessRunner(),
                    Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")!, null,
                    Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES"),
                    Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS"));
                foreach (var format in new[] { BiographyExportFormat.Pdf, BiographyExportFormat.Docx, BiographyExportFormat.Odt })
                {
                    var destination = Path.Combine(Path.GetDirectoryName(anna.MarkdownFilePath)!, "eksport-test." + format.ToString().ToLowerInvariant());
                    var targets = BiographyExportService.PlanTargets(snapshot.Items, BiographyExportMode.Combined, format, destination);
                    var result = await new BiographyExportService(sidecar, new AtomicFileWriter()).ExportAsync(
                        targets, BiographyExportMode.Combined, format, roots, null, CancellationToken.None, [], preflight.Warnings, preflight.Fingerprints);
                    result.Errors.Should().BeEmpty();
                    result.Saved.Should().Equal(destination);
                    if (format == BiographyExportFormat.Pdf)
                    {
                        File.ReadAllBytes(destination).Should().StartWith("%PDF-"u8.ToArray());
                        var python = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_PDF_READER_PYTHON");
                        if (!string.IsNullOrWhiteSpace(python))
                        {
                            var start = new System.Diagnostics.ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                            start.ArgumentList.Add("-c");
                            start.ArgumentList.Add("import sys; from pypdf import PdfReader; print('\\n'.join(page.extract_text() for page in PdfReader(sys.argv[1]).pages))");
                            start.ArgumentList.Add(destination);
                            using var process = System.Diagnostics.Process.Start(start)!;
                            var text = await process.StandardOutput.ReadToEndAsync();
                            var error = await process.StandardError.ReadToEndAsync();
                            await process.WaitForExitAsync();
                            process.ExitCode.Should().Be(0, error);
                            text.Should().Contain("Ændret genereret tekst æøå.").And.Contain("Ugemt fri biografi æøå.");
                            text.Should().NotContain("SlaegtsAssistent:generated").And.NotContain("formatVersion");
                        }
                    }
                    else
                    {
                        using var archive = ZipFile.OpenRead(destination);
                        var entry = archive.GetEntry(format == BiographyExportFormat.Docx ? "word/document.xml" : "content.xml")!;
                        using var reader = new StreamReader(entry.Open());
                        var xml = await reader.ReadToEndAsync();
                        xml.Should().Contain("Ændret genereret tekst æøå.").And.Contain("Ugemt fri biografi æøå.");
                        xml.Should().NotContain("SlaegtsAssistent:generated").And.NotContain("formatVersion");
                    }
                    (await File.ReadAllBytesAsync(anna.MarkdownFilePath)).Should().Equal(originalBytes);
                    editor.IsDirty.Should().BeTrue();
                }
            }
            finally { window.Close(); }
        });

    [Fact]
    public Task ContextPanel_ShouldPreserveSelectionAndEditorAcrossResizingAndToggling() =>
        HeadlessTestApplication.RunAsync(async () =>
        {
            using var workspace = await CreateWorkspaceAsync();
            var window = new MainWindow { DataContext = workspace.ViewModel, Width = 960, Height = 600 };
            var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
            ((Panel)webView.Parent!).Children.Remove(webView);
            try
            {
                window.Show();
                Flush();
                var grid = window.FindControl<Grid>("WorkspaceGrid")!;
                var context = window.FindControl<Border>("ContextPanel")!;
                var toggle = window.FindControl<Button>("ContextToggle")!;
                context.IsVisible.Should().BeFalse();
                grid.ColumnDefinitions[4].Width.Value.Should().Be(0);

                var selected = workspace.ViewModel.SelectedPerson;
                var editor = workspace.ViewModel.Editor!;
                editor.MarkdownText = "Ugemt tekst æøå";
                toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Flush();
                context.IsVisible.Should().BeTrue();
                grid.ColumnDefinitions[0].ActualWidth.Should().BeGreaterThanOrEqualTo(220);
                grid.ColumnDefinitions[2].ActualWidth.Should().BeGreaterThanOrEqualTo(420);
                toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                window.Width = 1220;
                Flush();
                toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                Flush();
                context.IsVisible.Should().BeTrue();
                workspace.ViewModel.SelectedPerson.Should().BeSameAs(selected);
                workspace.ViewModel.Editor.Should().BeSameAs(editor);
                editor.MarkdownText.Should().Be("Ugemt tekst æøå");
                editor.IsDirty.Should().BeTrue();
            }
            finally { window.Close(); }
        });

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

    private static async Task<Workspace> CreateWorkspaceAsync(bool withMedia = false)
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

        if (withMedia)
        {
            var content = File.ReadAllText(gedcomPath).Replace("0 TRLR", "1 OBJE\n2 FILE foto.png\n2 TITL Anna\n0 TRLR", StringComparison.Ordinal);
            File.WriteAllText(gedcomPath, content);
            File.WriteAllBytes(Path.Combine(root, "foto.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC"));
        }

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
        public string Root { get; } = root;
        public MainWindowViewModel ViewModel { get; } = viewModel;

        public CountingUnsavedChangesDialogService UnsavedDialog { get; } = unsavedDialog;

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
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
