using System.Text;
using System.Threading;
using Patagames.GedcomNetSdk;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Gedcom;

public sealed class GedcomLoader : IGedcomLoader
{
    private const char ContinuedLineBreak = '\uE000';

    private static readonly HashSet<string> PersonEventTags = new(StringComparer.Ordinal)
    {
        "ADOP", "BAPL", "BAPM", "BARM", "BASM", "BIRT", "BLES", "BURI", "CAST", "CHR",
        "CHRA", "CONF", "CONL", "CREM", "DEAT", "DSCR", "EDUC", "EMIG", "ENDL", "EVEN",
        "FACT", "FCOM", "GRAD", "IDNO", "IMMI", "NATI", "NATU", "NCHI", "NMR", "OCCU",
        "ORDN", "PROB", "PROP", "RELI", "RESI", "RETI", "SLGC", "SSN", "TITL", "WILL",
    };

    private static readonly HashSet<string> FamilyEventTags = new(StringComparer.Ordinal)
    {
        "ANUL", "CENS", "DIV", "DIVF", "ENGA", "EVEN", "MARB", "MARC", "MARL", "MARR",
        "MARS", "RESI", "SLGS",
    };

    private static readonly HashSet<string> PersonStructureTags = new(StringComparer.Ordinal)
    {
        "AFN", "ALIA", "ANCI", "ASSO", "CHAN", "DESI", "FAMC", "FAMS", "NAME", "OBJE",
        "REFN", "RESN", "RFN", "RIN", "SEX", "SOUR", "SUBM", "UID", "_FSFTID", "_UID",
    };

    private static readonly HashSet<string> FamilyStructureTags = new(StringComparer.Ordinal)
    {
        "CHAN", "CHIL", "HUSB", "NCHI", "OBJE", "REFN", "RESN", "RIN", "SOUR",
        "SUBM", "WIFE",
    };

    public FamilyTree Load(string filePath, FamilyTree? existingTree = null)
    {
        return Load(filePath, existingTree, CancellationToken.None);
    }

    public FamilyTree Load(
        string filePath,
        FamilyTree? existingTree,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new GedcomLoadException("Der skal angives en sti til en GEDCOM-fil.");
        }

        if (!File.Exists(filePath))
        {
            throw new GedcomLoadException($"GEDCOM-filen blev ikke fundet: '{filePath}'.");
        }

