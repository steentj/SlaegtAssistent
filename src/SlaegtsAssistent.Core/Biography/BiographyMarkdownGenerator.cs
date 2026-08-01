using System.Text;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyMarkdownGenerator : IBiographyMarkdownGenerator
{
    private const string BiographyPlaceholder = "_Skriv den fulde livshistorie her._";

    public string Generate(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        var facts = BuildFacts(person);
        var headingName = string.IsNullOrWhiteSpace(person.FullName)
            ? person.RecordId
            : person.FullName;

        var body = new StringBuilder()
            .Append("# ")
            .Append(headingName)
            .Append('\n')
            .Append('\n')
            .Append("## Fakta")
            .Append('\n');

        foreach (var fact in facts)
        {
            body.Append("- ").Append(fact).Append('\n');
        }

        body
            .Append('\n')
            .Append("## Biografi")
            .Append('\n')
            .Append(BiographyPlaceholder)
            .Append('\n');

        return BiographyDocumentSerializer.Serialize(
            new BiographyDocumentMetadata(
                1,
                person.RecordId,
                person.FullName,
                BiographyFactsSnapshot.FromPerson(person)),
            body.ToString());
    }

    private static IEnumerable<string> BuildFacts(Person person)
    {
        var birthFact = BuildDateAndPlaceFact("**Født:**", person.BirthDate, person.BirthPlace);
        if (birthFact is not null)
        {
            yield return birthFact;
        }

        var deathFact = BuildDateAndPlaceFact("**Død:**", person.DeathDate, person.DeathPlace);
        if (deathFact is not null)
        {
            yield return deathFact;
        }

        var parentNames = person.Parents
            .Select(parent => parent.FullName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        if (parentNames.Length > 0)
        {
            yield return $"**Forældre:** {string.Join(", ", parentNames)}";
        }
    }

    private static string? BuildDateAndPlaceFact(string label, string? date, string? place)
    {
        var hasDate = !string.IsNullOrWhiteSpace(date);
        var hasPlace = !string.IsNullOrWhiteSpace(place);
        if (!hasDate && !hasPlace)
        {
            return null;
        }

        if (hasDate && hasPlace)
        {
            return $"{label} {date} i {place}";
        }

        if (hasDate)
        {
            return $"{label} {date}";
        }

        return $"{label} i {place}";
    }
}
