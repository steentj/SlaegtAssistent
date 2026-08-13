using FluentAssertions;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class GedcomFaultToleranceTests
{
    [Fact]
    public void Load_RepresentativDelvisFixtureGiverSporbarRapport()
    {
        var tree = new GedcomLoader().Load(FixturePath("partial-recovery.ged"));

        tree.People.Select(person => person.RecordId).Should().Equal("@I1@", "@I2@");
        tree.ImportReport.ImportedRecords.Should().Be(2);
        tree.ImportReport.ImportedWithWarnings.Should().Be(2);
        tree.ImportReport.SkippedRecords.Should().Be(1);
        tree.ImportReport.FatalErrors.Should().Be(0);
        tree.ImportReport.Diagnostics.Should().HaveCount(3);
    }

    [Fact]
    public void Load_RepresentativFatalFixtureGiverFatalRapport()
    {
        var action = () => new GedcomLoader().Load(FixturePath("fatal-missing-trailer.ged"));

        var exception = action.Should().Throw<GedcomLoadException>().Which;
        exception.ImportReport!.FatalErrors.Should().Be(1);
        exception.ImportReport.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Tag == "TRLR");
    }

    [Fact]
    public void Load_DefektPersonMellemGyldigePosterSpringesOverMedKompletDiagnostik()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            0 INDI
            1 NAME Mangler /Id/
            0 @I2@ INDI
            1 NAME Bent /Jensen/
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.People.Select(person => person.RecordId).Should().Equal("@I1@", "@I2@");
            tree.ImportReport.ImportedRecords.Should().Be(2);
            tree.ImportReport.ImportedWithWarnings.Should().Be(0);
            tree.ImportReport.SkippedRecords.Should().Be(1);
            tree.ImportReport.FatalErrors.Should().Be(0);
            tree.ImportReport.IsPartial.Should().BeTrue();
            tree.Diagnostics.Should().ContainSingle();
            var diagnostic = tree.Diagnostics.Single();
            diagnostic.Severity.Should().Be(GedcomDiagnosticSeverity.Error);
            diagnostic.Line.Should().Be(5);
            diagnostic.RecordId.Should().BeNull();
            diagnostic.Tag.Should().Be("INDI");
            diagnostic.FilePath.Should().Be(path);
            diagnostic.Consequence.Should().Be("Personposten blev sprunget over; øvrige poster blev bevaret.");
            diagnostic.Message.Should().Be("Personposten mangler et gyldigt record-id.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_DefektUnderfeltSpringesOverUdenAtMistePersonposten()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            ugyldig linje
            1 BIRT
            2 DATE 1900
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.FindPerson("@I1@").Should().NotBeNull();
            tree.FindPerson("@I1@")!.BirthDate.Should().Be("1900");
            tree.ImportReport.ImportedRecords.Should().Be(1);
            tree.ImportReport.ImportedWithWarnings.Should().Be(1);
            tree.ImportReport.SkippedRecords.Should().Be(0);
            tree.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Line == 5
                && diagnostic.RecordId == "@I1@"
                && diagnostic.Tag == null
                && diagnostic.Consequence == "Kun det ugyldige underfelt blev sprunget over.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Anna /Jensen/\n0 TRLR\n", "HEAD")]
    [InlineData("0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Anna /Jensen/\n", "TRLR")]
    public void Load_FatalFilstrukturAfvisesMedStruktureretRapport(string content, string tag)
    {
        var path = CreateFixture(content);

        try
        {
            var action = () => new GedcomLoader().Load(path);

            var exception = action.Should().Throw<GedcomLoadException>().Which;
            exception.ImportReport.Should().NotBeNull();
            exception.ImportReport!.FatalErrors.Should().Be(1);
            exception.ImportReport.SkippedRecords.Should().Be(0);
            exception.ImportReport.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Severity == GedcomDiagnosticSeverity.Fatal
                && diagnostic.Tag == tag
                && diagnostic.FilePath == path
                && diagnostic.Consequence == "Hele importen blev afbrudt uden ændringer.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UkendtLevelZeroPostSpringesOverMedDiagnostik()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @X1@ _UKENDT
            1 NOTE Må ikke forsvinde lydløst
            0 @I1@ INDI
            1 NAME Anna /Jensen/
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.FindPerson("@I1@").Should().NotBeNull();
            tree.ImportReport.SkippedRecords.Should().Be(1);
            tree.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Tag == "_UKENDT"
                && diagnostic.RecordId == "@X1@"
                && diagnostic.Message.Contains("ukendte GEDCOM-post"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_DubleretRecordIdBevarerFoerstePostOgRapportererDenSenere()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Første /Person/
            0 @I1@ INDI
            1 NAME Senere /Dublet/
            0 @I2@ INDI
            1 NAME Gyldig /Efterfølger/
            0 TRLR
            """);

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.FindPerson("@I1@")!.FullName.Should().Be("Første Person");
            tree.FindPerson("@I2@").Should().NotBeNull();
            tree.ImportReport.SkippedRecords.Should().Be(1);
            tree.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.RecordId == "@I1@"
                && diagnostic.Consequence == "Den senere dubletpost blev sprunget over; den første post blev bevaret.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("NOTE")]
    [InlineData("REPO")]
    [InlineData("SUBN")]
    public void Load_KendtMenIkkeModelleretPostForsvinderIkkeLydloest(string tag)
    {
        var path = CreateFixture(
            $"0 HEAD\n1 CHAR UTF-8\n0 @X1@ {tag}\n1 NOTE Bevaringskrævende data\n" +
            "0 @I1@ INDI\n1 NAME Anna /Jensen/\n0 TRLR\n");

        try
        {
            var tree = new GedcomLoader().Load(path);

            tree.FindPerson("@I1@").Should().NotBeNull();
            tree.ImportReport.SkippedRecords.Should().Be(1);
            tree.Diagnostics.Should().ContainSingle(diagnostic =>
                diagnostic.Tag == tag
                && diagnostic.RecordId == "@X1@"
                && diagnostic.Consequence == "Posten blev ikke importeret; pointere i understøttede felter blev bevaret.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateFixture(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, content);
        return path;
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
