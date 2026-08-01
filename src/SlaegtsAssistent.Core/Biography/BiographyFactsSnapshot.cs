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
            person.Parents.Select(parent => parent.RecordId).ToArray());
    }
}
