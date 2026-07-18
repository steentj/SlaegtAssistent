using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_ShouldStartWithEmptyPeopleAndNoSelection()
    {
        var viewModel = new MainWindowViewModel();

        viewModel.People.Should().BeEmpty();
        viewModel.SelectedPerson.Should().BeNull();
    }

    [Fact]
    public void SettingSelectedPerson_ShouldRaisePropertyChanged()
    {
        var viewModel = new MainWindowViewModel();
        var selectedPerson = new PersonListItemViewModel("Anna Jensen");
        var raisedPropertyNames = new List<string?>();

        viewModel.PropertyChanged += (_, args) => raisedPropertyNames.Add(args.PropertyName);

        viewModel.SelectedPerson = selectedPerson;

        raisedPropertyNames.Should().Contain(nameof(MainWindowViewModel.SelectedPerson));
    }
}
