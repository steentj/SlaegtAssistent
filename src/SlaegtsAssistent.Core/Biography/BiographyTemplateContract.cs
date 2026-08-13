namespace SlaegtsAssistent.Core.Biography;

public static class BiographyTemplateContract
{
    public const int CurrentVersion = 1;

    public static IReadOnlyList<string> RootScalarPaths { get; } =
    [
        "person.recordId", "person.fullName", "person.sex", "person.birthDate",
        "person.birthPlace", "person.deathDate", "person.deathPlace", "person.parentNames",
        "submitter.recordId", "submitter.name", "submitter.address", "submitter.phone",
        "submitter.email", "submitter.website", "submitter.language",
    ];

    public static IReadOnlyList<string> CollectionExamples { get; } =
    [
        "{{#each person.parents}}{{ recordId }}{{ fullName }}{{/each}}",
        "{{#each events}}{{ tag }}{{ category }}{{ value }}{{ date }}{{ place }}{{ type }}{{ note }}{{#each sources}}{{ title }}{{/each}}{{/each}}",
        "{{#each familyEvents}}{{ tag }}{{ category }}{{ value }}{{ date }}{{ place }}{{ type }}{{ note }}{{/each}}",
        "{{#each allEvents}}{{ tag }}{{ category }}{{ value }}{{ date }}{{ place }}{{ type }}{{ note }}{{/each}}",
        "{{#each census}}{{ date }}{{ place }}{{ note }}{{#each sources}}{{ page }}{{/each}}{{/each}}",
        "{{#each sources}}{{ key }}{{ recordId }}{{ title }}{{ author }}{{ publication }}{{ text }}{{ repository }}{{ page }}{{ data }}{{ date }}{{ note }}{{/each}}",
        "{{#each media}}{{ recordId }}{{ file }}{{ relativeFile }}{{ form }}{{ title }}{{ type }}{{ note }}{{/each}}",
    ];

    public static IReadOnlyList<string> PublicFieldPaths { get; } = RootScalarPaths
        .Concat(["person.parents", "person.parents[].recordId", "person.parents[].fullName"])
        .Concat(EventCollectionPaths("events"))
        .Concat(EventCollectionPaths("familyEvents"))
        .Concat(EventCollectionPaths("allEvents"))
        .Concat(
        [
            "census", "census[].date", "census[].place", "census[].note", "census[].sources",
            "census[].sources[].key", "census[].sources[].recordId", "census[].sources[].title",
            "census[].sources[].author", "census[].sources[].publication", "census[].sources[].text",
            "census[].sources[].repository", "census[].sources[].page", "census[].sources[].data",
            "census[].sources[].date", "census[].sources[].note",
            "sources", "sources[].key", "sources[].recordId", "sources[].title",
            "sources[].author", "sources[].publication", "sources[].text",
            "sources[].repository", "sources[].page", "sources[].data", "sources[].date", "sources[].note",
            "media", "media[].recordId", "media[].file", "media[].relativeFile", "media[].form",
            "media[].title", "media[].type", "media[].note",
        ])
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

    private static IEnumerable<string> EventCollectionPaths(string root)
    {
        yield return root;
        foreach (var field in new[] { "tag", "category", "value", "date", "place", "type", "note", "sources" })
        {
            yield return $"{root}[].{field}";
        }

        foreach (var field in new[] { "key", "recordId", "title", "author", "publication", "text", "repository", "page", "data", "date", "note" })
        {
            yield return $"{root}[].sources[].{field}";
        }
    }

    internal static void Validate(BiographyTemplate template, string? filePath)
    {
        ValidateNodes(template.Nodes, Context.Root, template.Source, filePath);
    }

