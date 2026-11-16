using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Biography;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SlaegtsAssistent.App.Services;

public interface IMarkdownBiographyExportService
{
    void WriteBiographies(FamilyTree familyTree, string outputDirectory);

    IReadOnlyDictionary<string, string> GenerateBiographies(FamilyTree familyTree, string outputDirectory, CancellationToken cancellationToken = default) =>
        familyTree.People.ToDictionary(person => person.RecordId,
            person =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return GenerateBiography(familyTree, person, outputDirectory);
            });

    IReadOnlyDictionary<string, string> GenerateBiographies(FamilyTree familyTree, string outputDirectory,
        MediaFileIndex? mediaIndex, CancellationToken cancellationToken) =>
        GenerateBiographies(familyTree, outputDirectory, cancellationToken);

    string GenerateBiography(FamilyTree familyTree, Person person, string outputDirectory);
}
