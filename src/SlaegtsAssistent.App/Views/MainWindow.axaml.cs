using Avalonia;
using Avalonia.Controls;
using System;
using System.ComponentModel;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class MainWindow : Window
{
    private bool _canCloseWithoutPrompt;
    private EditorViewModel? _subscribedEditor;
    private bool _previewAdapterReady;

    public MainWindow()
    {
        InitializeComponent();
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