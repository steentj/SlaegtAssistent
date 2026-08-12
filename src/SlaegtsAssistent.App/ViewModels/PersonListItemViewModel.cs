using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(
        string recordId,
        string displayName,
        string markdownFilePath,
        string rawGedcom = "",
        BiographySyncStatus syncStatus = BiographySyncStatus.Ukendt)
    {
        RecordId = recordId;
        DisplayName = displayName;
        MarkdownFilePath = markdownFilePath;
        RawGedcom = rawGedcom;
        SyncStatus = syncStatus;
    }

    public string RecordId { get; }

    public string DisplayName { get; }

    public string MarkdownFilePath { get; }

    public string RawGedcom { get; }

    public BiographySyncStatus SyncStatus { get; }

    public string SyncStatusText => SyncStatus switch
    {
        BiographySyncStatus.Ny => "Ny",
        BiographySyncStatus.Uændret => "Uændret",
        BiographySyncStatus.Ændret => "Ændret",
        BiographySyncStatus.Tvetydig => "Tvetydig",
        _ => string.Empty,
    };
}
