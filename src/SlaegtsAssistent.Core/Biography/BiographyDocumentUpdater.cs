namespace SlaegtsAssistent.Core.Biography;

public static class BiographyDocumentUpdater
{
    public static string ApplyGedcomChoices(
        BiographyDocument document,
        BiographyFactsSnapshot gedcomFacts,
        IReadOnlyDictionary<string, bool> useGedcomByField)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(gedcomFacts);
        ArgumentNullException.ThrowIfNull(useGedcomByField);

        if (document.Metadata is not { } metadata)
        {
            throw new InvalidOperationException("Dokumentet mangler metadata og kan ikke synkroniseres sikkert.");
        }

        var current = metadata.Facts;
        var updatedFacts = current with
        {
            FullName = UseGedcom("Navn") ? gedcomFacts.FullName : current.FullName,
            Sex = UseGedcom("Køn") ? gedcomFacts.Sex : current.Sex,
            BirthDate = UseGedcom("Fødselsdato") ? gedcomFacts.BirthDate : current.BirthDate,
            BirthPlace = UseGedcom("Fødested") ? gedcomFacts.BirthPlace : current.BirthPlace,
            DeathDate = UseGedcom("Dødsdato") ? gedcomFacts.DeathDate : current.DeathDate,
            DeathPlace = UseGedcom("Dødssted") ? gedcomFacts.DeathPlace : current.DeathPlace,
            ParentRecordIds = UseGedcom("Forældre")
                ? gedcomFacts.ParentRecordIds
                : current.ParentRecordIds,
        };

        var updatedMetadata = metadata with
        {
            DisplayName = UseGedcom("Navn") ? gedcomFacts.FullName : metadata.DisplayName,
            Facts = updatedFacts,
            GedcomBaselineHash = gedcomFacts.ComputeFingerprint(),
        };

        var updatedBody = document.Body;
        if (UseGedcom("Fødselsdato") || UseGedcom("Fødested"))
        {
            updatedBody = ReplaceFact(
                updatedBody,
                "Født",
                FormatDateAndPlace(updatedFacts.BirthDate, updatedFacts.BirthPlace));
        }

        if (UseGedcom("Dødsdato") || UseGedcom("Dødssted"))
        {
            updatedBody = ReplaceFact(
                updatedBody,
                "Død",
                FormatDateAndPlace(updatedFacts.DeathDate, updatedFacts.DeathPlace));
        }

        if (UseGedcom("Forældre"))
        {
            updatedBody = ReplaceFact(
                updatedBody,
                "Forældre",
                updatedFacts.ParentDisplayText);
        }

        if (UseGedcom("Navn") && !string.IsNullOrWhiteSpace(updatedFacts.FullName))
        {
            updatedBody = ReplaceHeading(updatedBody, updatedFacts.FullName!);
        }

        return BiographyDocumentSerializer.Serialize(updatedMetadata, updatedBody);

        bool UseGedcom(string fieldName)
        {
            return useGedcomByField.TryGetValue(fieldName, out var useGedcom) && useGedcom;
        }
    }

    private static string ReplaceFact(string body, string label, string? value)
    {
        var lines = body.Split('\n').ToList();
        var prefix = $"- **{label}:**";
        var replacement = string.IsNullOrWhiteSpace(value)
            ? null
            : $"{prefix} {value}";
        var inFactsSection = false;
        var replaced = false;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var content = line.TrimEnd('\r');
            if (content.Equals("## Fakta", StringComparison.Ordinal))
            {
                inFactsSection = true;
                continue;
            }

            if (inFactsSection &&
                content.StartsWith("## ", StringComparison.Ordinal) &&
                !content.Equals("## Fakta", StringComparison.Ordinal))
            {
                if (!replaced && replacement is not null)
                {
                    lines.Insert(index, replacement);
                }

                break;
            }

            if (inFactsSection && content.StartsWith(prefix, StringComparison.Ordinal))
            {
                var lineEnding = line.EndsWith('\r') ? "\r" : string.Empty;
                if (replacement is null)
                {
                    lines.RemoveAt(index);
                    index--;
                }
                else
                {
                    lines[index] = replacement + lineEnding;
                }

                replaced = true;
            }
        }

        return string.Join('\n', lines);
    }

    private static string ReplaceHeading(string body, string name)
    {
        var lines = body.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].TrimEnd('\r').StartsWith("# ", StringComparison.Ordinal))
            {
                var lineEnding = lines[index].EndsWith('\r') ? "\r" : string.Empty;
                lines[index] = $"# {name}{lineEnding}";
                break;
            }
        }

        return string.Join('\n', lines);
    }

    private static string? FormatDateAndPlace(string? date, string? place)
    {
        var hasDate = !string.IsNullOrWhiteSpace(date);
        var hasPlace = !string.IsNullOrWhiteSpace(place);
        if (!hasDate && !hasPlace)
        {
            return null;
        }

        if (hasDate && hasPlace)
        {
            return $"{date} i {place}";
        }

        return hasDate ? date : $"i {place}";
    }
}
