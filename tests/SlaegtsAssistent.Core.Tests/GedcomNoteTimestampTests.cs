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
    [InlineData(BiographyTemplatePresentation.List)]
    [InlineData(BiographyTemplatePresentation.Table)]
    public void Notatsektion_ViserPersonOgFamilieMedDatoOgSikkerTekst(BiographyTemplatePresentation presentation)
    {
        var path = WriteFixture("""
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NOTE @N1@
            1 NOTE Uden dato
            0 @F1@ FAM
            1 HUSB @I1@
            1 NOTE @N2@
            0 @N1@ NOTE Person | tekst
            1 CONT ## Ikke overskrift
            1 CREA
            2 DATE 20 NOV 2025
            1 CHAN
            2 DATE 21 NOV 2025
            3 TIME 14:32:00
            0 @N2@ NOTE Familien flyttede
            1 CREA
            2 DATE 19 NOV 2025
            1 CHAN
            2 DATE 18 NOV 2025
            0 TRLR
            """);
        try
        {
            var person = new GedcomLoader().Load(path).FindPerson("@I1@")!;
            var snapshot = CanonicalBiographySnapshot.Create(person);
            person.Notes[1] = "Ny import";
            var block = new BiographyTemplateBlock(BiographyTemplateBlockType.Notes, true, "Notater",
                presentation, [BiographyTemplateField.PersonNote, BiographyTemplateField.FamilyNote]);
            var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion, [block]);
            var source = new BiographyTemplateEditorCompiler().Compile(model);
            var template = new BiographyTemplateLoader().Parse(source);
            var renderer = new BiographyTemplateRenderer();
            var output = renderer.Render(template, BiographyTemplateContext.FromSnapshot(snapshot));

            if (presentation == BiographyTemplatePresentation.Table)
            {
                output.Should().Contain("| Dato | Notat |\n| --- | --- |")
                    .And.Contain("| 21-11-2025 14:32 | Person: Person \\| tekst \\## Ikke overskrift |")
                    .And.Contain("|  | Person: Uden dato |")
                    .And.Contain("| 19-11-2025 | Familie: Familien flyttede |");
            }
            else
            {
                output.Should().Contain("- **21-11-2025 14:32**: Person: Person \\| tekst")
                    .And.Contain("- Person: Uden dato")
                    .And.Contain("- **19-11-2025**: Familie: Familien flyttede");
            }
            output.Should().NotContain("\n## Ikke overskrift").And.NotContain("Ny import");
            renderer.Render(template, BiographyTemplateContext.FromPerson(person)).Should().Contain("Ny import");

            var hidden = new BiographyTemplateEditorModel(model.Version, [block with { IsVisible = false }]);
            new BiographyTemplateEditorCompiler().Compile(hidden).Should().NotContain("Notater");
            var familyOnly = new BiographyTemplateEditorModel(model.Version,
                [new(block.Type, true, block.Heading, presentation, [BiographyTemplateField.FamilyNote])]);
            var familyOutput = renderer.Render(new BiographyTemplateLoader().Parse(
                new BiographyTemplateEditorCompiler().Compile(familyOnly)), BiographyTemplateContext.FromSnapshot(snapshot));
            familyOutput.Should().Contain("Familie: Familien flyttede").And.NotContain("Person:");
        }
        finally
        {
            File.Delete(path);
        }
    }

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
                direct.Should().Contain("- Person: Tekst\n");
                restored.Should().Contain("- Person: Tekst\n");
                snapshot.Person.NoteDetails.Should().BeNull();
            }
            else
            {
                direct.Should().Contain($"- **{expected}**: Person: Tekst");
                restored.Should().Contain($"- **{expected}**: Person: Tekst");
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
                output.Split("*(20-10-2023 14:03)*", StringSplitOptions.None).Length.Should().Be(6);
                output.Split("**20-10-2023 14:03**", StringSplitOptions.None).Length.Should().Be(3);
                output.Should().Contain("Andet notat").And.NotContain("14:02:20");
            }
            childBody.Should().Contain("**20-10-2023 14:03**: Familie:").And.Contain("Fælles notat");
            foreach (var (type, field) in new[]
                     {
                         (BiographyTemplateBlockType.Notes, BiographyTemplateField.PersonNote),
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
                    var rendered = renderer.Render(template, context);
                    if (type == BiographyTemplateBlockType.Notes)
                        rendered.Should().Contain("| 20-10-2023 14:03 | Person:");
                    else
                        rendered.Should().Contain("Fælles notat *(20-10-2023 14:03)*");
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
                    .Should().Contain($"**{expected}**: Person: Samme tekst");
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
            danish.Body.Should().Contain("**02-01-2023 08:04**: Person: Tekst");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var restored = danish.Metadata!.SyncBaseline!.Approved;
            var american = BiographyDocumentParser.Parse(generator.Generate(restored, restored)).Body;
            var expected = new DateTime(2023, 1, 2, 8, 4, 59).ToString("g", CultureInfo.CurrentCulture);
            american.Should().Contain($"**{expected}**: Person: Tekst");
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
