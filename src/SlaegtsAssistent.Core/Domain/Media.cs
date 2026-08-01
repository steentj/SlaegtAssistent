namespace SlaegtsAssistent.Core.Domain;

public sealed class Media
{
    public Media(string? recordId = null)
    {
        RecordId = recordId;
    }

    public string? RecordId { get; }

    public string? File { get; set; }

    public string? Form { get; set; }

    public string? Title { get; set; }

    public string? Type { get; set; }

    public string? Note { get; set; }
}
