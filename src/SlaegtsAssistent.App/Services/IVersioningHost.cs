using System;

namespace SlaegtsAssistent.App.Services;

public interface IVersioningHost
{
    string? WorkspaceFolder { get; }
    bool IsBusyWithImport { get; }
    bool HasUnsavedEditors { get; }
    bool SaveAllEditors();
    void ReloadWorkspace();
    event EventHandler? DocumentsSaved;
}
