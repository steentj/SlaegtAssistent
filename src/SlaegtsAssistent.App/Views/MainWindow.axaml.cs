using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Services;
using Avalonia.Platform.Storage;
using SlaegtsAssistent.Core.Graph.Export;
using System.Text;
using System.Threading;

namespace SlaegtsAssistent.App.Views;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _exportCancellation;
    private void GraphCancelExport(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _exportCancellation?.Cancel();
    private CancellationTokenSource BeginGraphExport()
    {
        if (_exportCancellation is not null) throw new InvalidOperationException("En grafeksport er allerede i gang.");
        var cancellation = _exportCancellation = new CancellationTokenSource();
        CancelGraphExport.IsEnabled = true;
        return cancellation;
    }
    private void EndGraphExport(CancellationTokenSource cancellation)
    {
        if (_exportCancellation == cancellation) _exportCancellation = null;
        CancelGraphExport.IsEnabled = false;
        cancellation.Dispose();
    }
    private void GraphZoomIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GraphControl.ZoomBy(1.2);
    private void GraphZoomOut(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GraphControl.ZoomBy(1 / 1.2);
    private void GraphShowAll(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GraphControl.ShowAll();
    private void GraphCenterProband(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => GraphControl.CenterProband();
    private GraphExportOptions CaptureExportOptions(string? proband)
    {
        var labels = ExportLabels.IsChecked == true;
        if (ExportViewport.IsChecked != true) return new(GraphExportScope.WholeGraph, labels, proband);
        var viewport = GraphControl.GetVisibleWorldBounds();
        return new(GraphExportScope.CurrentViewport, labels, proband, viewport.Bounds, viewport.Origin);
    }
    private async void GraphPrint(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.PersonGraph.Layout is not { } layout)
        {
            if (DataContext is MainWindowViewModel missing) missing.PersonGraph.Status = "Indlæs grafen før udskrift.";
            return;
        }
        var sidecar = LocalExportSidecar.TryCreateDefault(new LocalProcessRunner());
        if (sidecar is null) { viewModel.PersonGraph.Status = "Det lokale PDF-værktøj mangler i programpakken. Vælg PDF for at gemme, når værktøjet er tilgængeligt."; return; }
        if (_exportCancellation is not null) { viewModel.PersonGraph.Status = "En grafeksport er allerede i gang."; return; }
        var cancellation = BeginGraphExport();
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            var scene = new GraphExportSceneBuilder().Build(viewModel.PersonGraph.Graph, layout, CaptureExportOptions(viewModel.PersonGraph.ProbandId));
            var html = "<html><body>" + new GraphSvgWriter().Write(scene) + "</body></html>";
            var paper = ExportPaper.SelectedIndex >= 2 ? "A3" : "A4";
            var orientation = ExportPaper.SelectedIndex % 2 == 1 ? "portrait" : "landscape";
            viewModel.PersonGraph.Status = "PDF'en dannes. Vælg Udskriv i PDF-fremviseren for at vælge printer.";
            var pdf = await sidecar.RenderGraphPdfAsync(html, paper, orientation, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            var path = PrintDocumentStore.Save(pdf);
            try
            {
                new SystemDocumentLauncher().OpenPdf(path);
                viewModel.PersonGraph.Status = "PDF åbnet til udskrift. Vælg Udskriv i PDF-fremviseren for at vælge printer.";
            }
            catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                viewModel.PersonGraph.Status = "PDF-fremviseren kunne ikke åbnes. Vælg PDF for at gemme filen i stedet.";
            }
        }
        catch (OperationCanceledException) { viewModel.PersonGraph.Status = "Udskriften blev annulleret."; }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        { viewModel.PersonGraph.Status = $"PDF til udskrift kunne ikke dannes: {exception.Message}"; }
        finally { EndGraphExport(cancellation); if (sender is Button printButton) printButton.IsEnabled = true; }
    }
    private async void GraphExportPng(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.PersonGraph.Layout is not { } layout)
        {
            if (DataContext is MainWindowViewModel missing) missing.PersonGraph.Status = "Indlæs grafen før eksport.";
            return;
        }
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            var scene = new GraphExportSceneBuilder().Build(viewModel.PersonGraph.Graph, layout,
                CaptureExportOptions(viewModel.PersonGraph.ProbandId));
            var resolution = ExportPngSize.SelectedIndex switch { 0 => 2048, 2 => 8192, _ => 4096 };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Gem graf som PNG",
                SuggestedFileName = "personnetværk.png",
                DefaultExtension = "png",
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("PNG-billede") { Patterns = ["*.png"] }],
            });
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null) { viewModel.PersonGraph.Status = "Vælg en lokal fil til grafen."; return; }
            viewModel.PersonGraph.Status = "PNG-billedet dannes…";
            var png = new AvaloniaGraphRasterizer().Render(scene, resolution);
            new AtomicFileWriter().WriteBytes(path, png);
            viewModel.PersonGraph.Status = "Grafen er gemt som PNG.";
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        { viewModel.PersonGraph.Status = $"Grafen kunne ikke gemmes som PNG: {exception.Message}"; }
        finally { if (sender is Button exportButton) exportButton.IsEnabled = true; }
    }
    private async void GraphExportPdf(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.PersonGraph.Layout is not { } layout)
        {
            if (DataContext is MainWindowViewModel missing) missing.PersonGraph.Status = "Indlæs grafen før eksport.";
            return;
        }
        var sidecar = LocalExportSidecar.TryCreateDefault(new LocalProcessRunner());
        if (sidecar is null)
        {
            viewModel.PersonGraph.Status = "Det lokale PDF-værktøj mangler i programpakken.";
            return;
        }
        if (_exportCancellation is not null) { viewModel.PersonGraph.Status = "En grafeksport er allerede i gang."; return; }
        var cancellation = BeginGraphExport();
        if (sender is Button button) button.IsEnabled = false;
        try
        {
            var scene = new GraphExportSceneBuilder().Build(viewModel.PersonGraph.Graph, layout,
                CaptureExportOptions(viewModel.PersonGraph.ProbandId));
            var paper = ExportPaper.SelectedIndex >= 2 ? "A3" : "A4";
            var orientation = ExportPaper.SelectedIndex % 2 == 1 ? "portrait" : "landscape";
            var html = "<html><body>" + new GraphSvgWriter().Write(scene) + "</body></html>";
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Gem graf som PDF",
                SuggestedFileName = "personnetværk.pdf",
                DefaultExtension = "pdf",
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("PDF-dokument") { Patterns = ["*.pdf"] }],
            });
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null) { viewModel.PersonGraph.Status = "Vælg en lokal fil til grafen."; return; }
            viewModel.PersonGraph.Status = "PDF-filen dannes…";
            var pdf = await sidecar.RenderGraphPdfAsync(html, paper, orientation, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            new AtomicFileWriter().WriteBytes(path, pdf);
            viewModel.PersonGraph.Status = "Grafen er gemt som PDF.";
        }
        catch (OperationCanceledException) { viewModel.PersonGraph.Status = "PDF-eksporten blev annulleret."; }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception)
        {
            viewModel.PersonGraph.Status = $"Grafen kunne ikke gemmes som PDF: {exception.Message}";
        }
        finally { EndGraphExport(cancellation); if (sender is Button exportButton) exportButton.IsEnabled = true; }
    }
    private async void GraphExportSvg(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.PersonGraph.Layout is not { } layout)
        {
            if (DataContext is MainWindowViewModel missing) missing.PersonGraph.Status = "Indlæs grafen før eksport.";
            return;
        }
        try
        {
            var scene = new GraphExportSceneBuilder().Build(viewModel.PersonGraph.Graph, layout,
                CaptureExportOptions(viewModel.PersonGraph.ProbandId));
            var svg = new GraphSvgWriter().Write(scene);
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Gem graf som SVG",
                SuggestedFileName = "personnetværk.svg",
                DefaultExtension = "svg",
                ShowOverwritePrompt = true,
                FileTypeChoices = [new FilePickerFileType("SVG-billede") { Patterns = ["*.svg"] }],
            });
            if (file is null) return;
            var path = file.TryGetLocalPath();
            if (path is null) { viewModel.PersonGraph.Status = "Vælg en lokal fil til grafen."; return; }
            new AtomicFileWriter().WriteText(path, svg, new UTF8Encoding(false));
            viewModel.PersonGraph.Status = "Grafen er gemt som SVG.";
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or AtomicFileWriteException)
        {
            viewModel.PersonGraph.Status = $"Grafen kunne ikke gemmes: {exception.Message}";
        }
    }
    private void GraphListKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            viewModel.PersonGraph.OpenBiographyCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Escape)
        {
            viewModel.PersonGraph.Select(null);
            viewModel.PersonGraph.Hover(null);
            e.Handled = true;
        }
    }
    private bool _canCloseWithoutPrompt;
    private EditorViewModel? _subscribedEditor;
    private bool _previewAdapterReady;

    public MainWindow()
    {
        InitializeComponent();
        SafePreviewWebViewController.Attach(PreviewWebView, this, destination =>
            (DataContext as MainWindowViewModel)?.Editor?.TryNavigateToPerson(destination) == true);
        DataContextChanged += HandleDataContextChanged;
        PreviewWebView.AdapterCreated += (_, _) =>
        {
            _previewAdapterReady = true;
            RefreshPreview();
        };
        PreviewWebView.AdapterDestroyed += (_, _) => _previewAdapterReady = false;
        Application.Current?.ActualThemeVariantChanged += HandleThemeVariantChanged;
        Closed += HandleClosed;
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        Closing += HandleClosing;
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        AttachEditor(viewModel.Editor);
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Editor) &&
            sender is MainWindowViewModel viewModel)
        {
            AttachEditor(viewModel.Editor);
        }
    }

    private void AttachEditor(EditorViewModel? editor)
    {
        if (_subscribedEditor is not null)
        {
            _subscribedEditor.PropertyChanged -= HandleEditorPropertyChanged;
        }

        _subscribedEditor = editor;
        if (_subscribedEditor is not null)
        {
            _subscribedEditor.PropertyChanged += HandleEditorPropertyChanged;
        }

        RefreshPreview();
    }

    private void HandleEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditorViewModel.PreviewHtml)
            or nameof(EditorViewModel.PreviewHtmlDocument)
            or nameof(EditorViewModel.IsWebPreviewSelected))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (!_previewAdapterReady ||
            DataContext is not MainWindowViewModel { Editor: { } editor })
        {
            return;
        }

        PreviewWebView.NavigateToString(CheatSheetPreviewTheme.Apply(editor.PreviewHtmlDocument));
    }

    private void HandleThemeVariantChanged(object? sender, EventArgs e)
    {
        RefreshPreview();
    }

    private void HandleClosed(object? sender, EventArgs e)
    {
        Application.Current?.ActualThemeVariantChanged -= HandleThemeVariantChanged;
    }

    private async void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_canCloseWithoutPrompt)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (!viewModel.HasDirtyEditors)
        {
            return;
        }

        e.Cancel = true;

        if (!await viewModel.ConfirmCloseAsync())
        {
            return;
        }

        _canCloseWithoutPrompt = true;
        Close();
    }
}
