using System;
using System.Collections.Generic;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel
{
    public IReadOnlyList<MarkdownDocumentInfo> GetBiographyExportDocuments() => _markdownDocumentCatalog.Load(StandardMarkdownOutputFolder);

    public BiographySnapshotResult CreateBiographyExportSnapshot(IEnumerable<string> ids) =>
        new BiographyDocumentSnapshotService(_markdownFileStore).Create(ids, GetBiographyExportDocuments(), _editors);
}
