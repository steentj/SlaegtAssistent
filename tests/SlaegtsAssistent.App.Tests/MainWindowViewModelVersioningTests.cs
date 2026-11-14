using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public sealed class MainWindowViewModelVersioningTests
{
    [Fact]
    public void CommitCommand_BliverDeaktiveretUnderImport()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.IsImporting = false;
        viewModel.CommitCommand.CanExecute(null).Should().BeTrue();
        viewModel.ShowVersionHistoryCommand.CanExecute(null).Should().BeTrue();

        viewModel.IsImporting = true;
        viewModel.CommitCommand.CanExecute(null).Should().BeFalse();
        viewModel.ShowVersionHistoryCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ShowVersionHistoryCommand_FindesPaaStandardKonstruktoer()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.ShowVersionHistoryCommand.Should().NotBeNull();
        viewModel.CommitCommand.Should().NotBeNull();
    }
}
