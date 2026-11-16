using FluentAssertions;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyImageEditServiceTests
{
    private readonly BiographyImageEditService _service = new();
    private static readonly BiographyImageSelection Image = new("../medier/anna%20foto.jpg", "Anna [Jensen]", "Anna omkring 1930.");
    private const string Body = "<!-- SlaegtsAssistent:generated:start -->\n# Anna\n<!-- SlaegtsAssistent:generated:end -->\n## Biografi\nHej 🌳 verden.";

    [Theory]
    [InlineData(BiographyImageWidth.FullPage, "![Foto](foto.jpg)")]
    [InlineData(BiographyImageWidth.HalfPage, "![Foto](foto.jpg \"SlaegtsAssistent:width=50\")")]
    [InlineData(BiographyImageWidth.QuarterPage, "![Foto](foto.jpg \"SlaegtsAssistent:width=25\")")]
    public void BilledstoerrelseGemmesOgKanAEndres(BiographyImageWidth width, string expected)
    {
        var image = new BiographyImageSelection("foto.jpg", "Foto", null, width);
        var portrait = _service.SetPortrait(Body, image);
        portrait.IsSuccess.Should().BeTrue();
        portrait.Body.Should().Contain(expected);
        _service.SetPortrait(portrait.Body, image).IsSuccess.Should().BeTrue();
        var insertion = _service.InsertInBiography(Body, new(Body.Length, Body.Length, 1), 1, image);
        insertion.Body.Should().Contain(expected);
    }

    [Fact]
    public void TopbilledeKanTilfoejesUdskiftesOgFjernesUdenAtAendreResten()
    {
        var inserted = _service.SetPortrait(Body, Image);
        inserted.IsSuccess.Should().BeTrue();
        inserted.Body.Should().StartWith(BiographyImageEditService.PortraitStartMarker);
        inserted.Body.Should().Contain("![Anna \\[Jensen\\]](../medier/anna%20foto.jpg)");
        inserted.Body.Should().EndWith(Body);
        var changed = _service.SetPortrait(inserted.Body, new("foto.jpg", "Andet", null));
        changed.Body.Should().Contain("![Andet](foto.jpg)").And.NotContain("Anna omkring 1930.");
        var removed = _service.RemovePortrait(changed.Body);
        removed.Body.Should().Be(Body);
    }

    [Theory]
    [InlineData("<!-- SlaegtsAssistent:portrait:start -->\n![a](a.jpg)")]
    [InlineData("<!-- SlaegtsAssistent:portrait:end -->\n<!-- SlaegtsAssistent:portrait:start -->")]
    [InlineData("<!-- SlaegtsAssistent:portrait:start -->\n![a](a.jpg)\n![b](b.jpg)\n<!-- SlaegtsAssistent:portrait:end -->")]
    public void UgyldigTopblokAfvisesUdenAendringer(string body)
    {
        var result = _service.SetPortrait(body, Image);
        result.IsSuccess.Should().BeFalse();
        result.Body.Should().Be(body);
        _service.RemovePortrait(body).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void IndsaettelseRespektererUtf16MarkeringOgBevarerCrlf()
    {
        var body = Body.Replace("\n", "\r\n");
        var start = body.IndexOf("🌳", StringComparison.Ordinal);
        var result = _service.InsertInBiography(body, new EditorSelection(start, start + 2, 4), 4, Image);
        result.IsSuccess.Should().BeTrue();
        result.Body.Should().Contain("Hej \r\n\r\n![Anna \\[Jensen\\]](../medier/anna%20foto.jpg)\r\n\r\nAnna omkring 1930\\.\r\n\r\n verden.");
        result.Body.Replace("\r\n", "").Should().NotContain("\n");
        result.CaretPosition.Should().BeGreaterThan(start);
    }

    [Fact]
    public void BeskyttetEllerForaeldetMarkeringAfvises()
    {
        var generated = Body.IndexOf("# Anna", StringComparison.Ordinal);
        _service.InsertInBiography(Body, new(generated, generated, 1), 1, Image).IsSuccess.Should().BeFalse();
        var biography = Body.IndexOf("Hej", StringComparison.Ordinal);
        _service.InsertInBiography(Body, new(biography, biography, 1), 2, Image).IsSuccess.Should().BeFalse();
        _service.InsertInBiography(Body, new(generated, biography, 1), 1, Image).Body.Should().Be(Body);
        _service.InsertInBiography(Body.Replace(BiographyGeneratedSectionMerger.EndMarker, ""), new(biography, biography, 1), 1, Image).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void GamleDokumenterTilladerFriTekstMenBeskytterTopblokken()
    {
        const string original = "Ældre livsforløb.";
        var portrait = _service.SetPortrait(original, Image);
        var start = portrait.Body.IndexOf("livsforløb", StringComparison.Ordinal);
        _service.InsertInBiography(portrait.Body, new(start, start, 1), 1, Image).IsSuccess.Should().BeTrue();
        _service.InsertInBiography(portrait.Body, new(0, 0, 1), 1, Image).IsSuccess.Should().BeFalse();
        _service.RemovePortrait(portrait.Body).Body.Should().Be(original);
    }

    [Fact]
    public void MarkoererMidtPaaEnLinjeEllerFlereParAfvises()
    {
        var broken = Body.Replace(BiographyGeneratedSectionMerger.EndMarker,
            "tekst " + BiographyGeneratedSectionMerger.EndMarker, StringComparison.Ordinal);
        _service.InsertInBiography(broken, new(broken.Length, broken.Length, 1), 1, Image).IsSuccess.Should().BeFalse();
        var portrait = _service.SetPortrait(Body, Image).Body;
        var duplicate = portrait + portrait;
        _service.RemovePortrait(duplicate).IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void MarkeringMaaIkkeDeleEtEmoji()
    {
        var position = Body.IndexOf("🌳", StringComparison.Ordinal) + 1;
        var result = _service.InsertInBiography(Body, new(position, position, 1), 1, Image);
        result.IsSuccess.Should().BeFalse();
        result.Body.Should().Be(Body);
    }

    [Fact]
    public void KandidatopdateringBevarerValgtTopbilledeOgFriTekst()
    {
        var portrait = _service.SetPortrait(Body, Image);
        var candidate = BiographyGeneratedSectionMerger.CreateCandidate(portrait.Body, "# Nyt navn");
        candidate.Content.Should().StartWith(BiographyImageEditService.PortraitStartMarker);
        candidate.Content.Should().Contain("![Anna \\[Jensen\\]](../medier/anna%20foto.jpg)");
        candidate.Content.Should().EndWith("## Biografi\nHej 🌳 verden.");
    }

    [Fact]
    public void MarkupIEksterneTeksterKanIkkeSkabeRaHtmlEllerBilleder()
    {
        var result = _service.SetPortrait(Body, new("foto.jpg", "<img>\n![andet](x)", "<script>"));
        result.IsSuccess.Should().BeTrue();
        result.Body.Should().NotContain("<img>").And.NotContain("<script>");
        _service.SetPortrait(Body, new("https://example.com/x.jpg", "x", null)).IsSuccess.Should().BeFalse();
    }
}
