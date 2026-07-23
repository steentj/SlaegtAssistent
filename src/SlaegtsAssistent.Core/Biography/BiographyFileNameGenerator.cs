using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public static class BiographyFileNameGenerator
{
    public static string Generate(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        var slug = Slugify(person.FullName);
        var shortId = CreateShortId(person.RecordId);
        return $"{slug}-{shortId}.md";
    }

    private static string Slugify(string? fullName)
    {
        var source = string.IsNullOrWhiteSpace(fullName) ? "person" : fullName;
        var normalized = source.Normalize(NormalizationForm.FormD);
        var slugBuilder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                slugBuilder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator)
            {
                slugBuilder.Append('-');
                previousWasSeparator = true;
            }
        }

        var slug = slugBuilder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "person" : slug;
    }

    private static string CreateShortId(string recordId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(recordId));
        return Convert.ToHexString(hash).ToLowerInvariant()[..4];
    }
}
