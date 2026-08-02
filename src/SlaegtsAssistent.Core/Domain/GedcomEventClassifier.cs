namespace SlaegtsAssistent.Core.Domain;

public static class GedcomEventClassifier
{
    public static GedcomEventCategory Classify(string tag, string? type = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return tag.ToUpperInvariant() switch
        {
            "BIRT" => GedcomEventCategory.Birth,
            "BAPM" or "CHR" => GedcomEventCategory.Baptism,
            "CONF" => GedcomEventCategory.Confirmation,
            "MARR" => GedcomEventCategory.Marriage,
            "DEAT" => GedcomEventCategory.Death,
            "BURI" => GedcomEventCategory.Burial,
            "CENS" => GedcomEventCategory.Census,
            "EVEN" when IsMilitaryService(type) => GedcomEventCategory.MilitaryService,
            _ => GedcomEventCategory.Other,
        };
    }

    private static bool IsMilitaryService(string? type)
    {
        return !string.IsNullOrWhiteSpace(type)
            && (type.Contains("lægdsrulle", StringComparison.OrdinalIgnoreCase)
                || type.Contains("laegdsrulle", StringComparison.OrdinalIgnoreCase));
    }
}
