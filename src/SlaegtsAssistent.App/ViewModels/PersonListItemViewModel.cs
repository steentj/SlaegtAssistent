namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(string recordId, string displayName, string markdownFilePath)
    {
        RecordId = recordId;
        DisplayName = displayName;
        MarkdownFilePath = markdownFilePath;
    }

    public string RecordId { get; }

    public string DisplayName { get; }

    public string MarkdownFilePath { get; }
}
