using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Tests;

public class BiographyExportHtmlBuilderTests
{
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
