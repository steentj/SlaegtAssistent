namespace SlaegtsAssistent.Core.Domain;

public sealed class Source
{
    public Source(string? recordId = null)
    {
        RecordId = recordId;
    }

    public string? RecordId { get; }

    public string? Title { get; set; }

    public string? Author { get; set; }

    public string? Publication { get; set; }

    public string? Text { get; set; }

    public string? Repository { get; set; }

    public string? Page { get; set; }

    public string? Data { get; set; }

    public string? Date { get; set; }

    public string? Note { get; set; }
}
