namespace SlaegtsAssistent.Core.Biography;

public enum BiographyDocumentErrorCategory
{
    MalformedFrontMatter,
    DuplicateKey,
    InvalidValue,
    MissingRequiredField,
    UnsupportedFormatVersion,
}
