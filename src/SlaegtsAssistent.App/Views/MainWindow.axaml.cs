using Avalonia.Controls;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Views;

public partial class MainWindow : Window
{
    private bool _canCloseWithoutPrompt;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        Closing += HandleClosing;
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