namespace SlaegtsAssistent.Core.Domain;

public sealed class Census
{
    public string? Date { get; set; }

    public string? Place { get; set; }

    public string? Note { get; set; }

    public IList<Source> Sources { get; } = new List<Source>();
}
