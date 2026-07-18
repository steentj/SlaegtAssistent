using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Gedcom;

public interface IGedcomLoader
{
    FamilyTree Load(string filePath, FamilyTree? existingTree = null);
}
