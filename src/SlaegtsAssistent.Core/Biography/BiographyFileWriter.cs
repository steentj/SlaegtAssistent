using System.Text;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyFileWriter
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);
    private readonly IBiographyMarkdownGenerator _markdownGenerator;

    public BiographyFileWriter(IBiographyMarkdownGenerator markdownGenerator)
    {
        _markdownGenerator = markdownGenerator ?? throw new ArgumentNullException(nameof(markdownGenerator));
    }

    public void WriteAll(FamilyTree familyTree, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(familyTree);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (var person in familyTree.People.OrderBy(person => person.RecordId, StringComparer.Ordinal))
        {
            var fileName = BiographyFileNameGenerator.Generate(person);
            var path = Path.Combine(outputDirectory, fileName);
            if (File.Exists(path))
            {
                continue;
            }

            var markdown = _markdownGenerator.Generate(person);
            File.WriteAllText(path, markdown, Utf8WithoutBom);
        }
    }
}
