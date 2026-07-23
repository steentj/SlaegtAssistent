using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class EditorViewModel : ViewModelBase
{
    private readonly string _filePath;
    private readonly IMarkdownFileStore _markdownFileStore;

    public EditorViewModel(string filePath, IMarkdownFileStore markdownFileStore)
    {
        _filePath = filePath;
        _markdownFileStore = markdownFileStore;
    }

    [ObservableProperty]
    private string markdownText = string.Empty;

    public string PreviewHtml => string.IsNullOrWhiteSpace(MarkdownText)
        ? string.Empty
        : Markdown.ToHtml(MarkdownText);

    public void Load()
    {
        MarkdownText = _markdownFileStore.Read(_filePath);
    }

    [RelayCommand]
    private void Save()
    {
        _markdownFileStore.Write(_filePath, MarkdownText ?? string.Empty);
    }

    partial void OnMarkdownTextChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewHtml));
    }
}
