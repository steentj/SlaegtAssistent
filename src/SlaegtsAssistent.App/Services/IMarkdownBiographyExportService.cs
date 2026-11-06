using SlaegtsAssistent.Core.Domain;
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

    string GenerateBiography(FamilyTree familyTree, Person person, string outputDirectory);
}
