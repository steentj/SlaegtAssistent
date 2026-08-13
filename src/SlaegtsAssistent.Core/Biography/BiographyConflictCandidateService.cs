namespace SlaegtsAssistent.Core.Biography;

public static class BiographyConflictCandidateService
{
    public static string MergeWithExistingDocument(
        BiographyDocument existingDocument,
        string renderedSelection)
    {
        ArgumentNullException.ThrowIfNull(existingDocument);
        ArgumentNullException.ThrowIfNull(renderedSelection);

        var renderedDocument = BiographyDocumentParser.Parse(renderedSelection);
        if (renderedDocument.Metadata is null)
        {
            throw new InvalidOperationException("Previewkandidaten mangler dokumentmetadata.");
        }

        var candidate = BiographyGeneratedSectionMerger.CreateCandidate(
            existingDocument.Body,
            renderedDocument.Body);
        return BiographyDocumentSerializer.Serialize(renderedDocument.Metadata, candidate.Content);
    }
}
