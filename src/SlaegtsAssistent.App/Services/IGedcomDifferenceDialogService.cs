using System.Collections.Generic;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Services;

public sealed record GedcomDifferenceReviewItem(
    string Key,
    string PersonName,
    string FilePath,
    BiographyDocument Document,
    BiographyFactsSnapshot GedcomFacts,
    BiographyDifference Difference);

public interface IGedcomDifferenceDialogService
{
    Task<IReadOnlyDictionary<string, bool>?> ShowAsync(
        IReadOnlyList<GedcomDifferenceReviewItem> differences);
}
