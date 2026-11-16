using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.ViewModels;

public partial class BiographyExportPersonViewModel(MarkdownDocumentInfo document) : ViewModelBase
{
    public MarkdownDocumentInfo Document { get; } = document;
    public string DisplayName => Document.DisplayName;
    public string RecordId => Document.RecordId;
    public string ErrorMessage => string.IsNullOrWhiteSpace(Document.ErrorMessage) ? Document.ErrorCategory ?? "" : Document.ErrorMessage;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    [ObservableProperty] private bool isSelected;
}

public partial class BiographyExportViewModel : ViewModelBase
{
    private readonly List<BiographyExportPersonViewModel> _all;
    public ObservableCollection<BiographyExportPersonViewModel> VisiblePeople { get; } = [];
    public IReadOnlyList<string> FormatNames { get; } = ["Word (DOCX)", "PDF", "OpenDocument (ODT)"];
    public IReadOnlyList<string> ModeNames { get; } = ["Ét samlet dokument", "Én fil pr. person"];
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private int formatIndex;
    [ObservableProperty] private int modeIndex;
    [ObservableProperty] private bool isExporting;
    [ObservableProperty] private string status = "Vælg højst 100 biografier.";
    public BiographyExportFormat Format => FormatIndex switch { 1 => BiographyExportFormat.Pdf, 2 => BiographyExportFormat.Odt, _ => BiographyExportFormat.Docx };
    public BiographyExportMode Mode => ModeIndex == 1 ? BiographyExportMode.Separate : BiographyExportMode.Combined;
    public string SelectedCountText => $"{_all.Count(x => x.IsSelected)} valgt (højst 100)";
    public IReadOnlyList<string> SelectedIds => _all.Where(x => x.IsSelected).Select(x => x.RecordId).Distinct(StringComparer.Ordinal).ToArray();

    public BiographyExportViewModel(IEnumerable<MarkdownDocumentInfo> documents, string? currentId)
    {
        _all = documents.OrderBy(x => x.DisplayName, StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false))
            .ThenBy(x => x.RecordId, StringComparer.Ordinal).Select(x => new BiographyExportPersonViewModel(x)).ToList();
        foreach (var item in _all)
        {
            item.IsSelected = currentId is not null && item.RecordId == currentId;
            item.PropertyChanged += SelectionChanged;
        }
        RefreshFilter();
    }

    private void SelectionChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(BiographyExportPersonViewModel.IsSelected)) OnPropertyChanged(nameof(SelectedCountText));
    }
    partial void OnSearchTextChanged(string value) => RefreshFilter();
    private void RefreshFilter()
    {
        var search = SearchText?.Trim() ?? "";
        VisiblePeople.Clear();
        foreach (var person in _all.Where(x => x.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase) || x.RecordId.Contains(search, StringComparison.OrdinalIgnoreCase))) VisiblePeople.Add(person);
    }
    [RelayCommand] private void SelectAll() { foreach (var item in _all) item.IsSelected = true; }
    [RelayCommand] private void ClearSelection() { foreach (var item in _all) item.IsSelected = false; }
}
