using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly string _filePath;
    private readonly IMarkdownFileStore _markdownFileStore;
    private bool _suppressDirtyTracking;

    public EditorViewModel(string filePath, IMarkdownFileStore markdownFileStore)
    {
        _filePath = filePath;
        _markdownFileStore = markdownFileStore;
    }

    [ObservableProperty]
    private string markdownText = string.Empty;

    [ObservableProperty]
    private PreviewMode previewMode = PreviewMode.Web;

    [ObservableProperty]
    private bool isDirty;

    public string PreviewHtml => string.IsNullOrWhiteSpace(MarkdownText)
        ? string.Empty
        : Markdown.ToHtml(MarkdownText);

    public Uri PreviewWebUri => string.IsNullOrWhiteSpace(PreviewHtml)
        ? new Uri("about:blank")
        : new Uri(
            "data:text/html;charset=utf-8," +
            Uri.EscapeDataString(
                $"<!doctype html><html><head><meta charset=\"utf-8\"></head><body>{PreviewHtml}</body></html>"));

    public bool IsWebPreviewSelected
    {
        get => PreviewMode == PreviewMode.Web;
        set
        {
            if (value)
            {
                PreviewMode = PreviewMode.Web;
            }
        }
    }

    public bool IsHtmlPreviewSelected
    {
        get => PreviewMode == PreviewMode.Html;
        set
        {
            if (value)
            {
                PreviewMode = PreviewMode.Html;
            }
        }
    }

    public void Load()
    {
        _suppressDirtyTracking = true;
        try
        {
            MarkdownText = _markdownFileStore.Read(_filePath);
            IsDirty = false;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        _markdownFileStore.Write(_filePath, MarkdownText ?? string.Empty);
        IsDirty = false;
    }

    partial void OnMarkdownTextChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewHtml));
        OnPropertyChanged(nameof(PreviewWebUri));

        if (!_suppressDirtyTracking)
        {
            IsDirty = true;
        }
    }

    partial void OnPreviewModeChanged(PreviewMode value)
    {
        OnPropertyChanged(nameof(IsWebPreviewSelected));
        OnPropertyChanged(nameof(IsHtmlPreviewSelected));
    }
}
