using FluentAssertions;
using System.Globalization;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GedcomNoteTimestampTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public GedcomNoteTimestampTests() => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");

    public void Dispose() => CultureInfo.CurrentCulture = _originalCulture;

    [Theory]
    [InlineData("1 CHAN\n2 DATE 20 OCT 2023\n3 TIME 14:03:19\n1 CREA\n2 DATE 20 OCT 2023\n3 TIME 14:02:20", "20-10-2023 14:03")]
    [InlineData("1 CHAN\n2 DATE 19 OCT 2023\n1 CREA\n2 DATE 20 OCT 2023\n3 TIME 14:02:20", "20-10-2023 14:02")]
    [InlineData("1 CHAN\n2 DATE 20 OCT 2023", "20-10-2023")]
    [InlineData("1 CREA\n2 DATE 21 OCT 2023", "21-10-2023")]
    [InlineData("1 CHAN\n2 DATE UGYLDIG\n1 CREA\n2 DATE 21 OCT 2023", "21-10-2023")]
    [InlineData("", null)]
    public void DeltNotat_ViserSenesteGyldigeTidspunkt(string dates, string? expected)
    {
        var path = WriteFixture(
            "0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NOTE @N1@\n0 @N1@ NOTE Tekst\n" +
            dates + "\n0 TRLR\n");
        try
        {
            var tree = new GedcomLoader().Load(path);
            var person = tree.FindPerson("@I1@")!;
            var snapshot = CanonicalBiographySnapshot.Create(person);
            var generator = new BiographyTemplateMarkdownGenerator();
            var direct = BiographyDocumentParser.Parse(generator.Generate(person)).Body;
            var saved = BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot));
            var approved = saved.Metadata!.SyncBaseline!.Approved;
            var restored = BiographyDocumentParser.Parse(generator.Generate(approved, approved)).Body;

            person.Notes.Should().Equal("Tekst");
            tree.SharedNotes["@N1@"].Should().Be("Tekst");
            tree.Diagnostics.Should().BeEmpty();
            if (expected is null)
            {
                direct.Should().Contain("**Notat:** Tekst\n");
                restored.Should().Contain("**Notat:** Tekst\n");
                snapshot.Person.NoteDetails.Should().BeNull();
            }
            else
            {
                direct.Should().Contain($"**Notat:** Tekst *({expected})*");
                restored.Should().Contain($"**Notat:** Tekst *({expected})*");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Tidspunkt_FoelgerAlleNotatreferencerOgGentagneNotater()
    {
        var path = WriteFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NOTE @N1@
            1 BIRT
            2 NOTE @N1@
            2 NOTE Andet notat
            1 CENS
            2 NOTE @N1@
            1 SOUR @S1@
            1 OBJE @M1@
            0 @I2@ INDI
            0 @F1@ FAM
            1 HUSB @I1@
            1 CHIL @I2@
            1 NOTE @N1@
            1 MARR
            2 NOTE @N1@
            0 @S1@ SOUR
            1 TITL Kilde
            1 NOTE @N1@
            0 @M1@ OBJE
            1 TITL Medie
            1 NOTE @N1@
            0 @N1@ NOTE
            1 CONT Fælles notat
            1 CHAN
            2 DATE 20 OCT 2023
            3 TIME 14:03:19
            1 CREA
            2 DATE 20 OCT 2023
            3 TIME 14:02:20
            0 TRLR
            """);
        try
        {
            var tree = new GedcomLoader().Load(path);
            var person = tree.FindPerson("@I1@")!;
            var child = tree.FindPerson("@I2@")!;
            var snapshot = CanonicalBiographySnapshot.Create(person);
            var generator = new BiographyTemplateMarkdownGenerator();
            var direct = BiographyDocumentParser.Parse(generator.Generate(person)).Body;
            var stored = BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot));
            var restored = BiographyDocumentParser.Parse(
                generator.Generate(stored.Metadata!.SyncBaseline!.Approved, snapshot)).Body;
            var childSnapshot = CanonicalBiographySnapshot.Create(child);
            var childBody = BiographyDocumentParser.Parse(generator.Generate(childSnapshot, childSnapshot)).Body;

            person.Events.Single().Note.Should().Be("\nFælles notat\n\nAndet notat");
            tree.Diagnostics.Should().BeEmpty();
            foreach (var output in new[] { direct, restored })
            {
                output.Split("*(20-10-2023 14:03)*", StringSplitOptions.None).Length.Should().Be(8);
                output.Should().Contain("Andet notat").And.NotContain("14:02:20");
            }
            childBody.Should().Contain("Fælles notat *(20-10-2023 14:03)*");
            foreach (var (type, field) in new[]
                     {
                         (BiographyTemplateBlockType.Facts, BiographyTemplateField.PersonNote),
                         (BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote),
                         (BiographyTemplateBlockType.Census, BiographyTemplateField.CensusNote),
                         (BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote),
                         (BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote),
                     })
            {
                var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
                    [new(type, true, "Tabel", BiographyTemplatePresentation.Table, [field])]);
                var template = new BiographyTemplateLoader().Parse(new BiographyTemplateEditorCompiler().Compile(model));
                var renderer = new BiographyTemplateRenderer();
                foreach (var context in new[]
                         {
                             BiographyTemplateContext.FromPerson(person),
                             BiographyTemplateContext.FromSnapshot(snapshot),
                         })
                {
                    renderer.Render(template, context).Should().Contain("Fælles notat *(20-10-2023 14:03)*");
                }
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NytTidspunkt_ForSammeTekstKanGodkendesEllerAfvises()
    {
        var path = WriteFixture(
            "0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NOTE @N1@\n" +
            "0 @N1@ NOTE Samme tekst\n1 CHAN\n2 DATE 20 OCT 2023\n3 TIME 14:03:19\n0 TRLR\n");
        try
        {
            var loader = new GedcomLoader();
            var tree = loader.Load(path);
            var person = tree.FindPerson("@I1@")!;
            var approved = CanonicalBiographySnapshot.Create(person);
            File.WriteAllText(path,
                "0 HEAD\n1 CHAR UTF-8\n0 @N1@ NOTE Samme tekst\n1 CHAN\n2 DATE 21 OCT 2023\n0 TRLR\n");

            loader.Load(path, tree);
            var imported = CanonicalBiographySnapshot.Create(person);
            var state = new BiographyReconciliationState(BiographyBaselineStatus.Changed, imported, approved,
                new BiographyFactsSnapshot(null, null, null, null, null, null, []));
            var difference = new BiographyStructuredDifferenceService().Compare(state).Single();

            difference.Path.Should().Be("person.notes[0]");
            difference.ApprovedValue.Should().Contain("20-10-2023 14:03");
            difference.ImportedValue.Should().Contain("21-10-2023");
            var decisions = new BiographySnapshotDecisionService();
            var generator = new BiographyTemplateMarkdownGenerator();
            foreach (var (select, expected) in new[] { (false, "20-10-2023 14:03"), (true, "21-10-2023") })
            {
                var result = decisions.Apply(approved, imported, new Dictionary<string, bool> { [difference.Path] = select });
                result.Person.Notes.Should().Equal("Samme tekst");
                BiographyDocumentParser.Parse(generator.Generate(result, imported)).Body
                    .Should().Contain($"Samme tekst *({expected})*");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Kildehenvisninger_MedSammeTekstOgForskelligeTidspunkterBevares()
    {
        var path = WriteFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 SOUR @S1@
            2 NOTE @N1@
            1 SOUR @S1@
            2 NOTE @N2@
            0 @S1@ SOUR
            1 TITL Kilde
            0 @N1@ NOTE Tekst
            1 CHAN
            2 DATE 19 OCT 2023
            0 @N2@ NOTE Tekst
            1 CHAN
            2 DATE 20 OCT 2023
            0 TRLR
            """);
        try
        {
            var person = new GedcomLoader().Load(path).FindPerson("@I1@")!;
            var snapshot = CanonicalBiographySnapshot.Create(person);
            var generator = new BiographyTemplateMarkdownGenerator();
            foreach (var output in new[]
                     {
                         BiographyDocumentParser.Parse(generator.Generate(person)).Body,
                         BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot)).Body,
                     })
            {
                output.Should().Contain("Tekst *(19-10-2023)*")
                    .And.Contain("Tekst *(20-10-2023)*");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Tidspunkt_FormateresEfterRegionUdenAtAendreBaselineEllerSekunder()
    {
        var path = WriteFixture(
            "0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NOTE @N1@\n0 @N1@ NOTE Tekst\n" +
            "1 CREA\n2 DATE 2 JAN 2023\n3 TIME 08:04:59\n1 CHAN\n2 DATE 2 JAN 2023\n3 TIME 08:04:01\n0 TRLR\n");
        try
        {
            var person = new GedcomLoader().Load(path).FindPerson("@I1@")!;
            var snapshot = CanonicalBiographySnapshot.Create(person);
            var fingerprint = snapshot.ComputeFingerprint();
            snapshot.Person.NoteDetails![0]!.Parts.Single().Timestamp.Should().Be("2 JAN 2023 08:04:59");
            var generator = new BiographyTemplateMarkdownGenerator();
            var danish = BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot));
            danish.Body.Should().Contain("Tekst *(02-01-2023 08:04)*");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var restored = danish.Metadata!.SyncBaseline!.Approved;
            var american = BiographyDocumentParser.Parse(generator.Generate(restored, restored)).Body;
            var expected = new DateTime(2023, 1, 2, 8, 4, 59).ToString("g", CultureInfo.CurrentCulture);
            american.Should().Contain($"Tekst *({expected})*");
            restored.ComputeFingerprint().Should().Be(fingerprint);
            restored.Person.NoteDetails![0]!.Parts.Single().Timestamp.Should().Be("2 JAN 2023 08:04:59");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteFixture(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, content);
        return path;
    }
}
