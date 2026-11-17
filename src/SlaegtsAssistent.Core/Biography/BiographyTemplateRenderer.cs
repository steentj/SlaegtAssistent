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

        var output = new System.Text.StringBuilder();
        RenderNodes(template.Nodes, context, context, output);
        return output.ToString();
    }

    private static void RenderNodes(
        IReadOnlyList<BiographyTemplateNode> nodes,
        BiographyTemplateContext root,
        object? current,
        System.Text.StringBuilder output)
    {
        foreach (var node in nodes)
        {
            switch (node)
            {
                case TextTemplateNode text:
                    output.Append(text.Text);
                    break;
                case VariableTemplateNode variable:
                    output.Append(EscapeMarkdown(Resolve(root, current, variable.Path), IsTableRow(output)));
                    break;
                case IfTemplateNode @if:
                    if (IsTruthy(Resolve(root, current, @if.Path)))
                    {
                        RenderNodes(@if.Children, root, current, output);
                    }

                    break;
                case EachTemplateNode each:
                    if (Resolve(root, current, each.Path) is IEnumerable values)
                    {
                        foreach (var value in values)
                        {
                            RenderNodes(each.Children, root, value, output);
                        }
                    }

                    break;
            }
        }

    }

    private static bool IsTableRow(System.Text.StringBuilder output)
    {
        var index = output.Length - 1;
        while (index >= 0 && output[index] is not '\n' and not '\r')
        {
            index--;
        }

        index++;
        while (index < output.Length && output[index] is ' ' or '\t')
        {
            index++;
        }

        return index < output.Length && output[index] == '|';
    }

    private static object? Resolve(BiographyTemplateContext root, object? current, string path)
    {
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 1 && current is not null && !ReferenceEquals(current, root))
        {
            return GetMember(current, segments[0]);
        }

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
                "parentNames" => RenderReferences(person.Parents),
                "childNames" => RenderReferences(person.Children),
                "partnerNames" => RenderReferences(person.Partners),
                "children" => person.Children,
                "partners" => person.Partners,
                "createdDate" => person.CreatedDate,
                "notes" => person.Notes,
                "familyNotes" => person.FamilyNotes,
                _ => null,
            },
            PersonReferenceTemplateContext person => name switch
            {
                "recordId" => person.RecordId,
                "fullName" => RenderReference(person),
                "plainName" => person.FullName,
                "link" => RenderReference(person),
                "documentPath" => person.DocumentPath,
                _ => null,
            },
            NoteTemplateContext note => name switch
            {
                "text" => new NoteText(note.Text, note.NoteDetails),
                _ => null,
            },
            EventTemplateContext @event => name switch
            {
                "tag" => @event.Tag,
                "category" => DanishCategory(@event.Category),
                "value" => @event.Value,
                "date" => @event.Date,
                "place" => @event.Place,
                "type" => @event.Type,
                "note" => WrapNote(@event.Note, @event.NoteDetails),
                "sources" => @event.Sources,
                _ => null,
            },
            CensusTemplateContext census => name switch
            {
                "date" => census.Date,
                "place" => census.Place,
                "note" => WrapNote(census.Note, census.NoteDetails),
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
                "note" => WrapNote(source.Note, source.NoteDetails),
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
                "note" => WrapNote(media.Note, media.NoteDetails),
                "diagnostic" => media.Diagnostic,
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

    private static string DanishCategory(GedcomEventCategory category) => category switch
    {
        GedcomEventCategory.Birth => "Fødsel",
        GedcomEventCategory.Baptism => "Dåb",
        GedcomEventCategory.Confirmation => "Konfirmation",
        GedcomEventCategory.Marriage => "Vielse",
        GedcomEventCategory.Death => "Død",
        GedcomEventCategory.Burial => "Begravelse",
        GedcomEventCategory.Census => "Folketælling",
        GedcomEventCategory.MilitaryService => "Militærtjeneste",
        GedcomEventCategory.Residence => "Bopæl",
        _ => "Anden hændelse",
    };

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            PersonLinkMarkdown markdown => !string.IsNullOrWhiteSpace(markdown.Text),
            NoteText note => !string.IsNullOrWhiteSpace(note.Text),
            string text => !string.IsNullOrWhiteSpace(text),
            IEnumerable values => values.Cast<object?>().Any(),
            bool boolean => boolean,
            _ => true,
        };
    }

    private static PersonLinkMarkdown RenderReferences(IEnumerable<PersonReferenceTemplateContext> people) =>
        new(string.Join(", ", people.Select(person => RenderReference(person).Text)));

    private static PersonLinkMarkdown RenderReference(PersonReferenceTemplateContext person)
    {
        var name = EscapeMarkdown(string.IsNullOrWhiteSpace(person.FullName) ? person.RecordId : person.FullName);
        return new PersonLinkMarkdown(person.DocumentPath is null ? name : $"[{name}]({person.DocumentPath})");
    }

    private static NoteText? WrapNote(string? text, CanonicalNoteDetails? details) =>
        text is null ? null : new NoteText(text, details);

    private static string EscapeMarkdown(object? value, bool inTable = false)
    {
        if (value is PersonLinkMarkdown markdown)
        {
            return inTable
                ? markdown.Text.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\r', ' ').Replace('\n', ' ')
                : markdown.Text;
        }

        if (value is NoteText note)
        {
            if (string.IsNullOrWhiteSpace(note.Text))
            {
                return string.Empty;
            }
            var parts = note.Details?.Parts ?? [new CanonicalNotePart(note.Text, null)];
            return string.Join(inTable ? "  " : "  \n  \n  ", parts.Select(part =>
            {
                var lines = part.Text.Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n').Split('\n');
                var text = string.Join(inTable ? " " : "  \n  ", lines.Select(EscapeNoteLine));
                return part.Timestamp is null ? text
                    : $"{text} *({EscapeMarkdown(NoteTimestampFormatter.Format(part.Timestamp), inTable)})*";
            }));
        }

        if (value is null)
        {
            return string.Empty;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        if (inTable)
        {
            text = text.Replace("\r\n", " ", StringComparison.Ordinal)
                .Replace('\r', ' ').Replace('\n', ' ');
        }
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

    private static string EscapeNoteLine(string line)
    {
        var escaped = EscapeMarkdown(line);
        var index = 0;
        while (index < escaped.Length && escaped[index] is ' ' or '\t')
        {
            index++;
        }

        if (index < escaped.Length && escaped[index] is '#' or '-' or '+' or '~' or '=')
        {
            return escaped.Insert(index, "\\");
        }

        var digitEnd = index;
        while (digitEnd < escaped.Length && char.IsAsciiDigit(escaped[digitEnd]))
        {
            digitEnd++;
        }

        if (digitEnd > index && digitEnd + 1 < escaped.Length
            && escaped[digitEnd] is '.' or ')' && char.IsWhiteSpace(escaped[digitEnd + 1]))
        {
            return escaped.Insert(digitEnd, "\\");
        }

        return escaped;
    }

    private sealed record NoteText(string Text, CanonicalNoteDetails? Details);
}
