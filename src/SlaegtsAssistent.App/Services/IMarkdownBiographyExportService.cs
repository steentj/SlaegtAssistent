using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface IMarkdownBiographyExportService
{
    void WriteBiographies(FamilyTree familyTree, string outputDirectory);
}
