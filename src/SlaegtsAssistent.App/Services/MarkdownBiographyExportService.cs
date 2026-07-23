using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class MarkdownBiographyExportService : IMarkdownBiographyExportService
{
    private readonly BiographyFileWriter _fileWriter = new(new BiographyMarkdownGenerator());

    public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
    {
        _fileWriter.WriteAll(familyTree, outputDirectory);
    }
}