    private static void ValidateNodes(
        IReadOnlyList<BiographyTemplateNode> nodes,
        Context context,
        string source,
        string? filePath)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case VariableTemplateNode variable:
                    var variableField = Resolve(variable.Path, context, variable.Line, variable.Column, filePath);
                    if (variableField.Kind is FieldKind.Object or FieldKind.Collection)
                    {
                        throw Error($"Skabelonfeltet `{variable.Path}` kan ikke vises direkte.", filePath, variable.Line, variable.Column);
                    }
                    break;
                case IfTemplateNode condition:
                    Resolve(condition.Path, context, condition.Line, condition.Column, filePath);
                    ValidateNodes(condition.Children, context, source, filePath);
                    break;
                case EachTemplateNode each:
                    var collection = Resolve(each.Path, context, each.Line, each.Column, filePath);
                    if (collection.Kind != FieldKind.Collection || collection.ItemContext is null)
                    {
                        throw Error($"Skabelonfeltet `{each.Path}` kan ikke bruges som løkke.", filePath, each.Line, each.Column);
                    }
                    ValidateNodes(each.Children, collection.ItemContext.Value, source, filePath);
                    break;
            }
        }
    }

    private static Field Resolve(string path, Context context, int line, int column, string? filePath)
    {
        if (path.StartsWith("this.", StringComparison.Ordinal))
        {
            path = path[5..];
        }

        if (!path.Contains('.', StringComparison.Ordinal) && context != Context.Root)
        {
            if (Fields.TryGetValue(context, out var currentFields) && currentFields.TryGetValue(path, out var current))
            {
                return current;
            }

            throw Error($"Ukendt skabelonfelt `{path}` i {ContextName(context)}.", filePath, line, column);
        }

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || !RootFields.TryGetValue(segments[0], out var field))
        {
            throw Error($"Ukendt skabelonfelt `{path}`.", filePath, line, column);
        }

        for (var index = 1; index < segments.Length; index++)
        {
            if (field.Kind == FieldKind.Collection)
            {
                throw Error(
                    $"Skabelonfeltet `{string.Join('.', segments.Take(index))}` skal bruges som løkke før `{segments[index]}` kan læses.",
                    filePath,
                    line,
                    column);
            }

            if (field.ItemContext is not { } childContext ||
                !Fields[childContext].TryGetValue(segments[index], out field))
            {
                throw Error($"Ukendt skabelonfelt `{path}`.", filePath, line, column);
            }
        }

        return field;
    }

    private static BiographyTemplateException Error(string message, string? filePath, int line, int column) =>
        new(message, filePath, line, column);

    private static string ContextName(Context context) => context switch
    {
        Context.Event => "en hændelse",
        Context.Census => "en folketælling",
        Context.Source => "en kilde",
        Context.Media => "et medie",
        Context.Parent => "en forældrereference",
        _ => "den aktuelle løkkekontekst",
    };

    private static readonly IReadOnlyDictionary<string, Field> RootFields =
        new Dictionary<string, Field>(StringComparer.Ordinal)
        {
            ["person"] = Object(Context.Person),
            ["submitter"] = Object(Context.Submitter),
            ["events"] = Collection(Context.Event),
            ["familyEvents"] = Collection(Context.Event),
            ["allEvents"] = Collection(Context.Event),
            ["census"] = Collection(Context.Census),
            ["sources"] = Collection(Context.Source),
            ["media"] = Collection(Context.Media),
        };

    private static readonly IReadOnlyDictionary<Context, IReadOnlyDictionary<string, Field>> Fields =
        new Dictionary<Context, IReadOnlyDictionary<string, Field>>
        {
            [Context.Person] = ScalarFields("recordId", "fullName", "sex", "birthDate", "birthPlace", "deathDate", "deathPlace", "parentNames", ("parents", Collection(Context.Parent))),
            [Context.Submitter] = ScalarFields("recordId", "name", "address", "phone", "email", "website", "language"),
            [Context.Parent] = ScalarFields("recordId", "fullName"),
            [Context.Event] = ScalarFields("tag", "category", "value", "date", "place", "type", "note", ("sources", Collection(Context.Source))),
            [Context.Census] = ScalarFields("date", "place", "note", ("sources", Collection(Context.Source))),
            [Context.Source] = ScalarFields("key", "recordId", "title", "author", "publication", "text", "repository", "page", "data", "date", "note"),
            [Context.Media] = ScalarFields("recordId", "file", "relativeFile", "form", "title", "type", "note"),
        };

    private static IReadOnlyDictionary<string, Field> ScalarFields(params object[] names)
    {
        var result = new Dictionary<string, Field>(StringComparer.Ordinal);
        foreach (var item in names)
        {
            if (item is string name)
            {
                result[name] = Scalar();
            }
            else if (item is ValueTuple<string, Field> pair)
            {
                result[pair.Item1] = pair.Item2;
            }
        }
        return result;
    }

    private static Field Scalar() => new(FieldKind.Scalar, null);
    private static Field Object(Context context) => new(FieldKind.Object, context);
    private static Field Collection(Context context) => new(FieldKind.Collection, context);

    private enum Context { Root, Person, Submitter, Parent, Event, Census, Source, Media }
    private enum FieldKind { Scalar, Object, Collection }
    private readonly record struct Field(FieldKind Kind, Context? ItemContext);
}
