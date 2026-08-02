using Avalonia.Controls;
using System;
using System.ComponentModel;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class MarkdownCheatSheetWindow : Window
{
    private MarkdownCheatSheetViewModel? _viewModel;
    private bool _previewAdapterReady;

    public MarkdownCheatSheetWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
        PreviewWebView.AdapterCreated += (_, _) =>
        {
            _previewAdapterReady = true;
            RefreshPreview();
        };
        PreviewWebView.AdapterDestroyed += (_, _) => _previewAdapterReady = false;
    }

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        }

        _viewModel = DataContext as MarkdownCheatSheetViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        }

        RefreshPreview();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MarkdownCheatSheetViewModel.PreviewHtml)
            or nameof(MarkdownCheatSheetViewModel.PreviewHtmlDocument))
        {
            RefreshPreview();
        }
    }

    private void RefreshPreview()
    {
        if (_previewAdapterReady && _viewModel is not null)
        {
            PreviewWebView.NavigateToString(_viewModel.PreviewHtmlDocument);
        }
    }
}
