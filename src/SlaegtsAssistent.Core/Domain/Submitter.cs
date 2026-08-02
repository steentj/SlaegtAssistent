namespace SlaegtsAssistent.Core.Domain;

public sealed class Submitter
{
    public Submitter(string? recordId = null)
    {
        RecordId = recordId;
    }

    public string? RecordId { get; }

    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? Language { get; set; }
}
