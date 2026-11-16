using System.Collections.Generic;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Services;

public sealed class NullVersionHistoryWindowService : IVersionHistoryWindowService
{
    public void Show(IVersioningHost host)
    {
    }
}

public sealed class NullVersionDiffWindowService : IVersionDiffWindowService
{
    public void Show(string title, IReadOnlyList<DiffFile> files, string? notice)
    {
    }
}
