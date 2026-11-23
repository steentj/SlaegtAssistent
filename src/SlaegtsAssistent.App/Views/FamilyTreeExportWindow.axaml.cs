using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class FamilyTreeExportWindow : Window
{
    private readonly FamilyTreeExportViewModel _model;
    private readonly bool _print;
    private CancellationTokenSource? _cancellation;
    private bool _closeWhenFinished;

    public FamilyTreeExportWindow() : this(
        new FamilyTreeExportViewModel(new SlaegtsAssistent.Core.FamilyTrees.FamilyTreeIndex(
            new SlaegtsAssistent.Core.Domain.FamilyTree()), "", 0), false) { }

    public FamilyTreeExportWindow(FamilyTreeExportViewModel model, bool print)
    {
        InitializeComponent();
        _model = model;
        _print = print;
        DataContext = model;
        FormatPanel.IsVisible = !print;
        Title = print ? "Udskriv slægtstræ" : "Eksportér slægtstræ";
        StartButton.Content = print ? "Udskriv…" : "Eksportér…";
        Closing += (_, args) =>
        {
            if (_cancellation is null) return;
            args.Cancel = true;
            _closeWhenFinished = true;
            _cancellation.Cancel();
        };
    }

    private void CloseClick(object? sender, RoutedEventArgs args) => Close();

    private void CancelClick(object? sender, RoutedEventArgs args)
    {
        if (_cancellation is null) return;
        _model.Status = "Annullerer handlingen…";
        _cancellation.Cancel();
    }

    private async void StartClick(object? sender, RoutedEventArgs args) => await StartAsync();

    private async Task StartAsync()
    {
        if (_cancellation is not null || _model.Plan is not { } plan) return;
        var sidecar = LocalExportSidecar.TryCreateDefault(new LocalProcessRunner());
        if (sidecar is null)
        {
            _model.Status = "Det lokale eksportværktøj mangler i programpakken.";
            return;
        }
        var format = _print ? "pdf" : _model.Format;
        var paper = _model.Paper;
        var cancellation = _cancellation = new CancellationTokenSource();
        _model.IsExporting = true;
        try
        {
            string? destination = null;
            var destinationExisted = false;
            if (!_print)
            {
                destination = await ChooseDestinationAsync(format, plan.OriginalAncestorNumber);
                cancellation.Token.ThrowIfCancellationRequested();
                if (destination is null)
                {
                    _model.Status = "Eksporten blev annulleret.";
                    return;
                }
                destinationExisted = File.Exists(destination);
            }
            _model.Status = "Danner slægtstræet…";
            var bytes = await sidecar.RenderFamilyTreeAsync(plan, format, paper, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if (!_print)
            {
                var writer = new AtomicFileWriter();
                if (destinationExisted) writer.WriteBytes(destination!, bytes);
                else writer.WriteNewBytes(destination!, bytes);
                _model.Status = "Slægtstræet er gemt.";
                return;
            }
            var path = PrintDocumentStore.Save(bytes);
            try
            {
                new SystemDocumentLauncher().OpenPdf(path);
                _model.Status = "PDF er åbnet. Vælg Udskriv i PDF-fremviseren.";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                var fallback = await ChooseDestinationAsync("pdf", plan.OriginalAncestorNumber);
                cancellation.Token.ThrowIfCancellationRequested();
                if (fallback is null)
                {
                    _model.Status = "PDF-fremviseren kunne ikke åbnes. PDF-filen blev ikke gemt.";
                    return;
                }
                var writer = new AtomicFileWriter();
                if (File.Exists(fallback)) writer.WriteBytes(fallback, bytes);
                else writer.WriteNewBytes(fallback, bytes);
                _model.Status = "PDF-fremviseren kunne ikke åbnes. PDF-filen er gemt.";
            }
        }
        catch (OperationCanceledException) { _model.Status = "Handlingen blev annulleret."; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        { _model.Status = "Handlingen kunne ikke gennemføres: " + exception.Message; }
        finally
        {
            _cancellation = null;
            _model.IsExporting = false;
            cancellation.Dispose();
            if (_closeWhenFinished) Close();
        }
    }

    private async Task<string?> ChooseDestinationAsync(string extension, string ancestorNumber)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Gem slægtstræ",
            SuggestedFileName = $"slaegtstrae-ane-{ancestorNumber}.{extension}",
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType("Dokument") { Patterns = ["*." + extension] }]
        });
        var path = file?.TryGetLocalPath();
        if (path is not null && !string.Equals(Path.GetExtension(path), "." + extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Filens endelse skal svare til det valgte format.");
        return path;
    }
}
