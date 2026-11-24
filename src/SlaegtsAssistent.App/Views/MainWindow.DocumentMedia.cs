using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Views;

public partial class MainWindow
{
    private readonly DocumentMediaThumbnails _documentMediaThumbnails = new();
    private readonly Dictionary<Uri, DocumentMediaCard> _documentMediaLinks = new();
    private CancellationTokenSource? _documentMediaCancellation;
    private DocumentImageWindow? _documentImageWindow;

    private void CancelDocumentMediaPreview()
    {
        _documentMediaLinks.Clear();
        var pending = _documentMediaCancellation;
        _documentMediaCancellation = null;
        pending?.Cancel();
    }

    private async Task RefreshDocumentMediaPreviewAsync(MainWindowViewModel model, EditorViewModel editor, string ownerId)
    {
        using var pending = new CancellationTokenSource();
        _documentMediaCancellation = pending;
        var token = pending.Token;
        var showThumbnails = model.ShowDocumentMediaThumbnails;
        var templatePath = model.GlobalBiographyTemplatePath;
        var document = new BiographyDocument(editor.DocumentMetadata, editor.MarkdownText, false);
        try
        {
            var section = DocumentMediaSection.Find(document, ownerId, editor.DocumentPath, templatePath);
            if (section is null)
            {
                NavigatePreview(editor.PreviewHtmlDocument);
                return;
            }

            var initial = section.Items.Select(item => new DocumentMediaCard(item,
                LocalMediaInspectionService.IsSupportedImage(item.Reference)
                    ? new Uri($"{DocumentMediaSection.NavigationScheme}://billede/{Guid.NewGuid():N}")
                    : null)).ToArray();
            Publish(initial);

            if (!initial.Any(card => card.Navigation is not null)) return;
            var inspection = await CreateDocumentMediaInspectionAsync(model, editor.DocumentPath, token);
            var cards = new DocumentMediaCard[initial.Length];
            await Parallel.ForEachAsync(Enumerable.Range(0, initial.Length),
                new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = token },
                async (index, cancellationToken) =>
                {
                    cards[index] = await _documentMediaThumbnails.InspectAsync(
                        initial[index], inspection, showThumbnails, cancellationToken);
                });

            if (ReferenceEquals(_documentMediaCancellation, pending) && !cards.SequenceEqual(initial))
                Publish(cards);

            void Publish(IReadOnlyList<DocumentMediaCard> media)
            {
                var html = editor.RenderMediaPreview(section, media);
                _documentMediaLinks.Clear();
                foreach (var card in media)
                    if (card.Navigation is { } destination) _documentMediaLinks.Add(destination, card);
                NavigatePreview(html);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            if (ReferenceEquals(_documentMediaCancellation, pending))
                model.ErrorMessage = $"Medieoversigten kunne ikke indlæses: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(_documentMediaCancellation, pending)) _documentMediaCancellation = null;
        }
    }

    private void NavigatePreview(string html) =>
        PreviewWebView.NavigateToString(CheatSheetPreviewTheme.Apply(html));

    private static async Task<LocalMediaInspectionService> CreateDocumentMediaInspectionAsync(
        MainWindowViewModel model, string documentPath, CancellationToken token)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(documentPath))!;
        var gedcomDirectory = Path.GetDirectoryName(model.SelectedGedcomFilePath);
        var mediaDirectory = model.StandardMediaFolder;
        var index = await model.GetMediaIndexAsync(token);
        return new LocalMediaInspectionService(directory, gedcomDirectory, mediaDirectory, index);
    }

    private void OpenDocumentMedia(Uri destination)
    {
        if (!_documentMediaLinks.TryGetValue(destination, out var card) ||
            DataContext is not MainWindowViewModel { Editor: { } editor } model) return;

        if (_documentImageWindow is null)
        {
            var window = new DocumentImageWindow();
            window.Closed += (_, _) => _documentImageWindow = null;
            _documentImageWindow = window;
            window.Show(this);
        }

        _documentImageWindow.Activate();
        _ = _documentImageWindow.OpenImageAsync(card,
            token => CreateDocumentMediaInspectionAsync(model, editor.DocumentPath, token));
    }
}
