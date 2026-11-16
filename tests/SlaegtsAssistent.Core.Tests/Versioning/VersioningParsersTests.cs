using FluentAssertions;
using SlaegtsAssistent.Core.Versioning;

namespace SlaegtsAssistent.Core.Tests.Versioning;

public sealed class GitStatusParserTests
{
    [Fact]
    public void Parse_TomInput_ReturnererTomListe()
    {
        GitStatusParser.Parse(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Parse_KlassificererOgSortererPosterKorrekt()
    {
        var input = " M b.md\0?? ny fil æøå.md\0 D gammel.md\0A  a.md\0";

        var result = GitStatusParser.Parse(input);

        result.Should().Equal(
            new WorkingTreeChange("a.md", WorkingTreeChangeKind.Ny),
            new WorkingTreeChange("b.md", WorkingTreeChangeKind.Aendret),
            new WorkingTreeChange("gammel.md", WorkingTreeChangeKind.Slettet),
            new WorkingTreeChange("ny fil æøå.md", WorkingTreeChangeKind.Ny));
    }

    [Fact]
    public void Parse_Omdoebning_MedtagerKunNySti()
    {
        var input = "R  ny.md\0gammel.md\0";

        var result = GitStatusParser.Parse(input);

        result.Should().ContainSingle();
        result[0].Path.Should().Be("ny.md");
    }
}

public sealed class GitLogParserTests
{
    [Fact]
    public void Parse_BevarerRaekkefoelgeOgTidszone()
    {
        var input =
            "id1\u001f2026-11-12T09:23:00+01:00\u001fBesked æøå\u001e" +
            "id2\u001f2026-11-11T08:00:00+01:00\u001f\u001e";

        var result = GitLogParser.Parse(input, 10);

        result.HasMore.Should().BeFalse();
        result.Entries.Should().HaveCount(2);
        result.Entries[0].Id.Should().Be("id1");
        result.Entries[0].Message.Should().Be("Besked æøå");
        result.Entries[0].Time.ToString("o").Should().Be("2026-11-12T09:23:00.0000000+01:00");
        result.Entries[1].Message.Should().BeEmpty();
    }

    [Fact]
    public void Parse_AfkorterVedMaxEntries()
    {
        var input =
            "id1\u001f2026-11-12T09:23:00+01:00\u001fA\u001e" +
            "id2\u001f2026-11-12T10:23:00+01:00\u001fB\u001e";

        var result = GitLogParser.Parse(input, 1);

        result.HasMore.Should().BeTrue();
        result.Entries.Should().ContainSingle().Which.Id.Should().Be("id1");
    }
}

public sealed class UnifiedDiffParserTests
{
    [Fact]
    public void Parse_TomInput_ReturnererTomListe()
    {
        UnifiedDiffParser.Parse(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Parse_ParserFlereFiltyperOgHunks()
    {
        var diff = """
                   diff --git a/a.md b/a.md
                   index 111..222 100644
                   --- a/a.md
                   +++ b/a.md
                   @@ -1,2 +1,2 @@
                   -gammel
                   +ny
                    uændret
                   @@ -4,0 +4,1 @@
                   +ekstra
                   diff --git a/ny.md b/ny.md
                   new file mode 100644
                   --- /dev/null
                   +++ b/ny.md
                   @@ -0,0 +1 @@
                   +linje
                   diff --git a/slettet.md b/slettet.md
                   deleted file mode 100644
                   --- a/slettet.md
                   +++ /dev/null
                   @@ -1 +0,0 @@
                   -farvel
                   diff --git a/gammel.md b/nyt-navn.md
                   similarity index 100%
                   rename from gammel.md
                   rename to nyt-navn.md
                   @@ -1 +1 @@
                   -a
                   +b
                   """;

        var files = UnifiedDiffParser.Parse(diff);

        files.Should().HaveCount(4);
        files[0].Path.Should().Be("a.md");
        files[0].Kind.Should().Be(WorkingTreeChangeKind.Aendret);
        files[0].Lines.Count(line => line.Kind == DiffLineKind.HunkHeader).Should().Be(2);
        files[1].Kind.Should().Be(WorkingTreeChangeKind.Ny);
        files[2].Kind.Should().Be(WorkingTreeChangeKind.Slettet);
        files[3].Path.Should().Be("nyt-navn.md");
    }

    [Fact]
    public void Parse_SpringerNoNewlineOver_OgFjernerCr()
    {
        var diff = "diff --git a/a.md b/a.md\r\n--- a/a.md\r\n+++ b/a.md\r\n@@ -1 +1 @@\r\n-line\r\n+line2\r\n\\ No newline at end of file\r\n";

        var files = UnifiedDiffParser.Parse(diff);

        files.Should().ContainSingle();
        files[0].Lines.Should().Contain(line => line.Kind == DiffLineKind.Removed && line.Text == "line");
        files[0].Lines.Should().Contain(line => line.Kind == DiffLineKind.Added && line.Text == "line2");
    }

    [Fact]
    public void Parse_TekstlinjeDerStarterMedDiffGit_BehandlesSomIndhold()
    {
        var diff = "diff --git a/a.md b/a.md\n--- a/a.md\n+++ b/a.md\n@@ -1 +1 @@\n-old\n+diff --git dette er tekst\n";

        var files = UnifiedDiffParser.Parse(diff);

        files.Should().ContainSingle();
        files[0].Lines.Should().Contain(line => line.Kind == DiffLineKind.Added && line.Text == "diff --git dette er tekst");
    }
}

public sealed class CommitMessageSuggesterTests
{
    [Fact]
    public void Suggest_BrugerLokaltFormat()
    {
        var local = new DateTimeOffset(2026, 11, 12, 9, 23, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 11, 12, 9, 23, 0)));

        CommitMessageSuggester.Suggest(local).Should().Be("Ændringer 12-11-2026 09:23");
    }
}

public sealed class CommitIdValidatorTests
{
    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef01234567", true)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("ABCDEF0123456789abcdef0123456789abcdef01", false)]
    [InlineData("0123456789abcdef0123456789abcdef0123456", false)]
    [InlineData("abc; rm -rf", false)]
    [InlineData("--help", false)]
    public void IsValid_ValidererFormat(string? id, bool expected)
    {
        CommitIdValidator.IsValid(id).Should().Be(expected);
    }
}
