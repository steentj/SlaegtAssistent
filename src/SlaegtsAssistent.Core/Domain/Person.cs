namespace SlaegtsAssistent.Core.Domain;

public sealed class Person
{
    public Person(string recordId)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new ArgumentException("Record id is required.", nameof(recordId));
        }

        RecordId = recordId;
    }

    public string RecordId { get; }

    public string? FullName { get; set; }

    public string? Sex { get; set; }

    public string? BirthDate { get; set; }

    public string? BirthPlace { get; set; }

    public string? DeathDate { get; set; }

    public string? DeathPlace { get; set; }

    public IList<Person> Parents { get; } = new List<Person>();

    public IList<Person> Children { get; } = new List<Person>();
}
