using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class PersonListItemViewModel : ViewModelBase
{
    public PersonListItemViewModel(
        string recordId,
        string displayName,
        string markdownFilePath,
        string rawGedcom = "",
        BiographySyncStatus syncStatus = BiographySyncStatus.Ukendt,
        string? documentErrorCategory = null,
        string? documentErrorMessage = null,
        string? documentNextAction = null,
        bool requiresMigration = false)
    {
        RecordId = recordId;
        DisplayName = displayName;
        MarkdownFilePath = markdownFilePath;
        RawGedcom = rawGedcom;
        SyncStatus = syncStatus;
        DocumentErrorCategory = documentErrorCategory;
        DocumentErrorMessage = documentErrorMessage;
        DocumentNextAction = documentNextAction;
        RequiresMigration = requiresMigration;
    }

    public string RecordId { get; }

    public string DisplayName { get; }

    public string MarkdownFilePath { get; }

    public string RawGedcom { get; }

    public BiographySyncStatus SyncStatus { get; }

    public string? DocumentErrorCategory { get; }

    public string? DocumentErrorMessage { get; }

    public string? DocumentNextAction { get; }

    public bool RequiresMigration { get; }

    public bool HasDocumentDiagnostic => !string.IsNullOrWhiteSpace(DocumentErrorCategory);

    public string DocumentDiagnosticText => HasDocumentDiagnostic
        ? $"{DocumentErrorCategory}: {DocumentErrorMessage}\nFil: {MarkdownFilePath}\nNæste handling: {DocumentNextAction}"
        : string.Empty;

    public string MigrationNoticeText => RequiresMigration
        ? $"Migrering tilgængelig\nFil: {MarkdownFilePath}\nNæste handling: {DocumentNextAction}"
        : string.Empty;

    public bool HasStatusNotice => HasDocumentDiagnostic || RequiresMigration;

    public string StatusNoticeText => HasDocumentDiagnostic
        ? DocumentDiagnosticText
        : MigrationNoticeText;

    public string ToolTipText => HasStatusNotice ? StatusNoticeText : RawGedcom;

    public string SyncStatusText => SyncStatus switch
    {
        BiographySyncStatus.Ny => "Ny",
        BiographySyncStatus.Uændret => "Uændret",
        BiographySyncStatus.Ændret => "Ændret",
        BiographySyncStatus.Tvetydig => "Tvetydig",
        _ => string.Empty,
    };
}
