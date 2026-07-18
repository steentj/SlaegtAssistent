namespace SlaegtsAssistent.Core.Domain;

public sealed class FamilyTree
{
    private readonly Dictionary<string, Person> _peopleByRecordId = new(StringComparer.Ordinal);

    public IReadOnlyCollection<Person> People => _peopleByRecordId.Values;

    public Person? FindPerson(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        return _peopleByRecordId.GetValueOrDefault(recordId);
    }

    internal Person GetOrAddPerson(string recordId)
    {
        if (!_peopleByRecordId.TryGetValue(recordId, out var person))
        {
            person = new Person(recordId);
            _peopleByRecordId.Add(recordId, person);
        }

        return person;
    }

    internal bool TryGetPerson(string recordId, out Person person)
    {
        return _peopleByRecordId.TryGetValue(recordId, out person!);
    }
}
