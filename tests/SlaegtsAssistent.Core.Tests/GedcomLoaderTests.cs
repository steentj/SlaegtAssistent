using FluentAssertions;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public class GedcomLoaderTests
{
    private readonly IGedcomLoader _loader = new GedcomLoader();

    [Fact]
    public void Load_SinglePersonFixture_MapsFieldsIncludingRecordId()
    {
        var tree = _loader.Load(FixturePath("single-person.ged"));

        tree.People.Should().ContainSingle();
        var person = tree.People.Single();

        person.RecordId.Should().Be("@I1@");
        person.FullName.Should().Be("Anna Jensen");
        person.Sex.Should().Be("F");
        person.BirthDate.Should().Be("12 MAR 1900");
        person.BirthPlace.Should().Be("Aarhus, Denmark");
    }

    [Fact]
    public void Load_SinglePersonFixture_PreservesRawGedcomSegment()
    {
        var tree = _loader.Load(FixturePath("single-person.ged"));

        var rawGedcom = tree.FindPerson("@I1@")!.RawGedcom;

        rawGedcom.Should().Contain("0 @I1@ INDI");
        rawGedcom.Should().Contain("1 NAME Anna /Jensen/");
        rawGedcom.Should().NotContain("0 TRLR");
    }

    [Fact]
    public void Load_TwoGenerationsFixture_ResolvesParentChildRelationships()
    {
        var tree = _loader.Load(FixturePath("two-generations.ged"));

        var parent = tree.FindPerson("@I1@");
        var child = tree.FindPerson("@I2@");

        parent.Should().NotBeNull();
        child.Should().NotBeNull();

        child!.Parents.Should().ContainSingle(p => p.RecordId == "@I1@");
        parent!.Children.Should().ContainSingle(c => c.RecordId == "@I2@");
    }

    [Fact]
    public void Load_SinglePersonWithoutDeathData_SetsDeathFieldsToNull()
    {
        var tree = _loader.Load(FixturePath("single-person.ged"));
        var person = tree.FindPerson("@I1@");

        person.Should().NotBeNull();
        person!.DeathDate.Should().BeNull();
        person.DeathPlace.Should().BeNull();
    }

    [Fact]
    public void Load_SourcesAndMediaFixture_MapsRecordsAndPersonReferences()
    {
        var tree = _loader.Load(FixturePath("sources-and-media.ged"));

        var source = tree.FindSource("@S1@");
        var media = tree.FindMedia("@M1@");
        var person = tree.FindPerson("@I1@");

        source.Should().NotBeNull();
        source!.Title.Should().Be("Kirkebog for Aarhus");
        source.Author.Should().Be("Aarhus Sogn");
        source.Publication.Should().Be("Aarhus Arkiv, 1900");
        source.Text.Should().Be("Original registrering");
        source.Repository.Should().Be("@R1@");
        source.Data.Should().Be("Fødsler");
        source.Date.Should().Be("1900");

        media.Should().NotBeNull();
        media!.File.Should().Be("scans/anna-birth.jpg");
        media.Form.Should().Be("jpeg");
        media.Title.Should().Be("Fødselsregistrering");
        media.Type.Should().Be("Foto");
        media.Note.Should().Be("Scannet original");

        person.Should().NotBeNull();
        person!.Sources.Should().ContainSingle();
        person.Sources[0].RecordId.Should().Be("@S1@");
        person.Sources[0].Title.Should().Be("Kirkebog for Aarhus");
        person.Sources[0].Page.Should().Be("42");
        person.Sources[0].Data.Should().Be("Personregistrering");
        person.Sources[0].Date.Should().Be("12 MAR 1900");
        person.Sources[0].Text.Should().Be("Notat fra kildehenvisningen");
        person.Media.Should().ContainSingle();
        person.Media[0].RecordId.Should().Be("@M1@");
        person.Media[0].File.Should().Be("scans/anna-birth.jpg");
    }

    [Fact]
    public void Load_SourcesAndMediaFixture_MapsInlinePersonData()
    {
        var fixturePath = CreateTemporaryFixture(
            """
            0 HEAD
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            1 SOUR
            2 TITL Lokal kilde
            2 PAGE Side 3
            1 OBJE
            2 FILE lokal.jpg
            2 FORM jpeg
            0 TRLR
            """);

        try
        {
            var person = _loader.Load(fixturePath).FindPerson("@I1@");

            person.Should().NotBeNull();
            person!.Sources.Should().ContainSingle();
            person.Sources[0].Title.Should().Be("Lokal kilde");
            person.Sources[0].Page.Should().Be("Side 3");
            person.Media.Should().ContainSingle();
            person.Media[0].File.Should().Be("lokal.jpg");
            person.Media[0].Form.Should().Be("jpeg");
        }
        finally
        {
            File.Delete(fixturePath);
        }
    }

    [Fact]
    public void Load_EventsAndCensusFixture_MapsEventsAndCensusFields()
    {
        var tree = _loader.Load(FixturePath("events-and-census.ged"));
        var person = tree.FindPerson("@I1@");

        person.Should().NotBeNull();
        person!.Events.Should().HaveCount(4);
        person.Events.Select(genericEvent => genericEvent.Tag)
            .Should()
            .Equal("BIRT", "BAPM", "EVEN", "BURI");

        var baptism = person.Events.Single(genericEvent => genericEvent.Tag == "BAPM");
        baptism.Date.Should().Be("20 APR 1900");
        baptism.Place.Should().Be("Aarhus Domkirke");
        baptism.Type.Should().Be("Dåb");
        baptism.Note.Should().Be("Dåb registreret i kirkebogen");
        baptism.Sources.Should().ContainSingle(source => source.RecordId == "@S1@");

        var move = person.Events.Single(genericEvent => genericEvent.Tag == "EVEN");
        move.Value.Should().Be("Flytning");
        move.Date.Should().Be("01 MAY 1920");
        move.Place.Should().Be("København");
        move.Type.Should().Be("Bopæl");
        move.Note.Should().Be("Flyttede til København");

        var census = person.Census.Should().ContainSingle().Subject;
        census.Date.Should().Be("01 FEB 1911");
        census.Place.Should().Be("Aarhus");
        census.Note.Should().Be("Registreret i folketællingen");
        census.Sources.Should().ContainSingle(source => source.RecordId == "@S1@");
    }

    [Fact]
    public void Load_EventsAndCensusFixture_ContinuesMappingLegacyBirthAndDeathFields()
    {
        var fixturePath = CreateTemporaryFixture(
            """
            0 HEAD
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            1 BIRT
            2 DATE 12 MAR 1900
            2 PLAC Aarhus
            1 DEAT
            2 DATE 03 JAN 1980
            2 PLAC Aarhus
            0 TRLR
            """);

        try
        {
            var person = _loader.Load(fixturePath).FindPerson("@I1@");

            person.Should().NotBeNull();
            person!.BirthDate.Should().Be("12 MAR 1900");
            person.BirthPlace.Should().Be("Aarhus");
            person.DeathDate.Should().Be("03 JAN 1980");
            person.DeathPlace.Should().Be("Aarhus");
            person.Events.Select(genericEvent => genericEvent.Tag)
                .Should()
                .Equal("BIRT", "DEAT");
        }
        finally
        {
            File.Delete(fixturePath);
        }
    }

    [Fact]
    public void Load_WhenReimportingWithExistingTree_MergesByRecordIdInsteadOfDuplicating()
    {
        var existingTree = _loader.Load(FixturePath("two-generations.ged"));
        var mergedTree = _loader.Load(FixturePath("two-generations-updated.ged"), existingTree);

        mergedTree.Should().BeSameAs(existingTree);
        mergedTree.People.Should().HaveCount(3);
        mergedTree.People.Count(person => person.RecordId == "@I2@").Should().Be(1);

        var updatedChild = mergedTree.FindPerson("@I2@");
        var newChild = mergedTree.FindPerson("@I3@");
        var parent = mergedTree.FindPerson("@I1@");

        updatedChild.Should().NotBeNull();
        updatedChild!.BirthPlace.Should().Be("Aalborg, Denmark");
        newChild.Should().NotBeNull();
        parent.Should().NotBeNull();
        parent!.Children.Select(child => child.RecordId).Should().BeEquivalentTo("@I2@", "@I3@");
    }

    [Fact]
    public void Load_MalformedPersonRecord_SkipsRecordAndReportsPartialImport()
    {
        var tree = _loader.Load(FixturePath("malformed.ged"));

        tree.People.Should().BeEmpty();
        tree.ImportReport.SkippedRecords.Should().Be(1);
        tree.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Tag == "INDI");
    }

    [Fact]
    public void Load_MissingFile_ThrowsGedcomLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        var action = () => _loader.Load(missingPath);

        action.Should().Throw<GedcomLoadException>()
            .WithMessage("GEDCOM-filen blev ikke fundet:*");
    }

    private static string FixturePath(string fileName)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            ".."));

        return Path.Combine(projectDirectory, "Fixtures", "Gedcom", fileName);
    }

    private static string CreateTemporaryFixture(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, content);
        return path;
    }
}