        try
        {
            var parsedGedcom = ParseGedcom(filePath, cancellationToken);
            var tree = existingTree ?? new FamilyTree();

            MergeIntoTree(tree, parsedGedcom, cancellationToken);

            return tree;
        }
        catch (GedcomLoadException exception) when (exception.ImportReport is null)
        {
            var diagnostic = new GedcomDiagnostic(
                GedcomDiagnosticSeverity.Fatal,
                exception.Message,
                null,
                null,
                "FIL",
                "Hele importen blev afbrudt uden ændringer.",
                filePath);
            throw new GedcomLoadException(
                exception.Message,
                exception,
                new GedcomImportReport(0, 0, 0, 1, [diagnostic]));
        }
        catch (GedcomLoadException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var message = $"GEDCOM-filen '{filePath}' kunne ikke indlæses. {exception.Message}";
            var diagnostic = new GedcomDiagnostic(
                GedcomDiagnosticSeverity.Fatal,
                message,
                null,
                null,
                "FIL",
                "Hele importen blev afbrudt uden ændringer.",
                filePath);
            throw new GedcomLoadException(
                message,
                exception,
                new GedcomImportReport(0, 0, 0, 1, [diagnostic]));
        }
    }

    private static ParsedGedcom ParseGedcom(
        string filePath,
        CancellationToken cancellationToken)
    {
        var decoded = ReadGedcomFile(filePath);
        var prepared = PrepareGedcomText(decoded.Text, filePath);
        var rawGedcomByRecordId = ReadRawPersonSegments(decoded.Text);
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = CreateParserStream(prepared.Text);
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
        int currentPersonSourceLevel = -1;
        ParsedMedia? currentPersonMedia = null;
        bool currentSourceData = false;
        bool currentHeader = false;
        string? headerSubmitterId = null;
        ParsedEvent? currentFamilyEvent = null;
        ParsedSource? currentFamilySource = null;
        int currentFamilySourceLevel = -1;
        bool currentFamilySourceData = false;

        while (parser.ReadLevel())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!parser.ReadTag())
            {
                break;
            }

            var level = parser.Level;
            var tag = parser.Tag;
            var value = parser.Value;

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
                currentPersonSourceLevel = -1;
                currentPersonMedia = null;
                currentSourceData = false;
                currentHeader = false;
                currentFamilyEvent = null;
                currentFamilySource = null;
                currentFamilySourceLevel = -1;
                currentFamilySourceData = false;

                switch (tag)
                {
                    case "HEAD":
                        currentHeader = true;
                        break;

                    case "INDI":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Ugyldig GEDCOM: Personpost uden id på linje {parser.No}.");
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
                                $"Ugyldig GEDCOM: Familiepost uden id på linje {parser.No}.");
                        }

                        currentFamily = new ParsedFamily(value);
                        families.Add(currentFamily);
                        break;

                    case "SOUR":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Ugyldig GEDCOM: Kildepost uden id på linje {parser.No}.");
                        }

                        currentSource = new ParsedSource(value);
                        sources[value] = currentSource;
                        break;

                    case "OBJE":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Ugyldig GEDCOM: Mediepost uden id på linje {parser.No}.");
                        }

                        currentMedia = new ParsedMedia(value);
                        media[value] = currentMedia;
                        break;

                    case "SUBM":
                        if (!parser.HasId || string.IsNullOrWhiteSpace(value))
                        {
                            throw new GedcomLoadException(
                                $"Ugyldig GEDCOM: Afsenderpost uden id på linje {parser.No}.");
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
                    ref currentPersonSourceLevel,
                    ref currentPersonMedia,
                    ref currentSourceData);
                continue;
            }

            if (currentFamily is not null)
            {
                ParseFamilyLine(
                    currentFamily,
                    level,
                    tag,
                    value,
                    parser.No,
                    ref currentFamilyEvent,
                    ref currentFamilySource,
                    ref currentFamilySourceLevel,
                    ref currentFamilySourceData);
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

        var diagnostics = decoded.Diagnostics
            .Concat(prepared.Diagnostics)
            .Concat(people.Values.SelectMany(person => person.Diagnostics))
            .Concat(families.SelectMany(family => family.Diagnostics))
            .Select(diagnostic => diagnostic with
            {
                FilePath = diagnostic.FilePath ?? filePath,
                Consequence = diagnostic.Consequence
                    ?? "Værdien blev bevaret som ukendt og kræver brugerens gennemgang.",
            })
            .ToList();

        foreach (var person in people.Values)
        {
            person.Diagnostics.Clear();
        }

        foreach (var family in families)
        {
            family.Diagnostics.Clear();
        }

        return new ParsedGedcom(
            people.Values.ToList(),
            families,
            sources.Values.ToList(),
            media.Values.ToList(),
            submitters.Values.ToList(),
            headerSubmitterId,
            diagnostics,
            prepared.SkippedRecords);
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
        ref int currentSourceLevel,
        ref ParsedMedia? currentMedia,
        ref bool currentSourceData)
    {
        if (currentSource is not null && level > currentSourceLevel)
        {
            ParseSourceLine(currentSource, level, tag, value, ref currentSourceData);
            return;
        }

        if (currentSource is not null && level <= currentSourceLevel)
        {
            currentSource = null;
            currentSourceLevel = -1;
            currentSourceData = false;
        }

        if (level == 1)
        {
            currentEvent = null;
            currentCensus = null;
            currentSource = null;
            currentSourceLevel = -1;
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

                case var eventTag when PersonEventTags.Contains(eventTag):
                    currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                    person.Events.Add(currentEvent);
                    break;

                case "CENS":
                    currentCensus = new ParsedCensus();
                    person.Census.Add(currentCensus);
                    break;

                case "NOTE":
                    if (NormalizeToken(value) is { } note)
                    {
                        person.Notes.Add(note);
                    }

                    break;

                case "SOUR":
                    currentSource = CreateCitation(value);
                    currentSourceLevel = level;
                    person.Sources.Add(currentSource);
                    break;

                case "OBJE":
                    currentMedia = new ParsedMedia(NormalizeToken(value));
                    person.Media.Add(currentMedia);
                    break;

                case var structureTag when PersonStructureTags.Contains(structureTag):
                    break;

                default:
                    currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                    person.Events.Add(currentEvent);
                    person.Diagnostics.Add(new GedcomDiagnostic(
                        GedcomDiagnosticSeverity.Warning,
                        $"Ukendt GEDCOM-hændelsestag '{tag}' er bevaret som en anden hændelse.",
                        line,
                        person.RecordId,
                        tag));
                    break;

            }

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
                    currentSource = CreateCitation(value);
                    currentSourceLevel = level;
                    currentEvent.Sources.Add(currentSource);
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
                currentSource = CreateCitation(value);
                currentSourceLevel = level;
                currentCensus.Sources.Add(currentSource);
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
        if (tag == "DATA")
        {
            currentSourceData = true;
            source.Data = NormalizeToken(value);
            return;
        }

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
            case "DATE":
                source.Date = NormalizeToken(value);
                break;
            case "NOTE":
                source.Note = NormalizeToken(value);
                break;
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
        ref ParsedEvent? currentEvent,
        ref ParsedSource? currentSource,
        ref int currentSourceLevel,
        ref bool currentSourceData)
    {
        if (currentSource is not null && level > currentSourceLevel)
        {
            ParseSourceLine(currentSource, level, tag, value, ref currentSourceData);
            return;
        }

        if (currentSource is not null && level <= currentSourceLevel)
        {
            currentSource = null;
            currentSourceLevel = -1;
            currentSourceData = false;
        }

        if (level == 1)
        {
            currentEvent = null;
        }

        if (level == 1 && FamilyEventTags.Contains(tag))
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
                    currentSource = CreateCitation(value);
                    currentSourceLevel = level;
                    currentEvent.Sources.Add(currentSource);
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
                family.ChildrenIds.Add(NormalizeToken(value)!);
                break;

            case "NOTE":
                if (NormalizeToken(value) is { } note)
                {
                    family.Notes.Add(note);
                }

                break;

            case "SOUR":
                currentSource = CreateCitation(value);
                currentSourceLevel = level;
                family.Sources.Add(currentSource);
                break;

            case var structureTag when FamilyStructureTags.Contains(structureTag):
                break;

            default:
                currentEvent = new ParsedEvent(tag, NormalizeToken(value));
                family.Events.Add(currentEvent);
                family.Diagnostics.Add(new GedcomDiagnostic(
                    GedcomDiagnosticSeverity.Warning,
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

    private static void MergeIntoTree(
        FamilyTree tree,
        ParsedGedcom parsedGedcom,
        CancellationToken cancellationToken)
    {
        tree.Diagnostics.Clear();
        var importedRecordIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parsedSource in parsedGedcom.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(parsedSource.RecordId))
            {
                throw new GedcomLoadException("Ugyldig GEDCOM: Kildepost uden id.");
            }

            var source = tree.GetOrAddSource(parsedSource.RecordId);
            CopySource(parsedSource, source);
        }

        foreach (var parsedMedia in parsedGedcom.Media)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(parsedMedia.RecordId))
            {
                throw new GedcomLoadException("Ugyldig GEDCOM: Mediepost uden id.");
            }

            var media = tree.GetOrAddMedia(parsedMedia.RecordId);
            CopyMedia(parsedMedia, media);
        }

        foreach (var parsedPerson in parsedGedcom.People)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            person.Notes.Clear();
            person.Media.Clear();
            person.Events.Clear();
            person.Census.Clear();

            foreach (var parsedSource in parsedPerson.Sources)
            {
                person.Sources.Add(CreateSource(tree, parsedSource, parsedGedcom.Sources));
            }

            foreach (var note in parsedPerson.Notes)
            {
                person.Notes.Add(note);
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
            cancellationToken.ThrowIfCancellationRequested();
            var domainFamily = tree.GetOrAddFamily(family.RecordId);
            domainFamily.Events.Clear();
            domainFamily.Sources.Clear();
            domainFamily.Notes.Clear();
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

                AddIfMissing(domainFamily.Children, child, person => person.RecordId);
            }

            foreach (var parsedSource in family.Sources)
            {
                domainFamily.Sources.Add(CreateSource(tree, parsedSource, parsedGedcom.Sources));
            }

            foreach (var note in family.Notes)
            {
                domainFamily.Notes.Add(note);
            }

            foreach (var parsedEvent in family.Events)
            {
                domainFamily.Events.Add(CreateEvent(tree, parsedEvent, parsedGedcom.Sources));
            }

            foreach (var parent in parents)
            {
                AddIfMissing(parent.Families, domainFamily, familyRecord => familyRecord.RecordId);
            }

            foreach (var diagnostic in family.Diagnostics)
            {
                tree.Diagnostics.Add(diagnostic);
            }
        }

        foreach (var diagnostic in parsedGedcom.Diagnostics)
        {
            tree.Diagnostics.Add(diagnostic);
        }

        var importedRecordIdsForReport = parsedGedcom.People.Select(person => person.RecordId)
            .Concat(parsedGedcom.Families.Select(family => family.RecordId))
            .Concat(parsedGedcom.Sources.Select(source => source.RecordId))
            .Concat(parsedGedcom.Media.Select(mediaRecord => mediaRecord.RecordId))
            .Concat(parsedGedcom.Submitters.Select(submitter => submitter.RecordId))
            .Where(recordId => !string.IsNullOrWhiteSpace(recordId))
            .Select(recordId => recordId!)
            .ToHashSet(StringComparer.Ordinal);
        var importedWithWarnings = tree.Diagnostics
            .Where(diagnostic => diagnostic.RecordId is not null
                && importedRecordIdsForReport.Contains(diagnostic.RecordId))
            .Select(diagnostic => diagnostic.RecordId!)
            .Distinct(StringComparer.Ordinal)
            .Count();
        tree.ImportReport = new GedcomImportReport(
            importedRecordIdsForReport.Count,
            importedWithWarnings,
            parsedGedcom.SkippedRecords,
            0,
            tree.Diagnostics.ToList());
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
        target.Note = source.Note;
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
        target.Note = source.Note;
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

        if (overwriteWithNull || source.Note is not null)
        {
            target.Note = source.Note;
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

    private static MemoryStream CreateParserStream(string text)
    {
        var continuedText = CollapseContinuationLines(text);
        var normalizedLineEndings = NormalizeLineEndings(continuedText);
        return new MemoryStream(Encoding.UTF8.GetBytes(normalizedLineEndings));
    }

    private static PreparedGedcom PrepareGedcomText(string text, string filePath)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var nonEmptyLines = lines
            .Select((line, index) => new { Line = line, Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Line))
            .ToList();

        if (nonEmptyLines.Count == 0 || nonEmptyLines[0].Line.Trim() != "0 HEAD")
        {
            throw CreateFatalStructureException(
                filePath,
                nonEmptyLines.Count == 0 ? 1 : nonEmptyLines[0].Index + 1,
                "HEAD",
                "GEDCOM-filen mangler en gyldig HEAD-post som første post.");
        }

        if (nonEmptyLines[^1].Line.Trim() != "0 TRLR")
        {
            throw CreateFatalStructureException(
                filePath,
                nonEmptyLines[^1].Index + 1,
                "TRLR",
                "GEDCOM-filen mangler en afsluttende TRLR-post.");
        }

        var output = new List<string>(lines.Length);
        var diagnostics = new List<GedcomDiagnostic>();
        var skippedRecords = 0;
        var skipCurrentRecord = false;
        var seenRecordIds = new HashSet<string>(StringComparer.Ordinal);
        string? currentRecordId = null;
        string? currentTag = null;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            if (line.StartsWith("0 ", StringComparison.Ordinal))
            {
                skipCurrentRecord = false;
                currentRecordId = null;
                currentTag = ReadLevelZeroTag(line);
                TryReadRecordHeader(line, out currentRecordId);

                if (RequiresRecordId(currentTag) && currentRecordId is null)
                {
                    skipCurrentRecord = true;
                    skippedRecords++;
                    diagnostics.Add(new GedcomDiagnostic(
                        GedcomDiagnosticSeverity.Error,
                        RecordIdErrorMessage(currentTag),
                        index + 1,
                        null,
                        currentTag,
                        RecordSkipConsequence(currentTag),
                        filePath));
                    continue;
                }

                if (currentRecordId is not null && !seenRecordIds.Add(currentRecordId))
                {
                    skipCurrentRecord = true;
                    skippedRecords++;
                    diagnostics.Add(new GedcomDiagnostic(
                        GedcomDiagnosticSeverity.Error,
                        $"Record-id '{currentRecordId}' forekommer flere gange i GEDCOM-filen.",
                        index + 1,
                        currentRecordId,
                        currentTag,
                        "Den senere dubletpost blev sprunget over; den første post blev bevaret.",
                        filePath));
                    continue;
                }

                if (currentTag is "NOTE" or "REPO" or "SUBN")
                {
                    skipCurrentRecord = true;
                    skippedRecords++;
                    diagnostics.Add(new GedcomDiagnostic(
                        GedcomDiagnosticSeverity.Error,
                        $"GEDCOM-posten '{currentTag}' har endnu ikke et selvstændigt domæneobjekt.",
                        index + 1,
                        currentRecordId,
                        currentTag,
                        "Posten blev ikke importeret; pointere i understøttede felter blev bevaret.",
                        filePath));
                    continue;
                }

                if (!IsSupportedLevelZeroRecord(currentTag))
                {
                    skipCurrentRecord = true;
                    skippedRecords++;
                    diagnostics.Add(new GedcomDiagnostic(
                        GedcomDiagnosticSeverity.Error,
                        $"Den ukendte GEDCOM-post '{currentTag}' kan ikke fortolkes sikkert.",
                        index + 1,
                        currentRecordId,
                        currentTag,
                        "Den ukendte post blev sprunget over; øvrige poster blev bevaret.",
                        filePath));
                    continue;
                }
            }

            if (skipCurrentRecord)
            {
                continue;
            }

            if (line.Length > 0 && !IsSyntacticallyValidGedcomLine(line))
            {
                diagnostics.Add(new GedcomDiagnostic(
                    GedcomDiagnosticSeverity.Error,
                    "GEDCOM-linjen har ugyldig syntaks.",
                    index + 1,
                    currentRecordId,
                    null,
                    "Kun det ugyldige underfelt blev sprunget over.",
                    filePath));
                continue;
            }

            output.Add(line);
        }

        return new PreparedGedcom(string.Join('\n', output), diagnostics, skippedRecords);
    }

    private static GedcomLoadException CreateFatalStructureException(
        string filePath,
        int line,
        string tag,
        string message)
    {
        var diagnostic = new GedcomDiagnostic(
            GedcomDiagnosticSeverity.Fatal,
            message,
            line,
            null,
            tag,
            "Hele importen blev afbrudt uden ændringer.",
            filePath);
        return new GedcomLoadException(
            message,
            new GedcomImportReport(0, 0, 0, 1, [diagnostic]));
    }

    private static string? ReadLevelZeroTag(string line)
    {
        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length switch
        {
            >= 3 when fields[1].StartsWith('@') => fields[2],
            >= 2 => fields[1],
            _ => null,
        };
    }

    private static bool RequiresRecordId(string? tag)
    {
        return tag is "INDI" or "FAM" or "SOUR" or "OBJE" or "SUBM";
    }

    private static bool IsSupportedLevelZeroRecord(string? tag)
    {
        return tag is "HEAD" or "TRLR" or "INDI" or "FAM" or "SOUR" or "OBJE" or "SUBM";
    }

    private static bool TryReadRecordHeader(string line, out string? recordId)
    {
        recordId = null;
        var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 3
            || fields[0] != "0"
            || !fields[1].StartsWith('@')
            || !fields[1].EndsWith('@')
            || fields[1].Length < 3)
        {
            return false;
        }

        recordId = fields[1];
        return true;
    }

    private static string RecordIdErrorMessage(string? tag)
    {
        return tag switch
        {
            "INDI" => "Personposten mangler et gyldigt record-id.",
            "FAM" => "Familieposten mangler et gyldigt record-id.",
            "SOUR" => "Kildeposten mangler et gyldigt record-id.",
            "OBJE" => "Medieposten mangler et gyldigt record-id.",
            "SUBM" => "Afsenderposten mangler et gyldigt record-id.",
            _ => "GEDCOM-posten mangler et gyldigt record-id.",
        };
    }

    private static string RecordSkipConsequence(string? tag)
    {
        return tag switch
        {
            "INDI" => "Personposten blev sprunget over; øvrige poster blev bevaret.",
            "FAM" => "Familieposten blev sprunget over; øvrige poster blev bevaret.",
            "SOUR" => "Kildeposten blev sprunget over; øvrige poster blev bevaret.",
            "OBJE" => "Medieposten blev sprunget over; øvrige poster blev bevaret.",
            "SUBM" => "Afsenderposten blev sprunget over; øvrige poster blev bevaret.",
            _ => "Posten blev sprunget over; øvrige poster blev bevaret.",
        };
    }

    private static bool IsSyntacticallyValidGedcomLine(string line)
    {
        var firstSpace = line.IndexOf(' ');
        if (firstSpace <= 0 || !int.TryParse(line[..firstSpace], out _))
        {
            return false;
        }

        var remainder = line[(firstSpace + 1)..];
        return remainder.Length > 0 && !char.IsWhiteSpace(remainder[0]);
    }

    private static DecodedGedcom ReadGedcomFile(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        if (bytes.Length == 0)
        {
            throw new GedcomLoadException("GEDCOM-filen er tom.");
        }

        var encodingKind = DetectPhysicalEncoding(bytes);
        var preliminaryText = DecodeForHeader(bytes, encodingKind);
        var characterSet = FindHeaderCharacterSet(preliminaryText);
        var diagnostics = new List<GedcomDiagnostic>();

        if (characterSet is null)
        {
            diagnostics.Add(new GedcomDiagnostic(
                GedcomDiagnosticSeverity.Warning,
                "GEDCOM-headeren mangler det obligatoriske CHAR-felt; filen er fortolket som UTF-8.",
                Consequence: "Importen fortsatte med streng UTF-8-afkodning; resultatet kræver gennemgang."));
            characterSet = "UTF-8";
        }

        ValidateEncodingAgreement(encodingKind, characterSet);

        string text;
        try
        {
            text = characterSet switch
            {
                "UTF-8" or "UTF8" => new UTF8Encoding(false, true).GetString(bytes, encodingKind.PreambleLength, bytes.Length - encodingKind.PreambleLength),
                "UNICODE" => DecodeUnicode(bytes, encodingKind),
                "ASCII" => DecodeAscii(bytes, encodingKind.PreambleLength),
                "ANSEL" => DecodeAnsel(bytes, encodingKind.PreambleLength),
                _ => throw new GedcomLoadException($"GEDCOM-tegnsættet '{characterSet}' understøttes ikke."),
            };
        }
        catch (DecoderFallbackException exception)
        {
            throw new GedcomLoadException(
                $"GEDCOM-filen indeholder ugyldige byteværdier for tegnsættet '{characterSet}'; importen er afbrudt uden tegnkorruption.",
                exception);
        }

        return new DecodedGedcom(text.TrimStart('\uFEFF'), diagnostics);
    }

    private static PhysicalEncoding DetectPhysicalEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new PhysicalEncoding("UTF-8", 3);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return new PhysicalEncoding("UTF-16LE", 2);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return new PhysicalEncoding("UTF-16BE", 2);
        }

        if (bytes.Length >= 4 && bytes[1] == 0 && bytes[3] == 0)
        {
            return new PhysicalEncoding("UTF-16LE", 0);
        }

        if (bytes.Length >= 4 && bytes[0] == 0 && bytes[2] == 0)
        {
            return new PhysicalEncoding("UTF-16BE", 0);
        }

        return new PhysicalEncoding("8-BIT", 0);
    }

    private static string DecodeForHeader(byte[] bytes, PhysicalEncoding encodingKind)
    {
        if (encodingKind.Name == "UTF-16LE")
        {
            return new UnicodeEncoding(false, false, true)
                .GetString(bytes, encodingKind.PreambleLength, bytes.Length - encodingKind.PreambleLength);
        }

        if (encodingKind.Name == "UTF-16BE")
        {
            return new UnicodeEncoding(true, false, true)
                .GetString(bytes, encodingKind.PreambleLength, bytes.Length - encodingKind.PreambleLength);
        }

        var chars = bytes
            .Skip(encodingKind.PreambleLength)
            .Select(value => value <= 0x7F ? (char)value : '?')
            .ToArray();
        return new string(chars);
    }

    private static string? FindHeaderCharacterSet(string text)
    {
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            if (line.StartsWith("1 CHAR ", StringComparison.Ordinal))
            {
                return line[7..].Trim().ToUpperInvariant();
            }
        }

        return null;
    }

    private static void ValidateEncodingAgreement(PhysicalEncoding encodingKind, string characterSet)
    {
        var declaresUnicode = characterSet == "UNICODE";
        var isPhysicalUnicode = encodingKind.Name is "UTF-16LE" or "UTF-16BE";
        if (declaresUnicode != isPhysicalUnicode)
        {
            throw new GedcomLoadException(
                $"GEDCOM-headerens CHAR '{characterSet}' er modstridende med filens fysiske encoding '{encodingKind.Name}'.");
        }

        if (encodingKind.Name == "UTF-8" && characterSet is not ("UTF-8" or "UTF8"))
        {
            throw new GedcomLoadException(
                $"GEDCOM-headerens CHAR '{characterSet}' er modstridende med filens UTF-8-BOM.");
        }
    }

    private static string DecodeUnicode(byte[] bytes, PhysicalEncoding encodingKind)
    {
        var bigEndian = encodingKind.Name == "UTF-16BE";
        return new UnicodeEncoding(bigEndian, false, true)
            .GetString(bytes, encodingKind.PreambleLength, bytes.Length - encodingKind.PreambleLength);
    }

    private static string DecodeAscii(byte[] bytes, int offset)
    {
        if (bytes.Skip(offset).Any(value => value > 0x7F))
        {
            throw new GedcomLoadException(
                "GEDCOM-filen erklærer ASCII, men indeholder byteværdier uden for ASCII; importen er afbrudt uden tegnkorruption.");
        }

        return Encoding.ASCII.GetString(bytes, offset, bytes.Length - offset);
    }

    private static string DecodeAnsel(byte[] bytes, int offset)
    {
        var builder = new StringBuilder(bytes.Length - offset);
        var combiningMarks = new List<char>();

        for (var index = offset; index < bytes.Length; index++)
        {
            var value = bytes[index];
            if (TryMapAnselCombining(value, out var combiningMark))
            {
                combiningMarks.Add(combiningMark);
                continue;
            }

            var character = value <= 0x7F
                ? (char)value
                : MapAnselSpacing(value);
            builder.Append(character);
            foreach (var mark in combiningMarks)
            {
                builder.Append(mark);
            }

            combiningMarks.Clear();
        }

        if (combiningMarks.Count > 0)
        {
            throw new GedcomLoadException("GEDCOM-filen slutter med et ANSEL-diacritikum uden grundtegn.");
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool TryMapAnselCombining(byte value, out char character)
    {
        character = value switch
        {
            0xE1 => '\u0300',
            0xE2 => '\u0301',
            0xE3 => '\u0302',
            0xE4 => '\u0303',
            0xE5 => '\u0304',
            0xE6 => '\u0306',
            0xE7 => '\u0307',
            0xE8 => '\u0308',
            0xE9 => '\u030C',
            0xEA => '\u030A',
            0xED => '\u0315',
            0xEE => '\u030B',
            0xF0 => '\u0327',
            0xF1 => '\u0328',
            0xF6 => '\u0332',
            0xFE => '\u0313',
            _ => '\0',
        };
        return character != '\0';
    }

    private static char MapAnselSpacing(byte value)
    {
        return value switch
        {
            0xA1 => 'Ł', 0xA2 => 'Ø', 0xA3 => 'Đ', 0xA4 => 'Þ', 0xA5 => 'Æ', 0xA6 => 'Œ',
            0xA8 => '·', 0xA9 => '♭', 0xAA => '®', 0xAB => '±', 0xAE => 'ʻ', 0xB0 => 'ʿ',
            0xB1 => 'ł', 0xB2 => 'ø', 0xB3 => 'đ', 0xB4 => 'þ', 0xB5 => 'æ', 0xB6 => 'œ',
            0xB8 => 'ı', 0xB9 => '£', 0xBA => 'ð', 0xC3 => '©', 0xC5 => '¿', 0xC6 => '¡',
            _ => throw new GedcomLoadException(
                $"GEDCOM-filen indeholder den uunderstøttede ANSEL-byte 0x{value:X2}; importen er afbrudt uden tegnkorruption."),
        };
    }

    private static IReadOnlyDictionary<string, string> ReadRawPersonSegments(string text)
    {
        var segments = new Dictionary<string, string>(StringComparer.Ordinal);
        string? currentRecordId = null;
        var currentStart = -1;

        for (var lineStart = 0; lineStart < text.Length;)
        {
            var lineEnd = text.IndexOfAny(['\r', '\n'], lineStart);
            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            var line = text[lineStart..lineEnd];
            if (TryReadIndividualHeader(line, out var recordId))
            {
                if (currentRecordId is not null)
                {
                    segments[currentRecordId] = text[currentStart..lineStart];
                }

                currentRecordId = recordId;
                currentStart = lineStart;
            }
            else if (currentRecordId is not null && line.StartsWith("0 ", StringComparison.Ordinal))
            {
                segments[currentRecordId] = text[currentStart..lineStart];
                currentRecordId = null;
                currentStart = -1;
            }

            if (lineEnd == text.Length)
            {
                lineStart = text.Length;
            }
            else if (text[lineEnd] == '\r' && lineEnd + 1 < text.Length && text[lineEnd + 1] == '\n')
            {
                lineStart = lineEnd + 2;
            }
            else
            {
                lineStart = lineEnd + 1;
            }
        }

        if (currentRecordId is not null)
        {
            segments[currentRecordId] = text[currentStart..];
        }

        return segments;
    }

    private static string CollapseContinuationLines(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var output = new List<string>();
        var targetByLevel = new Dictionary<int, int>();

        foreach (var line in normalized.Split('\n'))
        {
            if (TryReadContinuation(line, out var level, out var tag, out var value)
                && targetByLevel.TryGetValue(level - 1, out var target))
            {
                output[target] += tag == "CONT" ? ContinuedLineBreak + value : value;
                targetByLevel[level] = target;
                continue;
            }

            output.Add(line);
            if (TryReadLevel(line, out level))
            {
                targetByLevel[level] = output.Count - 1;
            }
        }

        return string.Join('\n', output);
    }

    private static bool TryReadContinuation(
        string line,
        out int level,
        out string tag,
        out string value)
    {
        level = -1;
        tag = string.Empty;
        value = string.Empty;
        var firstSpace = line.IndexOf(' ');
        if (firstSpace <= 0 || !int.TryParse(line[..firstSpace], out level))
        {
            return false;
        }

        var remainder = line[(firstSpace + 1)..];
        var tagEnd = remainder.IndexOf(' ');
        tag = tagEnd < 0 ? remainder : remainder[..tagEnd];
        if (tag is not ("CONT" or "CONC"))
        {
            return false;
        }

        value = tagEnd < 0 ? string.Empty : remainder[(tagEnd + 1)..];
        return true;
    }

    private static bool TryReadLevel(string line, out int level)
    {
        level = -1;
        var firstSpace = line.IndexOf(' ');
        return firstSpace > 0 && int.TryParse(line[..firstSpace], out level);
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

    private static ParsedSource CreateCitation(string? value)
    {
        var normalized = NormalizeToken(value);
        if (normalized is not null
            && normalized.StartsWith('@')
            && normalized.EndsWith('@'))
        {
            return new ParsedSource(normalized);
        }

        return new ParsedSource(null)
        {
            Title = normalized,
        };
    }

    private static string? NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Replace(ContinuedLineBreak.ToString(), "\n", StringComparison.Ordinal);
    }

    private sealed record DecodedGedcom(
        string Text,
        IReadOnlyCollection<GedcomDiagnostic> Diagnostics);

    private sealed record PhysicalEncoding(string Name, int PreambleLength);

    private sealed record PreparedGedcom(
        string Text,
        IReadOnlyCollection<GedcomDiagnostic> Diagnostics,
        int SkippedRecords);

    private sealed record ParsedGedcom(
        IReadOnlyCollection<ParsedPerson> People,
        IReadOnlyCollection<ParsedFamily> Families,
        IReadOnlyCollection<ParsedSource> Sources,
        IReadOnlyCollection<ParsedMedia> Media,
        IReadOnlyCollection<ParsedSubmitter> Submitters,
        string? HeaderSubmitterId,
        IReadOnlyCollection<GedcomDiagnostic> Diagnostics,
        int SkippedRecords);

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

        public IList<string> Notes { get; } = new List<string>();

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

        public IList<ParsedSource> Sources { get; } = new List<ParsedSource>();

        public IList<string> Notes { get; } = new List<string>();

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

        public string? Note { get; set; }
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
