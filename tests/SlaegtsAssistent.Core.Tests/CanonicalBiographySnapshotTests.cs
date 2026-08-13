using System.Text.Json;
using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class CanonicalBiographySnapshotTests
{
    public static TheoryData<string, Action<FamilyTree>> StructuredMutations => new()
    {
        { "personfelt", tree => tree.FindPerson("@I1@")!.Sex = "U" },
        { "relation", tree => tree.FindPerson("@I1@")!.Children.Add(new Person("@I99@")) },
        { "personhændelse", tree => tree.FindPerson("@I1@")!.Events[0].Note = "Ny hændelsesnote" },
        { "familiehændelse", tree => tree.FindFamily("@F1@")!.Events[0].Date = "11 JUN 1920" },
        { "census", tree => tree.FindPerson("@I1@")!.Census[0].Place = "Odense" },
        { "kildecitation", tree => tree.FindPerson("@I1@")!.Events[0].Sources[0].Page = "Side 43" },
        { "medie", tree => tree.FindPerson("@I1@")!.Media[0].Title = "Nyt portræt" },
        { "familiedata", tree => tree.FindFamily("@F1@")!.Notes[0] = "Ny familienote" },
        { "submitter", tree => tree.Submitter!.Address = "Ny adresse" },
    };

    [Fact]
    public void Create_IndeholderAlleUnderstoettedeStruktureredeDatatyper()
    {
        var tree = new GedcomLoader().Load(FixturePath("complete-gedcom-551.ged"));
        var snapshot = CanonicalBiographySnapshot.Create(tree.FindPerson("@I1@")!, tree.Submitter);
        var json = snapshot.ToCanonicalJson();

        snapshot.Version.Should().Be(CanonicalBiographySnapshot.CurrentVersion);
        json.Should().Contain("Anna Jensen");
        json.Should().Contain("@I2@");
        json.Should().Contain("@F1@");
        json.Should().Contain("BIRT");
        json.Should().Contain("\"census\"");
        json.Should().Contain("Side 42");
        json.Should().Contain("Kirkebog for Aarhus");
        json.Should().Contain("Familie oprettet fra kirkebogen");
        json.Should().Contain("portraet-anna.jpg");
        json.Should().Contain("Det lokale arkiv");
    }

    [Fact]
    public void Fingerprint_ErStabiltForIkkeBetydendeRelationsOgKilderaekkefoelge()
    {
        var first = CreatePersonWithRepeatedData(reverseSets: false);
        var second = CreatePersonWithRepeatedData(reverseSets: true);

        CanonicalBiographySnapshot.Create(first).ComputeFingerprint()
            .Should().Be(CanonicalBiographySnapshot.Create(second).ComputeFingerprint());
    }

    [Fact]
    public void Fingerprint_AendresNaarBetydendeHaendelsesraekkefoelgeAendres()
    {
        var first = CreatePersonWithRepeatedData(reverseSets: false);
        var second = CreatePersonWithRepeatedData(reverseSets: false);
        var firstEvent = second.Events[0];
        second.Events.RemoveAt(0);
        second.Events.Add(firstEvent);

        CanonicalBiographySnapshot.Create(first).ComputeFingerprint()
            .Should().NotBe(CanonicalBiographySnapshot.Create(second).ComputeFingerprint());
    }

    [Fact]
    public void Create_GiverGentagneElementerUnikkeStabileIdentiteter()
    {
        var person = new Person("@I1@");
        person.Events.Add(new GedcomEvent("EVEN") { Value = "Gentaget" });
        person.Events.Add(new GedcomEvent("EVEN") { Value = "Gentaget" });
        person.Sources.Add(new Source("@S1@") { Page = "1" });
        person.Sources.Add(new Source("@S1@") { Page = "1" });

        var first = CanonicalBiographySnapshot.Create(person);
        var second = CanonicalBiographySnapshot.Create(person);

        first.Person.Events.Select(item => item.Identity).Should().OnlyHaveUniqueItems();
        first.Person.Sources.Select(item => item.Identity).Should().OnlyHaveUniqueItems();
        second.Person.Events.Select(item => item.Identity)
            .Should().Equal(first.Person.Events.Select(item => item.Identity));
        second.Person.Sources.Select(item => item.Identity)
            .Should().Equal(first.Person.Sources.Select(item => item.Identity));
    }

    [Theory]
    [MemberData(nameof(StructuredMutations))]
    public void Fingerprint_AendresForAlleHovedtyper(
        string scenario,
        Action<FamilyTree> mutate)
    {
        var beforeTree = new GedcomLoader().Load(FixturePath("complete-gedcom-551.ged"));
        var afterTree = new GedcomLoader().Load(FixturePath("complete-gedcom-551.ged"));
        var before = CanonicalBiographySnapshot.Create(
            beforeTree.FindPerson("@I1@")!,
            beforeTree.Submitter);

        mutate(afterTree);
        var after = CanonicalBiographySnapshot.Create(
            afterTree.FindPerson("@I1@")!,
            afterTree.Submitter);

        after.ComputeFingerprint().Should().NotBe(
            before.ComputeFingerprint(),
            $"ændringer i {scenario} skal opdages");
    }

    [Fact]
    public void Fingerprint_NormalisererNullTomTekstLinjeskiftOgUnicode()
    {
        var first = new Person("@I1@")
        {
            FullName = "A\u030Ase\r\nJensen",
            Sex = null,
        };
        var second = new Person("@I1@")
        {
            FullName = "Åse\nJensen",
            Sex = "   ",
        };

        CanonicalBiographySnapshot.Create(first).ComputeFingerprint()
            .Should().Be(CanonicalBiographySnapshot.Create(second).ComputeFingerprint());
    }

    [Fact]
    public void SyncBaseline_RoundtripperVersionsstyretOgAdskiltFraDokumentfakta()
    {
        var person = CreatePersonWithRepeatedData(reverseSets: false);
        var imported = CanonicalBiographySnapshot.Create(person);
        person.FullName = "Godkendt navn";
        var approved = CanonicalBiographySnapshot.Create(person);
        var baseline = new BiographySyncBaseline(
            BiographySyncBaseline.CurrentVersion,
            imported,
            approved);
        var metadata = new BiographyDocumentMetadata(
            BiographyDocumentParser.CurrentFormatVersion,
            person.RecordId,
            "Dokumentnavn",
            BiographyFactsSnapshot.FromPerson(person) with { FullName = "Synligt dokumentnavn" })
        {
            GedcomBaselineHash = imported.ComputeFingerprint(),
            SyncBaseline = baseline,
        };

        var parsed = BiographyDocumentParser.Parse(
            BiographyDocumentSerializer.Serialize(metadata, "# Synligt dokumentnavn\n"));

        parsed.Metadata!.SyncBaseline.Should().BeEquivalentTo(baseline);
        parsed.Metadata.Facts.FullName.Should().Be("Synligt dokumentnavn");
        parsed.Metadata.SyncBaseline!.Imported.Person.FullName.Should().NotBe(
            parsed.Metadata.SyncBaseline.Approved.Person.FullName);
    }

    [Fact]
    public void Reconciliation_OpsporerSkjultFeltOgUkendtEllerManglendeBaseline()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        var approved = CanonicalBiographySnapshot.Create(person);
        person.Notes.Add("Skjult note, som skabelonen ikke renderer");
        var imported = CanonicalBiographySnapshot.Create(person);
        var documentFacts = BiographyFactsSnapshot.FromPerson(person);

        BiographyReconciliationState.Create(
                new BiographySyncBaseline(BiographySyncBaseline.CurrentVersion, imported, approved),
                imported,
                documentFacts)
            .Status.Should().Be(BiographyBaselineStatus.Changed);
        BiographyReconciliationState.Create(null, imported, documentFacts)
            .Status.Should().Be(BiographyBaselineStatus.Missing);
        BiographyReconciliationState.Create(
                new BiographySyncBaseline(99, imported, approved),
                imported,
                documentFacts)
            .Status.Should().Be(BiographyBaselineStatus.UnsupportedVersion);
    }

    [Fact]
    public void Reconciliation_UaendretGenimportErNoOp()
    {
        var person = CreatePersonWithRepeatedData(reverseSets: false);
        var snapshot = CanonicalBiographySnapshot.Create(person);
        var state = BiographyReconciliationState.Create(
            BiographySyncBaseline.CreateInitial(snapshot),
            CanonicalBiographySnapshot.Create(person),
            BiographyFactsSnapshot.FromPerson(person));

        state.Status.Should().Be(BiographyBaselineStatus.Unchanged);
        state.RequiresReview.Should().BeFalse();
    }

    [Fact]
    public void Parse_FormatToMedManglendeBaselineKraeverMigrationOgBevarerIndhold()
    {
        const string content =
            "---\n" +
            "formatVersion: 2\n" +
            "recordId: \"@I1@\"\n" +
            "displayName: \"Anna Jensen\"\n" +
            "gedcomBaselineHash: \"GAMMEL\"\n" +
            "templateHash: null\n" +
            "facts:\n" +
            "  fullName: \"Anna Jensen\"\n" +
            "  sex: null\n" +
            "  birthDate: null\n" +
            "  birthPlace: null\n" +
            "  deathDate: null\n" +
            "  deathPlace: null\n" +
            "  parentRecordIds: []\n" +
            "---\n" +
            "# Anna Jensen\n\nFri tekst.\n";

        var result = BiographyDocumentParser.ParseSafely(content);

        result.IsSuccess.Should().BeTrue();
        result.RequiresMigration.Should().BeTrue();
        result.Document!.Metadata!.SyncBaseline.Should().BeNull();
        result.MigrationCandidate.Should().Contain("syncBaseline: null");
        result.MigrationCandidate.Should().Contain("Fri tekst.");
    }

    [Fact]
    public void Parse_UkendtBaselineversionBevaresTilManuelGennemgang()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        var snapshot = CanonicalBiographySnapshot.Create(person);
        var metadata = new BiographyDocumentMetadata(
            BiographyDocumentParser.CurrentFormatVersion,
            person.RecordId,
            person.FullName,
            BiographyFactsSnapshot.FromPerson(person))
        {
            SyncBaseline = new BiographySyncBaseline(99, snapshot, snapshot),
        };

        var document = BiographyDocumentParser.Parse(
            BiographyDocumentSerializer.Serialize(metadata, "# Anna Jensen\n"));
        var state = BiographyReconciliationState.Create(
            document.Metadata!.SyncBaseline,
            snapshot,
            document.Metadata.Facts);

        state.Status.Should().Be(BiographyBaselineStatus.UnsupportedVersion);
        state.RequiresReview.Should().BeTrue();
    }

    private static Person CreatePersonWithRepeatedData(bool reverseSets)
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        var parentA = new Person("@I2@") { FullName = "Forælder A" };
        var parentB = new Person("@I3@") { FullName = "Forælder B" };
        var sourceA = new Source("@S1@") { Title = "Kilde A", Page = "1" };
        var sourceB = new Source("@S2@") { Title = "Kilde B", Page = "2" };
        foreach (var parent in reverseSets ? new[] { parentB, parentA } : new[] { parentA, parentB })
        {
            person.Parents.Add(parent);
        }

        foreach (var source in reverseSets ? new[] { sourceB, sourceA } : new[] { sourceA, sourceB })
        {
            person.Sources.Add(source);
        }

        person.Events.Add(new GedcomEvent("BIRT") { Date = "1900" });
        person.Events.Add(new GedcomEvent("OCCU") { Value = "Snedker" });
        person.Census.Add(new Census { Date = "1911", Place = "Aarhus" });
        person.Media.Add(new Media("@M1@") { File = "portræt.jpg", Note = "Forside" });
        return person;
    }

    private static string FixturePath(string fileName)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        return Path.Combine(projectDirectory, "Fixtures", "Gedcom", fileName);
    }
}
