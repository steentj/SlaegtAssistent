namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyDifferenceService
{
    public IReadOnlyList<BiographyDifference> Compare(
        BiographyFactsSnapshot documentFacts,
        BiographyFactsSnapshot gedcomFacts)
    {
        ArgumentNullException.ThrowIfNull(documentFacts);
        ArgumentNullException.ThrowIfNull(gedcomFacts);

        var differences = new List<BiographyDifference>();
        AddIfDifferent(differences, "Navn", documentFacts.FullName, gedcomFacts.FullName);
        AddIfDifferent(differences, "Køn", documentFacts.Sex, gedcomFacts.Sex);
        AddIfDifferent(differences, "Fødselsdato", documentFacts.BirthDate, gedcomFacts.BirthDate);
        AddIfDifferent(differences, "Fødested", documentFacts.BirthPlace, gedcomFacts.BirthPlace);
        AddIfDifferent(differences, "Dødsdato", documentFacts.DeathDate, gedcomFacts.DeathDate);
        AddIfDifferent(differences, "Dødssted", documentFacts.DeathPlace, gedcomFacts.DeathPlace);

        var documentParents = string.Join(", ", documentFacts.ParentRecordIds);
        var gedcomParents = string.Join(", ", gedcomFacts.ParentRecordIds);
        AddIfDifferent(differences, "Forældre", documentParents, gedcomParents);
        return differences;
    }

    private static void AddIfDifferent(
        ICollection<BiographyDifference> differences,
        string fieldName,
        string? documentValue,
        string? gedcomValue)
    {
        if (!string.Equals(documentValue, gedcomValue, StringComparison.Ordinal))
        {
            differences.Add(new BiographyDifference(fieldName, documentValue, gedcomValue));
        }
    }
}
