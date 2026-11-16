using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Tests;

public class BiographyExportHtmlBuilderTests
{
    [Fact]
    public void EksportMedtagerTopOgLivsforloebsbilledeFraSeparatMediemappe()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var docs = Directory.CreateDirectory(Path.Combine(root, "dokumenter")).FullName;
        var media = Directory.CreateDirectory(Path.Combine(root, "medier")).FullName;
        try
        {
            File.WriteAllBytes(Path.Combine(media, "foto.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC"));
            var body = "<!-- SlaegtsAssistent:portrait:start -->\n![Anna](../medier/foto.png)\n<!-- SlaegtsAssistent:portrait:end -->\n\n<!-- SlaegtsAssistent:generated:start -->\n# Anna\n<!-- SlaegtsAssistent:generated:end -->\n\n## Biografi\n\n![Livsforløb](../medier/foto.png)";
            var collector = new ExportResourceCollector(Path.Combine(root, "assets"), [media]);
            var result = new BiographyExportHtmlBuilder().Build([new("a", "Anna", Path.Combine(docs, "person.md"), body)], BiographyExportMode.Combined, collector);
            result.Warnings.Should().BeEmpty();
            result.Html.Split("<img ").Length.Should().Be(3);
            result.Html.Should().NotContain("SlaegtsAssistent:portrait").And.NotContain("SlaegtsAssistent:generated");
            Directory.GetFiles(Path.Combine(root, "assets")).Should().ContainSingle();
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void TopbilledeBevarerTitelUdenSynligeInterneMarkoerer()
    {
        const string body = "<!-- SlaegtsAssistent:portrait:start -->\n![Foto](mangler.png)\n<!-- SlaegtsAssistent:portrait:end -->\n\n<!-- SlaegtsAssistent:generated:start -->\n# Anna\n<!-- SlaegtsAssistent:generated:end -->";
        var result = new BiographyExportHtmlBuilder().Build([new("a", "Anna", "/tmp/a.md", body)], BiographyExportMode.Combined);
        result.Html.Should().NotContain("SlaegtsAssistent:portrait");
        result.Html.Should().Contain("id=\"person-000001-anna\"");
        result.Html.Should().NotContain("<h2 id=\"person-000001-anna\">");
    }

    [Fact]
    public void SamletDokumentHarKapitlerPersonlinksOgSikkerHtml()
    {
        var result = new BiographyExportHtmlBuilder().Build(
        [
            new("b", "Bo", "/tmp/bo.md", "# Bo\n\n[Anna](anna.md) og [net](https://example.com).\n\n<script>alert(1)</script>"),
            new("a", "Anna", "/tmp/anna.md", "# Anna\n\n<!-- SlaegtsAssistent:generated:start -->\n\n## Kilder\n\nTekst æøå\n\n<!-- SlaegtsAssistent:generated:end -->")
        ], BiographyExportMode.Combined);

        result.Html.Should().Contain("id=\"person-000001\"").And.Contain("href=\"#person-000001\"");
        result.Html.Should().Contain("<h2 id=\"person-000001-kilder\">Kilder</h2>").And.Contain("Tekst æøå");
        result.Html.Should().NotContain("SlaegtsAssistent:generated").And.NotContain("<script>");
        result.Html.Should().Contain("&lt;script&gt;");
        result.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void LokaleAnkreErAdskiltOgKodeOmskrivesIkke()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "[Kilder](#kilder)\n\n## Kilder\n\n`[Bo](bo.md)`"),
             new("b", "Bo", "/tmp/bo.md", "[Kilder](#kilder)\n\n## Kilder")], BiographyExportMode.Combined);
        result.Html.Should().Contain("href=\"#person-000001-kilder\"")
            .And.Contain("href=\"#person-000002-kilder\"")
            .And.Contain("<code>[Bo](bo.md)</code>");
    }

    [Fact]
    public void FodnoterFraFlereKapitlerHarEntydigeLinks()
    {
        const string body = "Tekst[^1].\n\n[^1]: En kilde.";
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", body), new("b", "Bo", "/tmp/b.md", body)], BiographyExportMode.Combined);
        foreach (var chapter in new[] { "person-000001", "person-000002" })
        {
            result.Html.Should().Contain($"id=\"{chapter}-fn-1\"")
                .And.Contain($"href=\"#{chapter}-fn-1\"")
                .And.Contain($"id=\"{chapter}-fnref-1-1\"")
                .And.Contain($"href=\"#{chapter}-fnref-1-1\"");
        }
    }

    [Fact]
    public void IndledendeGenereretMarkoerGiverIkkeDobbeltKapiteloverskrift()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "<!-- SlaegtsAssistent:generated:start -->\n\n# Anna\n\n## Fakta\n\nTekst\n\n<!-- SlaegtsAssistent:generated:end -->")],
            BiographyExportMode.Combined);
        result.Html.Should().Contain("<h1 id=\"person-000001-anna\"><span id=\"person-000001\"></span>Anna</h1>");
        result.Html.Should().NotContain("<h2 id=\"person-000001-anna\">");
    }

    [Fact]
    public void ReferencelinkMedFragmentPeegerPaaValgtPerson()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "[Bo][person]\n\n[person]: bo.md#kilder"), new("b", "Bo", "/tmp/bo.md", "# Bo")],
            BiographyExportMode.Combined);
        result.Html.Should().Contain("href=\"#person-000002\">Bo</a>");
    }

    [Fact]
    public void RaatHtmlbilledeBliverTilPladsholderMedAlttekst()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "Tekst <img src=\"https://example.org/foto.png\" alt=\"Åse &amp; Bo\">.")],
            BiographyExportMode.Separate);
        result.Html.Should().Contain("Åse &amp; Bo (Billede ikke medtaget)");
        result.Html.Should().NotContain("example.org").And.NotContain("<img");
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void GentagneFodnotereferencerHarEntydigeMaal()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "Første[^1] og anden[^1].\n\n[^1]: Kilde.")], BiographyExportMode.Combined);
        result.Html.Should().Contain("id=\"person-000001-fnref-1-1\"")
            .And.Contain("id=\"person-000001-fnref-1-2\"")
            .And.Contain("href=\"#person-000001-fn-1\"");
    }

    [Fact]
    public void RaaSvgBliverKunTilPladsholder()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "<svg><image href=\"https://example.org/a\"/></svg>")], BiographyExportMode.Separate);
        result.Html.Should().Contain("Billede ikke medtaget");
        result.Html.Should().NotContain("<svg").And.NotContain("example.org");
    }

    [Fact]
    public void SvgbehandlingBevarerOmgivendeOgEfterfoelgendeTekst()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "Før <svg><path/></svg> efter.\n\nEn uafsluttet <svg> sluttekst.")], BiographyExportMode.Separate);
        result.Html.Should().Contain("Før ").And.Contain(" efter.").And.Contain(" sluttekst.");
    }

    [Fact]
    public void BrugersEgetPiltegnBevaresSomTekst()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "Min egen pil ↗ og fri tekst.")], BiographyExportMode.Separate);
        result.Html.Should().Contain("Min egen pil ↗ og fri tekst.");
    }

    [Fact]
    public void AktiveLinksOgBillederFaarIkkeRessourceadgang()
    {
        var result = new BiographyExportHtmlBuilder().Build(
            [new("a", "Anna", "/tmp/a.md", "[klik](javascript:alert) ![Portræt](https://example.com/a.png)")],
            BiographyExportMode.Separate);
        result.Html.Should().NotContain("javascript:").And.NotContain("example.com").And.Contain("Portræt").And.Contain("Billede ikke medtaget");
        result.Warnings.Should().NotBeEmpty();
    }
}
