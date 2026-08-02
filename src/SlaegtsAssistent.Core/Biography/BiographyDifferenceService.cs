namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyDifferenceService
{
    public IReadOnlyList<BiographyDifference> Compare(
        BiographyFactsSnapshot documentFacts,
        BiographyFactsSnapshot gedcomFacts,
        bool includeUnrepresentedFields = false)
    {
        ArgumentNullException.ThrowIfNull(documentFacts);
        ArgumentNullException.ThrowIfNull(gedcomFacts);

        var differences = new List<BiographyDifference>();
        AddIfRepresented(differences, documentFacts, "Navn", documentFacts.FullName, gedcomFacts.FullName, includeUnrepresentedFields);
        AddIfRepresented(differences, documentFacts, "Køn", documentFacts.Sex, gedcomFacts.Sex, includeUnrepresentedFields);
        AddIfRepresented(differences, documentFacts, "Fødselsdato", documentFacts.BirthDate, gedcomFacts.BirthDate, includeUnrepresentedFields);
        AddIfRepresented(differences, documentFacts, "Fødested", documentFacts.BirthPlace, gedcomFacts.BirthPlace, includeUnrepresentedFields);
        AddIfRepresented(differences, documentFacts, "Dødsdato", documentFacts.DeathDate, gedcomFacts.DeathDate, includeUnrepresentedFields);
        AddIfRepresented(differences, documentFacts, "Dødssted", documentFacts.DeathPlace, gedcomFacts.DeathPlace, includeUnrepresentedFields);

        var documentParents = documentFacts.ParentDisplayText
            ?? string.Join(", ", documentFacts.ParentRecordIds);
        var gedcomParents = gedcomFacts.ParentDisplayText
            ?? string.Join(", ", gedcomFacts.ParentRecordIds);
        AddIfRepresented(differences, documentFacts, "Forældre", documentParents, gedcomParents, includeUnrepresentedFields);
        return differences;
    }

    private static void AddIfRepresented(
        ICollection<BiographyDifference> differences,
        BiographyFactsSnapshot documentFacts,
        string fieldName,
        string? documentValue,
        string? gedcomValue,
        bool includeUnrepresentedFields)
    {
        if (!includeUnrepresentedFields &&
            documentFacts.RepresentedFields is not null &&
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
