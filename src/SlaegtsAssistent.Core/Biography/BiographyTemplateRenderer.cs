using System.Collections;
using System.Globalization;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateRenderer
{
    public string Render(BiographyTemplate template, BiographyTemplateContext context)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        return RenderNodes(template.Nodes, context, context);
    }

    private static string RenderNodes(
        IReadOnlyList<BiographyTemplateNode> nodes,
        BiographyTemplateContext root,
        object? current)
    {
        var output = new System.Text.StringBuilder();
        foreach (var node in nodes)
        {
            switch (node)
            {
                case TextTemplateNode text:
                    output.Append(text.Text);
                    break;
                case VariableTemplateNode variable:
                    output.Append(EscapeMarkdown(Resolve(root, current, variable.Path)));
                    break;
                case IfTemplateNode @if:
                    if (IsTruthy(Resolve(root, current, @if.Path)))
                    {
                        output.Append(RenderNodes(@if.Children, root, current));
                    }

                    break;
                case EachTemplateNode each:
                    if (Resolve(root, current, each.Path) is IEnumerable values)
                    {
                        foreach (var value in values)
                        {
                            output.Append(RenderNodes(each.Children, root, value));
                        }
                    }

                    break;
            }
        }

        return output.ToString();
    }

    private static object? Resolve(BiographyTemplateContext root, object? current, string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object? value = segments[0] switch
        {
            "person" => root.Person,
            "submitter" => root.Submitter,
            "events" => root.Events,
            "familyEvents" => root.FamilyEvents,
            "allEvents" => root.AllEvents,
            "census" => root.Census,
            "sources" => root.Sources,
            "media" => root.Media,
            "this" => current,
            _ => current is null ? null : GetMember(current, segments[0]),
        };

        foreach (var segment in segments.Skip(1))
        {
            value = value is null ? null : GetMember(value, segment);
        }

        return value;
    }

    private static object? GetMember(object value, string name)
    {
        return value switch
        {
            PersonTemplateContext person => name switch
            {
                "recordId" => person.RecordId,
                "fullName" => person.FullName,
                "sex" => person.Sex,
                "birthDate" => person.BirthDate,
                "birthPlace" => person.BirthPlace,
                "deathDate" => person.DeathDate,
                "deathPlace" => person.DeathPlace,
                "parents" => person.Parents,
                _ => null,
            },
            PersonReferenceTemplateContext person => name switch
            {
                "recordId" => person.RecordId,
                "fullName" => person.FullName,
                _ => null,
            },
            EventTemplateContext @event => name switch
            {
                "tag" => @event.Tag,
                "category" => @event.Category.ToString(),
                "value" => @event.Value,
                "date" => @event.Date,
                "place" => @event.Place,
                "type" => @event.Type,
                "note" => @event.Note,
                "sources" => @event.Sources,
                _ => null,
            },
            CensusTemplateContext census => name switch
            {
                "date" => census.Date,
                "place" => census.Place,
                "note" => census.Note,
                "sources" => census.Sources,
                _ => null,
            },
            SourceTemplateContext source => name switch
            {
                "key" => source.Key,
                "recordId" => source.RecordId,
                "title" => source.Title,
                "author" => source.Author,
                "publication" => source.Publication,
                "text" => source.Text,
                "repository" => source.Repository,
                "page" => source.Page,
                "data" => source.Data,
                "date" => source.Date,
                "note" => source.Note,
                _ => null,
            },
            MediaTemplateContext media => name switch
            {
                "recordId" => media.RecordId,
                "file" => media.File,
                "relativeFile" => media.RelativeFile,
                "form" => media.Form,
                "title" => media.Title,
                "type" => media.Type,
                "note" => media.Note,
                _ => null,
            },
            SubmitterTemplateContext submitter => name switch
            {
                "recordId" => submitter.RecordId,
                "name" => submitter.Name,
                "address" => submitter.Address,
                "phone" => submitter.Phone,
                "email" => submitter.Email,
                "website" => submitter.Website,
                "language" => submitter.Language,
                _ => null,
            },
            _ => null,
        };
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            IEnumerable values => values.Cast<object?>().Any(),
            bool boolean => boolean,
            _ => true,
        };
    }

    private static string EscapeMarkdown(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("<", "\\<", StringComparison.Ordinal)
            .Replace(">", "\\>", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal);
    }
}
