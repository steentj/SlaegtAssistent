using SlaegtsAssistent.Core.Domain;
using System.Threading;

namespace SlaegtsAssistent.Core.Gedcom;

public interface IGedcomLoader
{
    FamilyTree Load(string filePath, FamilyTree? existingTree = null);

    FamilyTree Load(
        string filePath,
        FamilyTree? existingTree,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = Load(filePath, existingTree);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
