namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateDocumentUpdater
{
    public string CreateCandidate(
        BiographyDocument document,
        string templateSource,
        string? mediaBaseDirectory = null,
        string? gedcomSourceDirectory = null,
        BiographyPersonLinks? personLinks = null,
        IReadOnlyDictionary<string, string?>? personNames = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Metadata is not { } metadata ||
            metadata.FormatVersion != BiographyDocumentParser.CurrentFormatVersion ||
            metadata.SyncBaseline is not { } baseline ||
            baseline.Version != BiographySyncBaseline.CurrentVersion ||
            baseline.Approved.Version != CanonicalBiographySnapshot.CurrentVersion ||
            baseline.Imported.Version != CanonicalBiographySnapshot.CurrentVersion)
        {
            throw new InvalidOperationException(
                "Dokumentet mangler et understøttet, godkendt snapshot. Gennemgå dokumentet via GEDCOM-import, før skabelonen anvendes.");
        }

        var facts = metadata.Facts;
        var approved = baseline.Approved with
        {
            Person = baseline.Approved.Person with
            {
                FullName = facts.FullName,
                Sex = facts.Sex,
                BirthDate = facts.BirthDate,
                BirthPlace = facts.BirthPlace,
                DeathDate = facts.DeathDate,
                DeathPlace = facts.DeathPlace,
            },
            ParentRecordIds = facts.ParentRecordIds,
        };
        var rendered = new BiographyTemplateMarkdownGenerator(
            templateSource,
            mediaBaseDirectory: mediaBaseDirectory,
            gedcomSourceDirectory: gedcomSourceDirectory,
            personLinks: personLinks).Generate(approved, baseline.Imported, personNames);
        var renderedDocument = BiographyDocumentParser.Parse(rendered);
        var merged = BiographyGeneratedSectionMerger.CreateCandidate(document.Body, renderedDocument.Body);
        return BiographyDocumentSerializer.Serialize(metadata with
        {
            TemplateHash = BiographyTemplateIdentity.ComputeHash(templateSource),
        }, merged.ChangesExistingDocument ? merged.Content : document.Body);
    }
}
