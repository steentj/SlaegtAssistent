using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Views;

public partial class MediaPickerWindow : Window
{
    private readonly MediaPickerViewModel _model;
    private readonly IMediaInspectionService _inspection;
    private readonly MediaThumbnailCache _cache;
    private readonly Func<CancellationToken, Task<SlaegtsAssistent.Core.Biography.MediaFileIndex?>>? _refreshIndex;
    private readonly CancellationTokenSource _closing = new();
    private CancellationTokenSource? _previewCancellation;
    private bool _ready;
    private bool _closed;
    private long _refreshRevision;

    public event EventHandler? SettingsRequested;

    public MediaPickerWindow() : this(
        new MediaPickerViewModel([], string.Empty, new LocalMediaInspectionService(".", null, null, null)),
        new LocalMediaInspectionService(".", null, null, null), new MediaThumbnailCache(), true) { }

    public MediaPickerWindow(MediaPickerViewModel model, IMediaInspectionService inspection,
        MediaThumbnailCache cache, bool portrait,
        Func<CancellationToken, Task<SlaegtsAssistent.Core.Biography.MediaFileIndex?>>? refreshIndex = null)
    {
        _model = model;
        _inspection = inspection;
        _cache = cache;
        _refreshIndex = refreshIndex;
        InitializeComponent();
        Title = portrait ? "Vælg topbillede" : "Indsæt billede i livsforløbet";
        ConfirmButton.Content = portrait ? "Vælg" : "Indsæt";
        Opened += async (_, _) => { _ready = true; await RefreshAsync(); if (!_closed) SearchBox.Focus(); };
        Closed += (_, _) => { _closed = true; _refreshRevision++; _closing.Cancel(); _previewCancellation?.Cancel(); _model.Dispose(); _cache.Dispose(); };
    }

    private async Task RefreshAsync()
    {
        if (!_ready || _closed) return;
        var revision = ++_refreshRevision;
        Gallery.ItemsSource = null;
        LargePreview.Source = null;
        ConfirmButton.IsEnabled = false;
        PageText.Text = "Undersøger medier…";
        await _model.RefreshAsync();
        if (_closed || revision != _refreshRevision) return;
        Gallery.ItemsSource = _model.Items.Select(item => new MediaPickerCard(item,
            item.Inspection.ThumbnailKey is { } key ? _cache.Find(key) : null)).ToArray();
        PreviousButton.IsEnabled = _model.Page > 0;
        NextButton.IsEnabled = _model.Page + 1 < _model.TotalPages;
        PageText.Text = _model.Items.Count == 0 ? "Ingen medier fundet. Kontrollér Indstillinger eller søgningen."
            : $"Side {_model.Page + 1} af {_model.TotalPages}";
    }

    private async void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_ready) return;
        _model.SearchText = SearchBox.Text ?? string.Empty;
        await RefreshAsync();
    }

    private async void ScopeChanged(object? sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _model.ShowAll = ShowAllBox.IsChecked == true;
        await RefreshAsync();
    }

    private async void PreviousClick(object? sender, RoutedEventArgs e) { _model.Page--; await RefreshAsync(); }
    private async void NextClick(object? sender, RoutedEventArgs e) { _model.Page++; await RefreshAsync(); }
    private async void RefreshClick(object? sender, RoutedEventArgs e)
    {
        _refreshRevision++;
        _model.CancelPending();
        Gallery.ItemsSource = null;
        LargePreview.Source = null;
        ConfirmButton.IsEnabled = false;
        RefreshButton.IsEnabled = false;
        SearchBox.IsEnabled = false;
        ShowAllBox.IsEnabled = false;
        PreviousButton.IsEnabled = false;
        NextButton.IsEnabled = false;
        PageText.Text = "Opdaterer mediemappen…";
        try
        {
            if (_inspection is LocalMediaInspectionService local)
            {
                if (_refreshIndex is null) await local.RebuildIndexAsync(_closing.Token);
                else local.SetIndex(await _refreshIndex(_closing.Token));
            }
            if (!_closed) await RefreshAsync();
        }
        catch (OperationCanceledException)
        {
            if (!_closed) PageText.Text = "Opdateringen blev afbrudt. Prøv igen.";
        }
        finally
        {
            if (!_closed) { RefreshButton.IsEnabled = true; SearchBox.IsEnabled = true; ShowAllBox.IsEnabled = true; }
        }
    }
    private void SettingsClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }
    private void CancelClick(object? sender, RoutedEventArgs e) => Close(null);

    private async void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = CancellationTokenSource.CreateLinkedTokenSource(_closing.Token);
        var cancellation = _previewCancellation;
        var card = Gallery.SelectedItem as MediaPickerCard;
        _model.SelectedItem = card?.Model;
        LargePreview.Source = card?.Thumbnail;
        Details.Text = card is null ? "Vælg et medie for at se oplysninger." :
            $"{card.Title}\nFil: {card.FileName}\nReference: {card.Model.Item.Reference}\nSti: {card.Model.Inspection.ResolvedPath ?? "Ikke fundet"}\n{card.Status}";
        AlternativeTextBox.Text = card?.Title ?? string.Empty;
        ConfirmButton.IsEnabled = card?.Model.Inspection.CanSelect == true;
        if (card?.Model.Inspection.CanSelect == true && _inspection is LocalMediaInspectionService local)
        {
            try
            {
                var result = await local.InspectPreviewAsync(card.Model.Item, cancellation.Token);
                if (!_closed && !cancellation.IsCancellationRequested && ReferenceEquals(Gallery.SelectedItem, card) &&
                    result.ThumbnailKey is { } key && _cache.Find(key) is { } preview)
                    LargePreview.Source = preview;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        }
    }

    private async void ConfirmClick(object? sender, RoutedEventArgs e)
    {
        var selected = _model.SelectedItem;
        if (selected is null) return;
        ConfirmButton.IsEnabled = false;
        var checkedAgain = await _inspection.InspectAsync(selected.Item, CancellationToken.None);
        if (_closed || !ReferenceEquals(selected, _model.SelectedItem)) return;
        if (!checkedAgain.CanSelect || checkedAgain.RelativePath != selected.Inspection.RelativePath)
        {
            Details.Text = "Billedet er ændret eller kan ikke længere læses. Opdatér og vælg det igen.";
            return;
        }
        _model.AlternativeText = AlternativeTextBox.Text ?? string.Empty;
        _model.Caption = CaptionBox.Text;
        _model.ImageWidth = ImageWidthBox.SelectedIndex switch
        {
            1 => BiographyImageWidth.HalfPage,
            2 => BiographyImageWidth.QuarterPage,
            _ => BiographyImageWidth.FullPage,
        };
        Close(_model.CreateResult());
    }
}

public sealed record MediaPickerCard(MediaPickerItemViewModel Model, Bitmap? Thumbnail)
{
    public string Title => string.IsNullOrWhiteSpace(Model.Item.Title) ? "Uden titel" : Model.Item.Title;
    public string FileName => Model.Item.FileName;
    public string Status => Model.Inspection.Status;
}
