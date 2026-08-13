namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplate
{
    internal BiographyTemplate(string source, IReadOnlyList<BiographyTemplateNode> nodes, int contractVersion)
    {
        Source = source;
        Nodes = nodes;
        ContractVersion = contractVersion;
    }

    public string Source { get; }

    public int ContractVersion { get; }

    internal IReadOnlyList<BiographyTemplateNode> Nodes { get; }
}

internal abstract record BiographyTemplateNode;

internal sealed record TextTemplateNode(string Text) : BiographyTemplateNode;

internal sealed record VariableTemplateNode(string Path, int Line, int Column) : BiographyTemplateNode;

internal sealed record IfTemplateNode(
    string Path,
    IReadOnlyList<BiographyTemplateNode> Children,
    int Line,
    int Column) : BiographyTemplateNode;

internal sealed record EachTemplateNode(
    string Path,
    IReadOnlyList<BiographyTemplateNode> Children,
    int Line,
    int Column) : BiographyTemplateNode;
