using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyConflictResolutionTests
{
    [Fact]
    public void Compare_ShouldReturnStableScalarDifferenceWithThreeValues()
    {
        var approved = Snapshot(person => person with { BirthDate = "1900" });
        var imported = Snapshot(person => person with { BirthDate = "1901" });
        var state = new BiographyReconciliationState(
            BiographyBaselineStatus.Changed,
            imported,
            approved,
            new BiographyFactsSnapshot("Anna", "F", "1902", null, null, null, []));

        var difference = new BiographyStructuredDifferenceService().Compare(state).Single();

        difference.Path.Should().Be("person.birthDate");
        difference.DocumentValue.Should().Be("1902");
        difference.ApprovedValue.Should().Be("1900");
        difference.ImportedValue.Should().Be("1901");
        difference.Kind.Should().Be(BiographyDifferenceKind.Changed);
        difference.Causes.Should().Be(BiographyDifferenceCause.Gedcom);
    }

    [Fact]
    public void Compare_ShouldClassifyAddedChangedAndRemovedRepeatedValues()
    {
        var approved = Snapshot(person => person with
        {
            Events = [Event("før"), Event("fjernes")],
            Sources = [Source("@S1@", "Gammel")],
            Media = [Media("@M1@", "gammel.jpg"), Media("@M2@", "fjernes.jpg")],
        });
        var imported = Snapshot(person => person with
        {
            Events = [Event("efter"), Event("fjernes"), Event("tilføjet")],
            Sources = [Source("@S1@", "Ny"), Source("@S2@", "Tilføjet")],
            Media = [Media("@M1@", "ny.jpg")],
        });
        var state = State(approved, imported);

        var differences = new BiographyStructuredDifferenceService().Compare(state);

        differences.Should().Contain(item => item.Path == "person.events[før]" && item.Kind == BiographyDifferenceKind.Changed);
        differences.Should().Contain(item => item.Path == "person.events[tilføjet]" && item.Kind == BiographyDifferenceKind.Added);
        differences.Should().Contain(item => item.Path == "person.sources[@S1@]" && item.Kind == BiographyDifferenceKind.Changed);
        differences.Should().Contain(item => item.Path == "person.sources[@S2@]" && item.Kind == BiographyDifferenceKind.Added);
        differences.Should().Contain(item => item.Path == "person.media[@M1@]" && item.Kind == BiographyDifferenceKind.Changed);
        differences.Should().Contain(item => item.Path == "person.media[@M2@]" && item.Kind == BiographyDifferenceKind.Removed);
        differences.Select(item => item.Path).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public void GentagneFeltidentiteterKanVaelgesUafhaengigt()
    {
        var previousEvent = Event("Oprindelig");
        var newEvent = Event("Ny");
        var previousCensus = new CanonicalCensusData("odense", null, "Odense", null, []);
        var newCensus = new CanonicalCensusData("aarhus", null, "Aarhus", null, []);
        var previousMedia = Media("@M1@", "gammel.jpg");
        var newMedia = Media("@M1@", "ny.jpg");
        var approved = Snapshot(person => person with
        {
            Events = [previousEvent],
            Census = [previousCensus],
            Media = [previousMedia],
        });
        var imported = Snapshot(person => person with
        {
            Events = [newEvent, previousEvent],
            Census = [newCensus, previousCensus],
            Media = [previousMedia, newMedia],
        });

        var differences = new BiographyStructuredDifferenceService().Compare(State(approved, imported));

        differences.Select(item => item.Path).Should().HaveCount(5).And.OnlyHaveUniqueItems();
        var choices = differences.ToDictionary(
            item => item.Path,
            item => item.Kind == BiographyDifferenceKind.Changed ||
                item.Path.StartsWith("person.media[", StringComparison.Ordinal),
            StringComparer.Ordinal);
        var selected = new BiographySnapshotDecisionService().Apply(approved, imported, choices);

        selected.Person.Events.Should().Equal(newEvent);
        selected.Person.Census.Should().Equal(newCensus);
        selected.Person.Media.Should().Equal(previousMedia, newMedia);
    }

    [Fact]
    public void Apply_ShouldCombineIndependentChoicesWithoutChangingInputs()
    {
        var approved = Snapshot(person => person with
        {
            BirthDate = "1900",
            BirthPlace = "Odense",
            Events = [Event("før")],
            Media = [Media("@M1@", "før.jpg")],
        });
        var imported = Snapshot(person => person with
        {
            BirthDate = "1901",
            BirthPlace = "Aarhus",
            Events = [Event("efter")],
            Media = [Media("@M1@", "efter.jpg")],
        });

        var selected = new BiographySnapshotDecisionService().Apply(
            approved,
            imported,
            new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["person.birthDate"] = true,
                ["person.birthPlace"] = false,
                ["person.events[før]"] = true,
                ["person.media[@M1@]"] = false,
            });

        selected.Person.BirthDate.Should().Be("1901");
        selected.Person.BirthPlace.Should().Be("Odense");
        selected.Person.Events[0].Value.Should().Be("efter");
        selected.Person.Media[0].File.Should().Be("før.jpg");
        approved.Person.BirthDate.Should().Be("1900");
        imported.Person.BirthPlace.Should().Be("Aarhus");
    }

    [Theory]
    [InlineData(true, "Nyt notat")]
    [InlineData(false, "Godkendt notat")]
    public void Notes_ShouldRespectReviewChoiceWhenRendering(bool useImported, string expected)
    {
        var approved = Snapshot(person => person with { Notes = ["Godkendt notat"] });
        var imported = Snapshot(person => person with { Notes = ["Nyt notat"] });
        var differences = new BiographyStructuredDifferenceService().Compare(State(approved, imported));
        differences.Should().ContainSingle(item =>
            item.Path == "person.notes[0]" && item.Label == "Notat"
            && item.Kind == BiographyDifferenceKind.Changed);

        var selected = new BiographySnapshotDecisionService().Apply(
            approved, imported, new Dictionary<string, bool> { ["person.notes[0]"] = useImported });
        var body = BiographyDocumentParser.Parse(
            new BiographyTemplateMarkdownGenerator().Generate(selected, imported)).Body;

        body.Should().Contain($"Person: {expected}");
        selected.Person.Notes.Should().Equal(expected);
    }

    [Theory]
    [InlineData(true, "Nyt familienotat")]
    [InlineData(false, "Godkendt familienotat")]
    public void ChildhoodFamilyNotes_ShouldRespectReviewChoice(bool useImported, string expected)
    {
        var approved = Snapshot(person => person with
        {
            ChildhoodFamilyNotes = [new("@F1@", ["Godkendt familienotat"])],
        });
        var imported = Snapshot(person => person with
        {
            ChildhoodFamilyNotes = [new("@F1@", ["Nyt familienotat"])],
        });
        var differences = new BiographyStructuredDifferenceService().Compare(State(approved, imported));
        differences.Should().ContainSingle(item =>
            item.Path == "person.childhoodFamilyNotes[@F1@]" && item.Label == "Familienotat"
            && item.Kind == BiographyDifferenceKind.Changed);

        var selected = new BiographySnapshotDecisionService().Apply(
            approved, imported,
            new Dictionary<string, bool> { ["person.childhoodFamilyNotes[@F1@]"] = useImported });

        selected.Person.ChildhoodFamilyNotes.Should().ContainSingle()
            .Which.Notes.Should().Equal(expected);
    }

    [Fact]
    public void Apply_ShouldHonorAddChangeAndRemoveChoicesForCollections()
    {
        var approved = Snapshot(person => person with
        {
            Sources = [Source("@S1@", "Bevares"), Source("@S2@", "Fjernes")],
            Media = [Media("@M1@", "gammel.jpg")],
        });
        var imported = Snapshot(person => person with
        {
            Sources = [Source("@S1@", "Ændret"), Source("@S3@", "Tilføjet")],
            Media = [Media("@M1@", "ny.jpg")],
        });

        var selected = new BiographySnapshotDecisionService().Apply(
            approved,
            imported,
            new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["person.sources[@S1@]"] = false,
                ["person.sources[@S2@]"] = true,
                ["person.sources[@S3@]"] = true,
                ["person.media[@M1@]"] = true,
            });

        selected.Person.Sources.Select(item => (item.RecordId, item.Title))
            .Should().Equal(("@S1@", "Bevares"), ("@S3@", "Tilføjet"));
        selected.Person.Media.Should().ContainSingle().Which.File.Should().Be("ny.jpg");
    }

    [Fact]
    public void Compare_ShouldExposeTemplateAndMigrationReasonsSeparately()
    {
        var snapshot = Snapshot(person => person);
        var state = new BiographyReconciliationState(
            BiographyBaselineStatus.Missing,
            snapshot,
            null,
            new BiographyFactsSnapshot("Anna", null, null, null, null, null, []));

        var differences = new BiographyStructuredDifferenceService().Compare(
            state,
            templateChanged: true,
            requiresMigration: true);

        differences.Should().ContainSingle(item =>
            item.Path == "migration.markers" &&
            item.Causes.HasFlag(BiographyDifferenceCause.BaselineMigration) &&
            item.Causes.HasFlag(BiographyDifferenceCause.Template));
    }

    [Fact]
    public void Candidate_ShouldRenderSelectedSnapshotAndPreserveFreeTextByteForByte()
    {
        var imported = Snapshot(person => person with { BirthDate = "1901", BirthPlace = "Aarhus" });
        var selected = Snapshot(person => person with { BirthDate = "1901", BirthPlace = "Odense" });
        var generator = new BiographyTemplateMarkdownGenerator();
        var original = BiographyDocumentParser.Parse(generator.Generate(
            Snapshot(person => person with { BirthDate = "1900", BirthPlace = "Odense" }),
            Snapshot(person => person with { BirthDate = "1900", BirthPlace = "Odense" })));
        var freeText = "\r\n## Biografi\r\nBrugerens  tekst  med  mellemrum.\r\n";
        var originalWithFreeText = original with
        {
            Body = original.Body[..(original.Body.IndexOf(BiographyGeneratedSectionMerger.EndMarker, StringComparison.Ordinal)
                + BiographyGeneratedSectionMerger.EndMarker.Length)] + freeText,
        };

        var candidate = BiographyDocumentParser.Parse(
            BiographyConflictCandidateService.MergeWithExistingDocument(
                originalWithFreeText,
                generator.Generate(selected, imported)));

        candidate.Body.Should().Contain("1901");
        candidate.Body.Should().Contain("Odense");
        candidate.Body.Should().NotContain("Aarhus");
        candidate.Body.Should().EndWith(freeText);
        candidate.Metadata!.SyncBaseline!.Imported.Should().BeEquivalentTo(imported);
        candidate.Metadata.SyncBaseline.Approved.Should().BeEquivalentTo(selected);
    }

    private static BiographyReconciliationState State(
        CanonicalBiographySnapshot approved,
        CanonicalBiographySnapshot imported)
    {
        return new BiographyReconciliationState(
            BiographyBaselineStatus.Changed,
            imported,
            approved,
            new BiographyFactsSnapshot("Anna", "F", approved.Person.BirthDate, approved.Person.BirthPlace, null, null, []));
    }

    private static CanonicalBiographySnapshot Snapshot(
        Func<CanonicalPersonData, CanonicalPersonData> update)
    {
        var person = new CanonicalPersonData(
            "@I1@", "Anna", "F", null, null, null, null, [], [], [], [], []);
        return new CanonicalBiographySnapshot(1, update(person), [], [], [], null);
    }

    private static CanonicalEventData Event(string value) =>
        new(value, "EVEN", GedcomEventCategory.Other.ToString(), value, null, null, null, null, []);

    private static CanonicalSourceData Source(string id, string title) =>
        new(id, id, title, null, null, null, null, null, null, null, null);

    private static CanonicalMediaData Media(string id, string file) =>
        new(id, id, file, null, null, null, null);
}
