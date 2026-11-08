using System.Text.Json.Serialization;

namespace SlaegtsAssistent.Core.Biography;

public sealed record BiographyTemplateEditorModel
{
    public const int CurrentVersion = 1;

    [JsonConstructor]
    public BiographyTemplateEditorModel(
        int version,
        IReadOnlyList<BiographyTemplateBlock> blocks)
    {
        Version = version;
        Blocks = Array.AsReadOnly(
            blocks?.ToArray()
            ?? throw new ArgumentNullException(nameof(blocks)));
    }

    public int Version { get; }

    public IReadOnlyList<BiographyTemplateBlock> Blocks { get; }

    public static BiographyTemplateEditorModel CreateDefault() => new(
        CurrentVersion,
        [
            new(
                BiographyTemplateBlockType.Title,
                true,
                string.Empty,
                BiographyTemplatePresentation.Standard,
                [BiographyTemplateField.PersonFullName]),
            new(
                BiographyTemplateBlockType.Facts,
                true,
                "Fakta",
                BiographyTemplatePresentation.List,
                [
                    BiographyTemplateField.Birth,
                    BiographyTemplateField.Death,
                    BiographyTemplateField.Parents,
                    BiographyTemplateField.Children,
                    BiographyTemplateField.Partners,
                    BiographyTemplateField.CreatedDate,
                ]),
            new(
                BiographyTemplateBlockType.Events,
                true,
                "Hændelser",
                BiographyTemplatePresentation.List,
                [
                    BiographyTemplateField.EventCategory,
                    BiographyTemplateField.EventType,
                    BiographyTemplateField.EventValue,
                    BiographyTemplateField.EventDate,
                    BiographyTemplateField.EventPlace,
                ]),
            new(
                BiographyTemplateBlockType.Census,
                true,
                "Folketællinger",
                BiographyTemplatePresentation.List,
                [
                    BiographyTemplateField.CensusDate,
                    BiographyTemplateField.CensusPlace,
                    BiographyTemplateField.CensusNote,
                ]),
            new(
                BiographyTemplateBlockType.Sources,
                true,
                "Kilder",
                BiographyTemplatePresentation.List,
                [
                    BiographyTemplateField.SourceTitle,
                    BiographyTemplateField.SourceAuthor,
                    BiographyTemplateField.SourcePage,
                ]),
            new(
                BiographyTemplateBlockType.Media,
                true,
                "Medier",
                BiographyTemplatePresentation.Standard,
                [BiographyTemplateField.MediaTitle, BiographyTemplateField.MediaRelativeFile]),
            new(
                BiographyTemplateBlockType.Submitter,
                true,
                string.Empty,
                BiographyTemplatePresentation.Standard,
                [BiographyTemplateField.SubmitterName]),
            new(
                BiographyTemplateBlockType.Biography,
                true,
                "Biografi",
                BiographyTemplatePresentation.Standard,
                []),
        ]);
}

public sealed record BiographyTemplateBlock
{
    [JsonConstructor]
    public BiographyTemplateBlock(
        BiographyTemplateBlockType type,
        bool isVisible,
        string heading,
        BiographyTemplatePresentation presentation,
        IReadOnlyList<BiographyTemplateField> fields)
    {
        Type = type;
        IsVisible = isVisible;
        Heading = heading ?? throw new ArgumentNullException(nameof(heading));
        Presentation = presentation;
        Fields = Array.AsReadOnly(
            fields?.ToArray()
            ?? throw new ArgumentNullException(nameof(fields)));
    }

    public BiographyTemplateBlockType Type { get; init; }

    public bool IsVisible { get; init; }

    public string Heading { get; init; }

    public BiographyTemplatePresentation Presentation { get; init; }

    public IReadOnlyList<BiographyTemplateField> Fields { get; }
}

[JsonConverter(typeof(JsonStringEnumConverter<BiographyTemplateBlockType>))]
public enum BiographyTemplateBlockType
{
    Title,
    Facts,
    Events,
    Census,
    Sources,
    Media,
    Submitter,
    Biography,
}

[JsonConverter(typeof(JsonStringEnumConverter<BiographyTemplatePresentation>))]
public enum BiographyTemplatePresentation
{
    Standard,
    List,
    Table,
}

[JsonConverter(typeof(JsonStringEnumConverter<BiographyTemplateField>))]
public enum BiographyTemplateField
{
    PersonFullName,
    Birth,
    Death,
    Parents,
    Children,
    Partners,
    CreatedDate,
    EventCategory,
    EventType,
    EventValue,
    EventDate,
    EventPlace,
    CensusDate,
    CensusPlace,
    CensusNote,
    SourceTitle,
    SourceAuthor,
    SourcePage,
    MediaTitle,
    MediaRelativeFile,
    SubmitterName,
}
