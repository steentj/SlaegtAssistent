using System.Collections.Generic;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public interface IGedcomDifferenceDialogService
{
    Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
        string personName,
        IReadOnlyList<BiographyDifference> differences);
}
