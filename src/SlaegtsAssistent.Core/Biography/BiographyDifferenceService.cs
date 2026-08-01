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
        AddIfRepresented(differences, documentFacts, "Navn", documentFacts.FullName, gedcomFacts.FullName);
        AddIfRepresented(differences, documentFacts, "Køn", documentFacts.Sex, gedcomFacts.Sex);
        AddIfRepresented(differences, documentFacts, "Fødselsdato", documentFacts.BirthDate, gedcomFacts.BirthDate);
        AddIfRepresented(differences, documentFacts, "Fødested", documentFacts.BirthPlace, gedcomFacts.BirthPlace);
        AddIfRepresented(differences, documentFacts, "Dødsdato", documentFacts.DeathDate, gedcomFacts.DeathDate);
        AddIfRepresented(differences, documentFacts, "Dødssted", documentFacts.DeathPlace, gedcomFacts.DeathPlace);

        var documentParents = documentFacts.ParentDisplayText
            ?? string.Join(", ", documentFacts.ParentRecordIds);
        var gedcomParents = gedcomFacts.ParentDisplayText
            ?? string.Join(", ", gedcomFacts.ParentRecordIds);
        AddIfRepresented(differences, documentFacts, "Forældre", documentParents, gedcomParents);
        return differences;
    }

    private static void AddIfRepresented(
        ICollection<BiographyDifference> differences,
        BiographyFactsSnapshot documentFacts,
        string fieldName,
        string? documentValue,
        string? gedcomValue)
    {
        if (documentFacts.RepresentedFields is not null &&
            !documentFacts.RepresentedFields.Contains(fieldName))
        {
            return;
        }

        AddIfDifferent(differences, fieldName, documentValue, gedcomValue);
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
