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
            throw new ArgumentException("Outputmappen er påkrævet.", nameof(outputDirectory));
        }

        Directory.CreateDirectory(outputDirectory);
        var existingRecordIds = FindExistingRecordIds(outputDirectory);

        foreach (var person in familyTree.People.OrderBy(person => person.RecordId, StringComparer.Ordinal))
        {
            if (existingRecordIds.Contains(person.RecordId))
            {
                continue;
            }

            var fileName = BiographyFileNameGenerator.Generate(person);
            var path = Path.Combine(outputDirectory, fileName);
            if (File.Exists(path))
            {
                continue;
            }

            var markdown = _markdownGenerator.Generate(person);
            File.WriteAllText(path, markdown, Utf8WithoutBom);
            existingRecordIds.Add(person.RecordId);
        }
    }

    private static HashSet<string> FindExistingRecordIds(string outputDirectory)
    {
        var recordIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(outputDirectory, "*.md", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var document = BiographyDocumentParser.Parse(File.ReadAllText(path));
                if (!string.IsNullOrWhiteSpace(document.Metadata?.RecordId))
                {
                    recordIds.Add(document.Metadata.RecordId);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            catch (FormatException)
            {
            }
        }

        return recordIds;
    }
}
