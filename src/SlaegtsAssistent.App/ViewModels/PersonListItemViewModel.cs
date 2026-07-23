namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(string recordId, string displayName)
    {
        RecordId = recordId;
        DisplayName = displayName;
    }

    public string RecordId { get; }

    public string DisplayName { get; }
}
