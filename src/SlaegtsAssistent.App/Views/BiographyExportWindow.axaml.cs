using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Views;

public partial class BiographyExportWindow : Window
{
    private readonly MainWindowViewModel _main;
    private readonly BiographyExportViewModel _model;
    private CancellationTokenSource? _cancellation;
    private bool _closeWhenFinished;

    public BiographyExportWindow() : this(new MainWindowViewModel()) { }

    public BiographyExportWindow(MainWindowViewModel main)
    {
        InitializeComponent();
        _main = main;
        _model = new BiographyExportViewModel(main.GetBiographyExportDocuments(), main.SelectedPerson?.RecordId);
        DataContext = _model;
        Closing += (_, e) =>
        {
            if (_cancellation is null) return;
            e.Cancel = true;
            _closeWhenFinished = true;
            CancelOperation();
        };
        KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            if (_cancellation is null) Close();
            else CancelOperation();
        };
    }

    private async void ExportClick(object? sender, RoutedEventArgs e) => await StartAsync(false);
    private async void PrintClick(object? sender, RoutedEventArgs e) => await StartAsync(true);
    private void CancelClick(object? sender, RoutedEventArgs e) => CancelOperation();
    private void CancelOperation()
    {
        if (_cancellation is null) return;
        _model.Status = "Annullerer handlingen. Vent, mens arbejdet afsluttes sikkert…";
        _cancellation.Cancel();
    }
    private void CloseClick(object? sender, RoutedEventArgs e) => Close();

    private async Task StartAsync(bool print)
    {
        if (!_model.CanStart || _cancellation is not null) return;
        var sidecar = LocalExportSidecar.TryCreateDefault(new LocalProcessRunner());
        if (sidecar is null) { _model.Status = "Det lokale eksportværktøj mangler i programpakken."; return; }
        var ids = _model.SelectedIds;
        var snapshot = _main.CreateBiographyExportSnapshot(ids);
        if (snapshot.Errors.Count > 0)
        {
            _model.Status = string.Join("\n", snapshot.Errors.Select(error => $"{error.DisplayName}: {error.Message}"));
            return;
        }
        var format = print ? BiographyExportFormat.Pdf : _model.Format;
        var mode = print ? BiographyExportMode.Combined : _model.Mode;
        var roots = new[] { _main.StandardMarkdownOutputFolder, Path.GetDirectoryName(_main.SelectedGedcomFilePath ?? ""), _main.StandardMediaFolder }
            .Where(path => !string.IsNullOrWhiteSpace(path)).Select(path => path!).ToArray();
        var cancellation = _cancellation = new CancellationTokenSource();
        _model.IsExporting = true;
        try
        {
            _model.Status = "Forbereder eksporten…";
            var preflight = await Task.Run(() => new BiographyExportPreflightService().Prepare(snapshot.Items, mode, roots, cancellation.Token), cancellation.Token);
            var warnings = preflight.Warnings;
            cancellation.Token.ThrowIfCancellationRequested();
            if (print)
            {
                if (warnings.Count > 0 && !await ConfirmAsync("Advarsler ved udskrift", string.Join("\n", warnings), "Fortsæt")) { _model.Status = "Udskriften blev annulleret."; return; }
                _model.Status = "Danner PDF til udskrift…";
                var converted = await Task.Run(() => sidecar.RenderBiographyAsync(snapshot.Items, BiographyExportMode.Combined, BiographyExportFormat.Pdf, roots, cancellation.Token), cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (!string.Equals(converted.Fingerprint, preflight.Fingerprints[0], StringComparison.Ordinal))
                    throw new InvalidOperationException("Dokumentindhold eller billeder blev ændret efter gennemgangen. Gennemgå udskriften igen.");
                var newWarnings = converted.Warnings.Except(warnings, StringComparer.Ordinal).ToArray();
                if (newWarnings.Length > 0 && !await ConfirmAsync("Nye advarsler ved udskrift", string.Join("\n", newWarnings), "Fortsæt")) { _model.Status = "Udskriften blev annulleret."; return; }
                cancellation.Token.ThrowIfCancellationRequested();
                var path = PrintDocumentStore.Save(converted.Content);
                try
                {
                    new SystemDocumentLauncher().OpenPdf(path);
                    _model.Status = "PDF er åbnet. Vælg Udskriv i PDF-fremviseren.";
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    _model.Status = "PDF-fremviseren kunne ikke åbnes. Vælg PDF og Eksportér for at gemme dokumentet i stedet.";
                }
                return;
            }
            var extension = format switch { BiographyExportFormat.Docx => "docx", BiographyExportFormat.Odt => "odt", _ => "pdf" };
            string? destination;
            if (mode == BiographyExportMode.Combined)
            {
                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Gem biografier", SuggestedFileName = "slægtsbiografier." + extension, DefaultExtension = extension,
                    ShowOverwritePrompt = true, FileTypeChoices = [new FilePickerFileType("Dokument") { Patterns = ["*." + extension] }]
                });
                destination = file?.TryGetLocalPath();
            }
            else
            {
                var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Vælg mappe til biografier", AllowMultiple = false });
                destination = folders.FirstOrDefault()?.TryGetLocalPath();
            }
            cancellation.Token.ThrowIfCancellationRequested();
            if (destination is null) { _model.Status = "Eksporten blev annulleret."; return; }
            var targets = BiographyExportService.PlanTargets(snapshot.Items, mode, format, destination);
            if (targets.Any(target => _main.GetBiographyExportDocuments().Any(item => string.Equals(Path.GetFullPath(item.FilePath), target.Path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))))
                throw new InvalidOperationException("En målfil er et af de valgte Markdown-dokumenter.");
            var existing = targets.Where(target => File.Exists(target.Path)).Select(target => target.Path).ToArray();
            var description = "Filer, der dannes:\n" + string.Join("\n", targets.Select(target => target.Path)) +
                (existing.Length == 0 ? "" : "\n\nEksisterende filer, der overskrives:\n" + string.Join("\n", existing)) +
                (warnings.Count == 0 ? "" : "\n\nAdvarsler:\n" + string.Join("\n", warnings));
            if (!await ConfirmAsync("Gennemgå eksport", description, existing.Length == 0 ? "Eksportér" : "Overskriv de viste filer")) { _model.Status = "Eksporten blev annulleret."; return; }
            cancellation.Token.ThrowIfCancellationRequested();
            var progress = new Progress<BiographyExportProgress>(value => { if (!cancellation.IsCancellationRequested) _model.Status = $"{value.Phase}: {value.Current} af {value.Total}…"; });
            var outcome = await Task.Run(() => new BiographyExportService(sidecar, new AtomicFileWriter()).ExportAsync(targets, mode, format, roots, progress, cancellation.Token, existing, warnings, preflight.Fingerprints), cancellation.Token);
            _model.Status = (outcome.Cancelled ? "Eksporten blev annulleret.\n" : "") +
                "Gemte:\n" + (outcome.Saved.Count == 0 ? "Ingen" : string.Join("\n", outcome.Saved)) +
                (outcome.NotSaved.Count == 0 ? "" : "\nIkke gemte:\n" + string.Join("\n", outcome.NotSaved)) +
                (outcome.Errors.Count == 0 ? "" : "\nFejl:\n" + string.Join("\n", outcome.Errors)) +
                (outcome.Warnings.Count == 0 ? "" : "\nAdvarsler:\n" + string.Join("\n", outcome.Warnings));
        }
        catch (OperationCanceledException) { _model.Status = "Eksporten blev annulleret."; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        { _model.Status = "Eksporten kunne ikke gennemføres: " + exception.Message; }
        finally { _cancellation = null; _model.IsExporting = false; cancellation.Dispose(); if (_closeWhenFinished) Close(); }
    }

    private async Task<bool> ConfirmAsync(string title, string message, string accept)
    {
        var result = false;
        var dialog = new Window { Title = title, Width = 620, Height = 440, MinWidth = 420, MinHeight = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var content = new DockPanel { Margin = new Avalonia.Thickness(16) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Annullér" };
        var confirm = new Button { Content = accept };
        cancel.Click += (_, _) => dialog.Close();
        confirm.Click += (_, _) => { result = true; dialog.Close(); };
        buttons.Children.Add(cancel); buttons.Children.Add(confirm);
        DockPanel.SetDock(buttons, Dock.Bottom); content.Children.Add(buttons);
        content.Children.Add(new ScrollViewer { Content = new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap }, Margin = new Avalonia.Thickness(0, 0, 0, 12) });
        dialog.Content = content;
        await dialog.ShowDialog(this);
        return result;
    }
}
