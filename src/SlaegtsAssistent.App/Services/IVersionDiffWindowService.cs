using System.Collections.Generic;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.App.Services;

public interface IVersionDiffWindowService
{
    void Show(string title, IReadOnlyList<DiffFile> files, string? notice);
}
