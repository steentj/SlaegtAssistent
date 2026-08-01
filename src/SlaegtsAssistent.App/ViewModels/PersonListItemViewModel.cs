namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(
        string recordId,
        string displayName,
        string markdownFilePath,
        string rawGedcom = "")
    {
        RecordId = recordId;
        DisplayName = displayName;
        MarkdownFilePath = markdownFilePath;
        RawGedcom = rawGedcom;
    }

    public string RecordId { get; }

    public string DisplayName { get; }

    public string MarkdownFilePath { get; }

    public string RawGedcom { get; }
}
