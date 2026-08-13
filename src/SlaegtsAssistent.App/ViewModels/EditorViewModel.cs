using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly string _filePath;
    private readonly IMarkdownFileStore _markdownFileStore;
    private bool _suppressDirtyTracking;
    private BiographyDocumentMetadata? _metadata;
    private readonly SafeMarkdownPreviewService _previewService = new();
    private readonly IReadOnlyCollection<string> _allowedPreviewRoots;
    private string? _cachedPreviewSource;
    private SafeMarkdownPreviewResult? _cachedPreview;

    public EditorViewModel(
        string filePath,
        IMarkdownFileStore markdownFileStore,
        IReadOnlyCollection<string>? allowedPreviewRoots = null)
    {
        _filePath = filePath;
        _markdownFileStore = markdownFileStore;
        _allowedPreviewRoots = allowedPreviewRoots ?? [];
    }

    [ObservableProperty]
    private string markdownText = string.Empty;

    [ObservableProperty]
    private PreviewMode previewMode = PreviewMode.Web;

    [ObservableProperty]
    private bool isDirty;

    public string PreviewHtml => string.IsNullOrWhiteSpace(MarkdownText)
        ? string.Empty
        : CreatePreview().Html;

    public Uri PreviewWebUri => string.IsNullOrWhiteSpace(PreviewHtml)
        ? new Uri("about:blank")
        : new Uri(
            "data:text/html;charset=utf-8," +
            Uri.EscapeDataString(
                PreviewHtmlDocument));

    public string PreviewHtmlDocument => CreatePreview().HtmlDocument;

    public string PreviewSecurityMessage => string.Join(" ", CreatePreview().Diagnostics);

    public bool HasPreviewSecurityMessage => CreatePreview().Diagnostics.Count > 0;

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
            var document = BiographyDocumentParser.Parse(_markdownFileStore.Read(_filePath));
            _metadata = document.Metadata;
            MarkdownText = document.Body;
            IsDirty = false;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    public BiographyDocument CreateDocument()
    {
        if (_metadata is null)
        {
            return new BiographyDocument(null, MarkdownText ?? string.Empty, false);
        }

        var serialized = BiographyDocumentSerializer.Serialize(_metadata, MarkdownText ?? string.Empty);
        return BiographyDocumentParser.Parse(serialized);
    }

    public void ApplySerializedDocument(string content)
    {
        var document = BiographyDocumentParser.Parse(content);
        _suppressDirtyTracking = true;
        try
        {
            _metadata = document.Metadata;
            MarkdownText = document.Body;
            IsDirty = true;
        }
        finally
        {
            _suppressDirtyTracking = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        var document = CreateDocument();
        var content = document.Metadata is null
            ? document.Body
            : BiographyDocumentSerializer.Serialize(document.Metadata, document.Body);
        _metadata = document.Metadata;
        _markdownFileStore.Write(_filePath, content);
        IsDirty = false;
    }

    partial void OnMarkdownTextChanged(string value)
    {
        _cachedPreviewSource = null;
        _cachedPreview = null;
        OnPropertyChanged(nameof(PreviewHtml));
        OnPropertyChanged(nameof(PreviewWebUri));
        OnPropertyChanged(nameof(PreviewHtmlDocument));
        OnPropertyChanged(nameof(PreviewSecurityMessage));
        OnPropertyChanged(nameof(HasPreviewSecurityMessage));

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

    private SafeMarkdownPreviewResult CreatePreview()
    {
        var source = MarkdownText ?? string.Empty;
        if (_cachedPreview is not null &&
            string.Equals(_cachedPreviewSource, source, StringComparison.Ordinal))
        {
            return _cachedPreview;
        }

        _cachedPreviewSource = source;
        _cachedPreview = _previewService.Render(source, _filePath, _allowedPreviewRoots);
        return _cachedPreview;
    }
}
