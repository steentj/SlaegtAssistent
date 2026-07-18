using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private PersonListItemViewModel? selectedPerson;

    public ObservableCollection<PersonListItemViewModel> People { get; } = [];
}
