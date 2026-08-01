namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyDifference(
    string FieldName,
    string? DocumentValue,
    string? GedcomValue);
