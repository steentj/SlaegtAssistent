using System.Text;
using Patagames.GedcomNetSdk;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Gedcom;

public sealed class GedcomLoader : IGedcomLoader
{
    public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new GedcomLoadException("A GEDCOM file path is required.");
        }

        if (!File.Exists(filePath))
        {
            throw new GedcomLoadException($"GEDCOM file was not found: '{filePath}'.");
        }

        try
        {
            var parsedGedcom = ParseGedcom(filePath);
            var tree = existingTree ?? new FamilyTree();

            MergeIntoTree(tree, parsedGedcom);

            return tree;
        }
        catch (GedcomLoadException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GedcomLoadException(
                $"Failed to load GEDCOM file '{filePath}'. {exception.Message}",
                exception);
        }
    }

    private static ParsedGedcom ParseGedcom(string filePath)
    {
        var rawGedcomByRecordId = ReadRawPersonSegments(filePath);
        using var stream = CreateNormalizedLineEndingStream(filePath);
        using var parser = new Parser(stream);

        var people = new Dictionary<string, ParsedPerson>(StringComparer.Ordinal);
        var families = new List<ParsedFamily>();
        var sources = new Dictionary<string, ParsedSource>(StringComparer.Ordinal);
        var media = new Dictionary<string, ParsedMedia>(StringComparer.Ordinal);
        var submitters = new Dictionary<string, ParsedSubmitter>(StringComparer.Ordinal);

        ParsedPerson? currentPerson = null;
        ParsedFamily? currentFamily = null;
        ParsedSource? currentSource = null;
        ParsedMedia? currentMedia = null;
        ParsedSubmitter? currentSubmitter = null;
        ParsedEvent? currentEvent = null;
        ParsedCensus? currentCensus = null;
        ParsedSource? currentPersonSource = null;
        ParsedMedia? currentPersonMedia = null;
        bool currentSourceData = false;
        bool currentHeader = false;
        string? headerSubmitterId = null;
        ParsedEvent? currentFamilyEvent = null;

        while (parser.ReadLevel())
        {
            if (!parser.ReadTag())
            {
                break;
            }

            var level = parser.Level;
            var tag = parser.Tag;
            var value = parser.Value?.Trim();

            if (level == 0)
            {
                currentPerson = null;
                currentFamily = null;
                currentSource = null;
                currentMedia = null;
                currentSubmitter = null;
                currentEvent = null;
                currentCensus = null;
                currentPersonSource = null;
                currentPersonMedia = null;
                currentSourceData = false;
                currentHeader = false;
                currentFamilyEvent = null;

                switch (tag)
                {
                    case "HEAD":
                        currentHeader = true;
                        break;

                    case "INDI":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: INDI record without an id at line {parser.No}.");
                        }

                        if (!people.TryGetValue(value, out currentPerson))
                        {
                            currentPerson = new ParsedPerson(value);
                            people.Add(value, currentPerson);
                        }

                        currentPerson.RawGedcom = rawGedcomByRecordId.GetValueOrDefault(value, string.Empty);
                        break;

                    case "FAM":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: FAM record without an id at line {parser.No}.");
                        }

                        currentFamily = new ParsedFamily(value);
                        families.Add(currentFamily);
                        break;

                    case "SOUR":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: SOUR record without an id at line {parser.No}.");
                        }

                        currentSource = new ParsedSource(value);
                        sources[value] = currentSource;
                        break;

                    case "OBJE":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: OBJE record without an id at line {parser.No}.");
                        }

                        currentMedia = new ParsedMedia(value);
                        media[value] = currentMedia;
                        break;

                    case "SUBM":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Malformed GEDCOM: SUBM record without an id at line {parser.No}.");
                        }

                        currentSubmitter = new ParsedSubmitter(value);
                        submitters[value] = currentSubmitter;
                        break;
                }

                continue;
            }

            if (currentPerson is not null)
            {
                ParsePersonLine(
                    currentPerson,
                    level,
                    tag,
                    value,
                    parser.No,
                    ref currentEvent,
                    ref currentCensus,
                    ref currentPersonSource,
                    ref currentPersonMedia,
                    ref currentSourceData);
                continue;
            }

            if (currentFamily is not null)
            {
                ParseFamilyLine(currentFamily, level, tag, value, parser.No, ref currentFamilyEvent);
                continue;
            }

            if (currentSource is not null)
            {
                ParseSourceLine(currentSource, level, tag, value, ref currentSourceData);
                continue;
            }

            if (currentMedia is not null)
            {
                ParseMediaLine(currentMedia, level, tag, value);
                continue;
            }

            if (currentSubmitter is not null)
            {
                ParseSubmitterLine(currentSubmitter, level, tag, value);
                continue;
            }

            if (currentHeader)
            {
                if (level == 1 && tag == "SUBM")
                {
                    headerSubmitterId = NormalizeToken(value);
                }
            }
        }

        return new ParsedGedcom(
            people.Values.ToList(),
            families,
            sources.Values.ToList(),
            media.Values.ToList(),
            submitters.Values.ToList(),
            headerSubmitterId,
            []);
    }

    private static void ParsePersonLine(
        ParsedPerson person,
        int level,
        string tag,
        string? value,
        int line,
        ref ParsedEvent? currentEvent,
        ref ParsedCensus? currentCensus,
        ref ParsedSource? currentSource,
        ref ParsedMedia? currentMedia,
        ref bool currentSourceData)
    {
        if (level == 1)
        {
            currentEvent = null;
            currentCensus = null;
            currentSource = null;
            currentMedia = null;
            currentSourceData = false;

            switch (tag)
            {
                case "NAME":
                    person.FullName = NormalizeName(value);
                    break;

                case "SEX":
                    person.Sex = NormalizeToken(value);
                    break;

                case "BIRT":
                case "DEAT":
                case "BAPM":
                case "CHR":
                case "BURI":
                case "EVEN":
                case "CONF":
                    currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                    person.Events.Add(currentEvent);
                    break;

                case "CENS":
                    currentCensus = new ParsedCensus();
                    person.Census.Add(currentCensus);
                    break;

                default:
                    currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                    person.Events.Add(currentEvent);
                    person.Diagnostics.Add(new GedcomDiagnostic(
                        "Warning",
                        $"Ukendt GEDCOM-hændelsestag '{tag}' er bevaret som en anden hændelse.",
                        line,
                        person.RecordId,
                        tag));
                    break;

                case "SOUR":
                    currentSource = new ParsedSource(NormalizeToken(value));
                    person.Sources.Add(currentSource);
                    break;

                case "OBJE":
                    currentMedia = new ParsedMedia(NormalizeToken(value));
                    person.Media.Add(currentMedia);
                    break;
            }

            return;
        }

        if (currentSource is not null)
        {
            ParseSourceLine(currentSource, level, tag, value, ref currentSourceData);
            return;
        }

        if (currentMedia is not null)
        {
            ParseMediaLine(currentMedia, level, tag, value);
            return;
        }

        if (level != 2)
        {
            return;
        }

        if (currentEvent is not null)
        {
            switch (tag)
            {
                case "DATE":
                    currentEvent.Date = NormalizeToken(value);
                    if (currentEvent.Tag == "BIRT")
                    {
                        person.BirthDate = currentEvent.Date;
                    }
                    else if (currentEvent.Tag == "DEAT")
                    {
                        person.DeathDate = currentEvent.Date;
                    }

                    break;

                case "PLAC":
                    currentEvent.Place = NormalizeToken(value);
                    if (currentEvent.Tag == "BIRT")
                    {
                        person.BirthPlace = currentEvent.Place;
                    }
                    else if (currentEvent.Tag == "DEAT")
                    {
                        person.DeathPlace = currentEvent.Place;
                    }

                    break;

                case "TYPE":
                    currentEvent.Type = NormalizeToken(value);
                    break;

                case "NOTE":
                    currentEvent.Note = NormalizeToken(value);
                    break;

                case "SOUR":
                    currentEvent.Sources.Add(new ParsedSource(NormalizeToken(value)));
                    break;
            }

            return;
        }

        if (currentCensus is null)
        {
            return;
        }

        switch (tag)
        {
            case "DATE":
                currentCensus.Date = NormalizeToken(value);
                break;

            case "PLAC":
                currentCensus.Place = NormalizeToken(value);
                break;

            case "NOTE":
                currentCensus.Note = NormalizeToken(value);
                break;

            case "SOUR":
                currentCensus.Sources.Add(new ParsedSource(NormalizeToken(value)));
                break;
        }
    }

    private static void ParseSourceLine(
        ParsedSource source,
        int level,
        string tag,
        string? value,
        ref bool currentSourceData)
    {
        if (level == 1 || level == 2)
        {
            currentSourceData = tag == "DATA";
        }

        if (level == 1 || level == 2)
        {
            switch (tag)
            {
                case "TITL":
                    source.Title = NormalizeToken(value);
                    break;

                case "AUTH":
                    source.Author = NormalizeToken(value);
                    break;

                case "PUBL":
                    source.Publication = NormalizeToken(value);
                    break;

                case "TEXT":
                    source.Text = NormalizeToken(value);
                    break;

                case "REPO":
                    source.Repository = NormalizeToken(value);
                    break;

                case "PAGE":
                    source.Page = NormalizeToken(value);
                    break;

                case "DATA":
                    source.Data = NormalizeToken(value);
                    break;

                case "DATE":
                    source.Date = NormalizeToken(value);
                    break;
            }

            return;
        }

        if (level > 2 && currentSourceData)
        {
            switch (tag)
            {
                case "DATE":
                    source.Date = NormalizeToken(value);
                    break;

                case "TEXT":
                    source.Text = NormalizeToken(value);
                    break;
            }
        }
    }

    private static void ParseMediaLine(ParsedMedia media, int level, string tag, string? value)
    {
        if (level != 1 && level != 2)
        {
            return;
        }

        switch (tag)
        {
            case "FILE":
                media.File = NormalizeToken(value);
                break;

            case "FORM":
                media.Form = NormalizeToken(value);
                break;

            case "TITL":
                media.Title = NormalizeToken(value);
                break;

            case "TYPE":
                media.Type = NormalizeToken(value);
                break;

            case "NOTE":
                media.Note = NormalizeToken(value);
                break;
        }
    }

    private static void ParseFamilyLine(
        ParsedFamily family,
        int level,
        string tag,
        string? value,
        int line,
        ref ParsedEvent? currentEvent)
    {
        if (level == 1)
        {
            currentEvent = null;
        }

        if (level == 1 && tag is "MARR" or "EVEN" or "CONF")
        {
            currentEvent = new ParsedEvent(tag, NormalizeToken(value));
            family.Events.Add(currentEvent);
            return;
        }

        if (level == 1 && string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (level == 2 && currentEvent is not null)
        {
            switch (tag)
            {
                case "DATE":
                    currentEvent.Date = NormalizeToken(value);
                    break;
                case "PLAC":
                    currentEvent.Place = NormalizeToken(value);
                    break;
                case "TYPE":
                    currentEvent.Type = NormalizeToken(value);
                    break;
                case "NOTE":
                    currentEvent.Note = NormalizeToken(value);
                    break;
                case "SOUR":
                    currentEvent.Sources.Add(new ParsedSource(NormalizeToken(value)));
                    break;
            }

            return;
        }

        if (level != 1)
        {
            return;
        }

        switch (tag)
        {
            case "HUSB":
                family.HusbandId = value!;
                break;

            case "WIFE":
                family.WifeId = value!;
                break;

            case "CHIL":
                family.ChildrenIds.Add(value!);
                break;

            default:
                currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                family.Events.Add(currentEvent);
                family.Diagnostics.Add(new GedcomDiagnostic(
                    "Warning",
                    $"Ukendt GEDCOM-familietag '{tag}' er bevaret som en anden hændelse.",
                    line,
                    family.RecordId,
                    tag));
                break;
        }
    }

    private static void ParseSubmitterLine(ParsedSubmitter submitter, int level, string tag, string? value)
    {
        if (level != 1 && level != 2)
        {
            return;
        }

        switch (tag)
        {
            case "NAME":
                submitter.Name = NormalizeToken(value);
                break;
            case "ADDR":
                submitter.Address = NormalizeToken(value);
                break;
            case "PHON":
                submitter.Phone = NormalizeToken(value);
                break;
            case "EMAIL":
                submitter.Email = NormalizeToken(value);
                break;
            case "WWW":
                submitter.Website = NormalizeToken(value);
                break;
            case "LANG":
                submitter.Language = NormalizeToken(value);
                break;
        }
    }

    private static void MergeIntoTree(FamilyTree tree, ParsedGedcom parsedGedcom)
    {
        tree.Diagnostics.Clear();
        var importedRecordIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parsedSource in parsedGedcom.Sources)
        {
            if (string.IsNullOrWhiteSpace(parsedSource.RecordId))
            {
                throw new GedcomLoadException("Malformed GEDCOM: SOUR record without an id.");
            }

            var source = tree.GetOrAddSource(parsedSource.RecordId);
            CopySource(parsedSource, source);
        }

        foreach (var parsedMedia in parsedGedcom.Media)
        {
            if (string.IsNullOrWhiteSpace(parsedMedia.RecordId))
            {
                throw new GedcomLoadException("Malformed GEDCOM: OBJE record without an id.");
            }

            var media = tree.GetOrAddMedia(parsedMedia.RecordId);
            CopyMedia(parsedMedia, media);
        }

        foreach (var parsedPerson in parsedGedcom.People)
        {
            importedRecordIds.Add(parsedPerson.RecordId);

            var person = tree.GetOrAddPerson(parsedPerson.RecordId);
            person.RawGedcom = parsedPerson.RawGedcom;
            person.FullName = parsedPerson.FullName;
            person.Sex = parsedPerson.Sex;
            person.BirthDate = parsedPerson.BirthDate;
            person.BirthPlace = parsedPerson.BirthPlace;
            person.DeathDate = parsedPerson.DeathDate;
            person.DeathPlace = parsedPerson.DeathPlace;
            person.Sources.Clear();
            person.Media.Clear();
            person.Events.Clear();
            person.Census.Clear();

            foreach (var parsedSource in parsedPerson.Sources)
            {
                person.Sources.Add(CreateSource(tree, parsedSource, parsedGedcom.Sources));
            }

            foreach (var parsedMedia in parsedPerson.Media)
            {
                person.Media.Add(CreateMedia(tree, parsedMedia, parsedGedcom.Media));
            }

            foreach (var parsedEvent in parsedPerson.Events)
            {
                person.Events.Add(CreateEvent(tree, parsedEvent, parsedGedcom.Sources));
            }

            foreach (var parsedCensus in parsedPerson.Census)
            {
                person.Census.Add(CreateCensus(tree, parsedCensus, parsedGedcom.Sources));
            }

            foreach (var diagnostic in parsedPerson.Diagnostics)
            {
                tree.Diagnostics.Add(diagnostic);
            }
        }

        tree.SubmitterRecordId = parsedGedcom.HeaderSubmitterId;
        tree.Submitter = CreateSubmitter(
            parsedGedcom.HeaderSubmitterId,
            parsedGedcom.Submitters);

        foreach (var person in tree.People)
        {
            person.Families.Clear();
        }

        foreach (var recordId in importedRecordIds)
        {
            if (!tree.TryGetPerson(recordId, out var person))
            {
                continue;
            }

            UnlinkPerson(person);
        }

        foreach (var family in parsedGedcom.Families)
        {
            var domainFamily = tree.GetOrAddFamily(family.RecordId);
            domainFamily.Events.Clear();
            domainFamily.Children.Clear();
            domainFamily.Husband = null;
            domainFamily.Wife = null;

            var parents = new List<Person>(2);

            if (!string.IsNullOrWhiteSpace(family.HusbandId)
                && tree.TryGetPerson(family.HusbandId, out var husband))
            {
                parents.Add(husband);
                domainFamily.Husband = husband;
            }

            if (!string.IsNullOrWhiteSpace(family.WifeId)
                && tree.TryGetPerson(family.WifeId, out var wife)
                && parents.All(parent => parent.RecordId != wife.RecordId))
            {
                parents.Add(wife);
                domainFamily.Wife = wife;
            }

            foreach (var childId in family.ChildrenIds)
            {
                if (!tree.TryGetPerson(childId, out var child))
                {
                    continue;
                }

                foreach (var parent in parents)
                {
                    AddIfMissing(parent.Children, child, person => person.RecordId);
                    AddIfMissing(child.Parents, parent, person => person.RecordId);
                }

                foreach (var diagnostic in family.Diagnostics)
                {
                    tree.Diagnostics.Add(diagnostic);
                }

                AddIfMissing(domainFamily.Children, child, person => person.RecordId);
            }

            foreach (var parsedEvent in family.Events)
            {
                domainFamily.Events.Add(CreateEvent(tree, parsedEvent, parsedGedcom.Sources));
            }

            foreach (var parent in parents)
            {
                AddIfMissing(parent.Families, domainFamily, familyRecord => familyRecord.RecordId);
            }
        }

        foreach (var diagnostic in parsedGedcom.Diagnostics)
        {
            tree.Diagnostics.Add(diagnostic);
        }
    }

    private static Submitter? CreateSubmitter(
        string? recordId,
        IReadOnlyCollection<ParsedSubmitter> submitters)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            return null;
        }

        var parsed = submitters.FirstOrDefault(submitter => submitter.RecordId == recordId);
        if (parsed is null)
        {
            return null;
        }

        return new Submitter(parsed.RecordId)
        {
            Name = parsed.Name,
            Address = parsed.Address,
            Phone = parsed.Phone,
            Email = parsed.Email,
            Website = parsed.Website,
            Language = parsed.Language,
        };
    }

    private static Source CreateSource(
        FamilyTree tree,
        ParsedSource parsedSource,
        IReadOnlyCollection<ParsedSource> parsedSources)
    {
        var source = new Source(parsedSource.RecordId);
        var recordId = parsedSource.RecordId;

        if (!string.IsNullOrWhiteSpace(recordId))
        {
            var parsedRecord = parsedSources.FirstOrDefault(candidate => candidate.RecordId == recordId);
            if (parsedRecord is not null)
            {
                CopySource(parsedRecord, source);
            }
            else if (tree.FindSource(recordId) is { } existingSource)
            {
                CopySource(existingSource, source);
            }
        }

        CopySource(parsedSource, source, overwriteWithNull: false);
        return source;
    }

    private static Media CreateMedia(
        FamilyTree tree,
        ParsedMedia parsedMedia,
        IReadOnlyCollection<ParsedMedia> parsedMediaRecords)
    {
        var media = new Media(parsedMedia.RecordId);
        var recordId = parsedMedia.RecordId;

        if (!string.IsNullOrWhiteSpace(recordId))
        {
            var parsedRecord = parsedMediaRecords.FirstOrDefault(candidate => candidate.RecordId == recordId);
            if (parsedRecord is not null)
            {
                CopyMedia(parsedRecord, media);
            }
            else if (tree.FindMedia(recordId) is { } existingMedia)
            {
                CopyMedia(existingMedia, media);
            }
        }

        CopyMedia(parsedMedia, media, overwriteWithNull: false);
        return media;
    }

    private static GedcomEvent CreateEvent(
        FamilyTree tree,
        ParsedEvent parsedEvent,
        IReadOnlyCollection<ParsedSource> parsedSources)
    {
        var gedcomEvent = new GedcomEvent(parsedEvent.Tag)
        {
            Category = GedcomEventClassifier.Classify(parsedEvent.Tag, parsedEvent.Type),
            Value = parsedEvent.Value,
            Date = parsedEvent.Date,
            Place = parsedEvent.Place,
            Type = parsedEvent.Type,
            Note = parsedEvent.Note
        };

        foreach (var parsedSource in parsedEvent.Sources)
        {
            gedcomEvent.Sources.Add(CreateSource(tree, parsedSource, parsedSources));
        }

        return gedcomEvent;
    }

    private static Census CreateCensus(
        FamilyTree tree,
        ParsedCensus parsedCensus,
        IReadOnlyCollection<ParsedSource> parsedSources)
    {
        var census = new Census
        {
            Date = parsedCensus.Date,
            Place = parsedCensus.Place,
            Note = parsedCensus.Note
        };

        foreach (var parsedSource in parsedCensus.Sources)
        {
            census.Sources.Add(CreateSource(tree, parsedSource, parsedSources));
        }

        return census;
    }

    private static void CopySource(ParsedSource source, Source target)
    {
        target.Title = source.Title;
        target.Author = source.Author;
        target.Publication = source.Publication;
        target.Text = source.Text;
        target.Repository = source.Repository;
        target.Page = source.Page;
        target.Data = source.Data;
        target.Date = source.Date;
    }

    private static void CopySource(Source source, Source target)
    {
        target.Title = source.Title;
        target.Author = source.Author;
        target.Publication = source.Publication;
        target.Text = source.Text;
        target.Repository = source.Repository;
        target.Page = source.Page;
        target.Data = source.Data;
        target.Date = source.Date;
    }

    private static void CopySource(ParsedSource source, Source target, bool overwriteWithNull)
    {
        if (overwriteWithNull || source.Title is not null)
        {
            target.Title = source.Title;
        }

        if (overwriteWithNull || source.Author is not null)
        {
            target.Author = source.Author;
        }

        if (overwriteWithNull || source.Publication is not null)
        {
            target.Publication = source.Publication;
        }

        if (overwriteWithNull || source.Text is not null)
        {
            target.Text = source.Text;
        }

        if (overwriteWithNull || source.Repository is not null)
        {
            target.Repository = source.Repository;
        }

        if (overwriteWithNull || source.Page is not null)
        {
            target.Page = source.Page;
        }

        if (overwriteWithNull || source.Data is not null)
        {
            target.Data = source.Data;
        }

        if (overwriteWithNull || source.Date is not null)
        {
            target.Date = source.Date;
        }
    }

    private static void CopyMedia(ParsedMedia media, Media target)
    {
        target.File = media.File;
        target.Form = media.Form;
        target.Title = media.Title;
        target.Type = media.Type;
        target.Note = media.Note;
    }

    private static void CopyMedia(Media media, Media target)
    {
        target.File = media.File;
        target.Form = media.Form;
        target.Title = media.Title;
        target.Type = media.Type;
        target.Note = media.Note;
    }

    private static void CopyMedia(ParsedMedia media, Media target, bool overwriteWithNull)
    {
        if (overwriteWithNull || media.File is not null)
        {
            target.File = media.File;
        }

        if (overwriteWithNull || media.Form is not null)
        {
            target.Form = media.Form;
        }

        if (overwriteWithNull || media.Title is not null)
        {
            target.Title = media.Title;
        }

        if (overwriteWithNull || media.Type is not null)
        {
            target.Type = media.Type;
        }

        if (overwriteWithNull || media.Note is not null)
        {
            target.Note = media.Note;
        }
    }

    private static void UnlinkPerson(Person person)
    {
        foreach (var parent in person.Parents.ToList())
        {
            parent.Children.Remove(person);
        }

        foreach (var child in person.Children.ToList())
        {
            child.Parents.Remove(person);
        }

        person.Parents.Clear();
        person.Children.Clear();
    }

    private static void AddIfMissing<T>(
        IList<T> items,
        T itemToAdd,
        Func<T, string> identity)
    {
        if (items.Any(existing => string.Equals(identity(existing), identity(itemToAdd), StringComparison.Ordinal)))
        {
            return;
        }

        items.Add(itemToAdd);
    }

    private static MemoryStream CreateNormalizedLineEndingStream(string filePath)
    {
        var fileContent = File.ReadAllText(filePath);
        var normalizedLineEndings = NormalizeLineEndings(fileContent);
        return new MemoryStream(Encoding.UTF8.GetBytes(normalizedLineEndings));
    }

    private static IReadOnlyDictionary<string, string> ReadRawPersonSegments(string filePath)
    {
        var segments = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = File.ReadAllLines(filePath);
        var currentRecordId = string.Empty;
        var currentLines = new List<string>();

        void StoreCurrentSegment()
        {
            if (currentRecordId.Length == 0)
            {
                return;
            }

            segments[currentRecordId] = string.Join(Environment.NewLine, currentLines);
            currentRecordId = string.Empty;
            currentLines.Clear();
        }

        foreach (var line in lines)
        {
            if (TryReadIndividualHeader(line, out var recordId))
            {
                StoreCurrentSegment();
                currentRecordId = recordId;
                currentLines.Add(line);
                continue;
            }

            if (currentRecordId.Length > 0 && line.StartsWith("0 ", StringComparison.Ordinal))
            {
                StoreCurrentSegment();
                continue;
            }

            if (currentRecordId.Length > 0)
            {
                currentLines.Add(line);
            }
        }

        StoreCurrentSegment();
        return segments;
    }

    private static bool TryReadIndividualHeader(string line, out string recordId)
    {
        recordId = string.Empty;
        var fields = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 3
            || fields[0] != "0"
            || !string.Equals(fields[2], "INDI", StringComparison.Ordinal))
        {
            return false;
        }

        recordId = fields[1];
        return true;
    }

    private static string NormalizeLineEndings(string text)
    {
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal);

        return normalized.EndsWith("\r\n", StringComparison.Ordinal)
            ? normalized
            : normalized + "\r\n";
    }

    private static string? NormalizeName(string? value)
    {
        var normalized = NormalizeToken(value);
        if (normalized is null)
        {
            return null;
        }

        var noSlash = normalized.Replace("/", string.Empty, StringComparison.Ordinal);
        var compact = string.Join(' ', noSlash.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return compact.Length == 0 ? null : compact;
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private sealed record ParsedGedcom(
        IReadOnlyCollection<ParsedPerson> People,
        IReadOnlyCollection<ParsedFamily> Families,
        IReadOnlyCollection<ParsedSource> Sources,
        IReadOnlyCollection<ParsedMedia> Media,
        IReadOnlyCollection<ParsedSubmitter> Submitters,
        string? HeaderSubmitterId,
        IReadOnlyCollection<GedcomDiagnostic> Diagnostics);

    private sealed class ParsedPerson
    {
        public ParsedPerson(string recordId)
        {
            RecordId = recordId;
        }

        public string RecordId { get; }

        public string RawGedcom { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string? Sex { get; set; }

        public string? BirthDate { get; set; }

        public string? BirthPlace { get; set; }

        public string? DeathDate { get; set; }

        public string? DeathPlace { get; set; }

        public IList<ParsedSource> Sources { get; } = new List<ParsedSource>();

        public IList<ParsedMedia> Media { get; } = new List<ParsedMedia>();

        public IList<ParsedEvent> Events { get; } = new List<ParsedEvent>();

        public IList<ParsedCensus> Census { get; } = new List<ParsedCensus>();

        public IList<GedcomDiagnostic> Diagnostics { get; } = new List<GedcomDiagnostic>();
    }

    private sealed class ParsedEvent
    {
        public ParsedEvent(string tag, string? value)
        {
            Tag = tag;
            Value = value;
        }

        public string Tag { get; }

        public string? Value { get; }

        public string? Date { get; set; }

        public string? Place { get; set; }

        public string? Type { get; set; }

        public string? Note { get; set; }

        public IList<ParsedSource> Sources { get; } = new List<ParsedSource>();
    }

    private sealed class ParsedCensus
    {
        public string? Date { get; set; }

        public string? Place { get; set; }

        public string? Note { get; set; }

        public IList<ParsedSource> Sources { get; } = new List<ParsedSource>();
    }

    private sealed class ParsedFamily
    {
        public ParsedFamily(string recordId)
        {
            RecordId = recordId;
        }

        public string RecordId { get; }

        public string? HusbandId { get; set; }

        public string? WifeId { get; set; }

        public IList<string> ChildrenIds { get; } = new List<string>();

        public IList<ParsedEvent> Events { get; } = new List<ParsedEvent>();

        public IList<GedcomDiagnostic> Diagnostics { get; } = new List<GedcomDiagnostic>();
    }

    private sealed class ParsedSource
    {
        public ParsedSource(string? recordId)
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
    }

    private sealed class ParsedMedia
    {
        public ParsedMedia(string? recordId)
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

    private sealed class ParsedSubmitter
    {
        public ParsedSubmitter(string recordId)
        {
            RecordId = recordId;
        }

        public string RecordId { get; }

        public string? Name { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }

        public string? Language { get; set; }
    }
}
