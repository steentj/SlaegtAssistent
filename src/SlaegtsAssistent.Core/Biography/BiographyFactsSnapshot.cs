using System.Security.Cryptography;
using System.Text;

namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyFactsSnapshot(
    string? FullName,
    string? Sex,
    string? BirthDate,
    string? BirthPlace,
    string? DeathDate,
    string? DeathPlace,
    IReadOnlyList<string> ParentRecordIds)
{
    public string? ParentDisplayText { get; init; }

    public IReadOnlySet<string>? RepresentedFields { get; init; }

    public string ComputeFingerprint()
    {
        var canonical = string.Join(
            "\n",
            Encode(FullName),
            Encode(Sex),
            Encode(BirthDate),
            Encode(BirthPlace),
            Encode(DeathDate),
            Encode(DeathPlace),
            string.Join(
                "\n",
                ParentRecordIds
                    .OrderBy(recordId => recordId, StringComparer.Ordinal)
                    .Select(Encode)));

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static BiographyFactsSnapshot FromPerson(Domain.Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        return new BiographyFactsSnapshot(
            person.FullName,
            person.Sex,
            person.BirthDate,
            person.BirthPlace,
            person.DeathDate,
            person.DeathPlace,
            person.Parents.Select(parent => parent.RecordId).ToArray())
        {
            ParentDisplayText = string.Join(
                ", ",
                person.Parents
                    .Select(parent => parent.FullName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))),
        };
    }

    private static string Encode(string? value)
    {
        return value is null ? "<null>" : $"{value.Length}:{value}";
    }
}
