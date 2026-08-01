namespace SlaegtsAssistent.Core.Biography;

public static class BiographyDocumentUpdater
{
    public static string ApplyGedcomChoices(
        BiographyDocument document,
        BiographyFactsSnapshot gedcomFacts,
        IReadOnlyDictionary<string, bool> useGedcomByField)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(gedcomFacts);
        ArgumentNullException.ThrowIfNull(useGedcomByField);

        if (document.Metadata is not { } metadata)
        {
            throw new InvalidOperationException("Dokumentet mangler metadata og kan ikke synkroniseres sikkert.");
        }

        var current = metadata.Facts;
        var updatedFacts = current with
        {
            FullName = UseGedcom("Navn") ? gedcomFacts.FullName : current.FullName,
            Sex = UseGedcom("Køn") ? gedcomFacts.Sex : current.Sex,
            BirthDate = UseGedcom("Fødselsdato") ? gedcomFacts.BirthDate : current.BirthDate,
            BirthPlace = UseGedcom("Fødested") ? gedcomFacts.BirthPlace : current.BirthPlace,
            DeathDate = UseGedcom("Dødsdato") ? gedcomFacts.DeathDate : current.DeathDate,
            DeathPlace = UseGedcom("Dødssted") ? gedcomFacts.DeathPlace : current.DeathPlace,
            ParentRecordIds = UseGedcom("Forældre")
                ? gedcomFacts.ParentRecordIds
                : current.ParentRecordIds,
        };

        var updatedMetadata = metadata with
        {
            DisplayName = UseGedcom("Navn") ? gedcomFacts.FullName : metadata.DisplayName,
            Facts = updatedFacts,
        };

        return BiographyDocumentSerializer.Serialize(updatedMetadata, document.Body);

        bool UseGedcom(string fieldName)
        {
            return useGedcomByField.TryGetValue(fieldName, out var useGedcom) && useGedcom;
        }
    }
}
