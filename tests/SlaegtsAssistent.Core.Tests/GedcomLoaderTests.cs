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
    public void Load_MalformedGedcom_ThrowsGedcomLoadExceptionWithMessage()
    {
        var action = () => _loader.Load(FixturePath("malformed.ged"));

        var exception = action.Should().Throw<GedcomLoadException>().Which;
        exception.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Load_MissingFile_ThrowsGedcomLoadException()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        var action = () => _loader.Load(missingPath);

        action.Should().Throw<GedcomLoadException>()
            .WithMessage("GEDCOM file was not found:*");
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
}
