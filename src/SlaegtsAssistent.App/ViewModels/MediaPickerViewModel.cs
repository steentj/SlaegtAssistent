using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MediaPickerViewModel : ViewModelBase, IDisposable
{
    public const int PageSize = 60;
    private readonly IReadOnlyList<MediaCatalogItem> _catalog;
    private readonly string _personId;
    private readonly IMediaInspectionService _inspection;
    private readonly SemaphoreSlim _inspectionSlots = new(2);
    private CancellationTokenSource? _cancellation;
    private long _generation;
    private bool _disposed;

    public MediaPickerViewModel(IReadOnlyList<MediaCatalogItem> catalog, string personId, IMediaInspectionService inspection)
    {
        _catalog = catalog;
        _personId = personId;
        _inspection = inspection;
    }

    public ObservableCollection<MediaPickerItemViewModel> Items { get; } = [];
    [ObservableProperty] private bool showAll;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int page;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private MediaPickerItemViewModel? selectedItem;
    [ObservableProperty] private string alternativeText = string.Empty;
    [ObservableProperty] private string? caption;
    [ObservableProperty] private BiographyImageWidth imageWidth = BiographyImageWidth.FullPage;

    partial void OnShowAllChanged(bool value) { Page = 0; Invalidate(); }
    partial void OnSearchTextChanged(string value) { Page = 0; Invalidate(); }
    partial void OnPageChanged(int value) => Invalidate();

    private void Invalidate()
    {
        _generation++;
        _cancellation?.Cancel();
        SelectedItem = null;
        Items.Clear();
        IsBusy = false;
    }

    public void CancelPending() => Invalidate();

    public async Task RefreshAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Invalidate();
        var query = _catalog.Where(item => ShowAll || item.OwnerId == _personId)
            .Where(item => string.IsNullOrWhiteSpace(SearchText) ||
                (item.Title?.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                item.FileName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        TotalPages = Math.Max(1, (query.Length + PageSize - 1) / PageSize);
        Page = Math.Clamp(Page, 0, TotalPages - 1);
        var selected = query.Skip(Page * PageSize).Take(PageSize).ToArray();
        _cancellation?.Dispose();
        var cancellation = _cancellation = new CancellationTokenSource();
        var generation = ++_generation;
        IsBusy = true;
        try
        {
            var inspected = await Task.WhenAll(selected.Select(item => InspectAsync(item, cancellation.Token)));
            if (_disposed || generation != _generation || cancellation.IsCancellationRequested) return;
            Items.Clear();
            foreach (var item in inspected) Items.Add(item);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            if (!_disposed && generation == _generation) IsBusy = false;
        }
    }

    private async Task<MediaPickerItemViewModel> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken)
    {
        await _inspectionSlots.WaitAsync(cancellationToken);
        try
        {
            var result = await _inspection.InspectAsync(item, cancellationToken);
            return new(item, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(item, new MediaInspection(false, "Filen kan ikke læses. Vælg et andet billede.", null));
        }
        finally { _inspectionSlots.Release(); }
    }

    public MediaSelectionResult? CreateResult() => SelectedItem is { Inspection.CanSelect: true, Inspection.RelativePath: { } path }
        ? new(path, AlternativeText, string.IsNullOrWhiteSpace(Caption) ? null : Caption, ImageWidth)
        : null;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _generation++;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
    }
}

public sealed record MediaPickerItemViewModel(MediaCatalogItem Item, MediaInspection Inspection);
