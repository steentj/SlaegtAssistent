namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(string displayName)
    {
        DisplayName = displayName;
    }

    public string DisplayName { get; }
}
