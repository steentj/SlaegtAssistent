using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class DocumentMediaPreviewTests
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "person.md");

    [Theory]
    [InlineData(BiographyTemplatePresentation.List)]
    [InlineData(BiographyTemplatePresentation.Table)]
    public void GodkendteMedierVisesUnderDenAnvendteSkabelonsOverskrift(BiographyTemplatePresentation presentation)
    {
        var templatePath = Path.GetTempFileName();
        try
        {
            var model = BiographyTemplateEditorModel.CreateDefault();
            model = new(model.Version, model.Blocks.Select(block => block.Type == BiographyTemplateBlockType.Media
                ? block with { Heading = "Familiearkiv", Presentation = presentation }
                : block).ToArray());
            File.WriteAllText(templatePath, new BiographyTemplateEditorSerializer().Serialize(model));
            var person = new Person("@I1@") { FullName = "Anna" };
            person.Media.Add(new Media("@M1@") { Title = "Foto <Anna>", File = "foto.jpg" });
            person.Media.Add(new Media("@M2@") { File = "brev.pdf" });
            person.Media.Add(new Media("@M3@"));
            var generator = new BiographyTemplateMarkdownGenerator(new BiographyTemplateEditorCompiler().Compile(model));
            var document = BiographyDocumentParser.Parse(generator.Generate(person));
            var section = DocumentMediaSection.Find(document, "@I1@", _path, templatePath)!;

            section.Items.Select(item => new DocumentMediaCard(item, null).Name)
                .Should().Equal("Foto <Anna>", "brev.pdf", "Medie uden navn");
            var destination = new Uri("slaegt-media://billede/gyldigt");
            DocumentMediaCard[] cards = [new(section.Items[0], destination, "dGVzdA=="), new(section.Items[1], null)];
            var service = new SafeMarkdownPreviewService();
            var preview = service.Render(document.Body, _path, [], null, section, cards);
            preview.Html.Should().Contain("<h2>Familiearkiv</h2>\n<ul class=\"document-media\"");
            preview.Html.Should().Contain("Foto &lt;Anna&gt;").And.Contain("brev.pdf")
                .And.Contain("data:image/png;base64,dGVzdA==").And.Contain(destination.AbsoluteUri);
            service.Render(document.Body, _path).Html.Should().NotContain("document-media");
            DocumentMediaSection.Find(document, "@I2@", _path, templatePath).Should().BeNull();
            DocumentMediaSection.Find(document, "@I1@", _path, null).Should().BeNull();
        }
        finally { File.Delete(templatePath); }
    }

    [Fact]
    public void AeldreLokalHenvisningAfkodesOgLinksUdenForSektionenUdelades()
    {
        var markdown = """
            ## Medier
            - [Familiefoto](%C3%86ldre%20foto.png)
            - [Noget eksternt](https://eksempel.dk/foto.png)
            - [Absolut netværkssti](%2F%2Fserver/mappe/foto.png)
            ## Biografi
            - [Andet foto](andet.png)
            """;
        var section = DocumentMediaSection.Find(new BiographyDocument(null, markdown, false), "@I1@", _path, null);

        section.Should().NotBeNull();
        section!.InsertAt.Should().Be("## Medier\n".Length);
        section.Items.Should().ContainSingle().Which.Reference.Should().Be(Path.Combine(Path.GetTempPath(), "Ældre foto.png"));
    }

    [Fact]
    public void UdeladtMediesektionBrugerIkkeOverskriftIFriTekst()
    {
        var markdown = BiographyGeneratedSectionMerger.Wrap("## Fakta\n") + "\n## Medier\n[Andet](foto.png)";
        DocumentMediaSection.Find(new BiographyDocument(null, markdown, false), "@I1@", _path, null)
            .Should().BeNull();
    }

    [Fact]
    public void InterntBilledlinkFraMarkdownFaarIngenNavigation()
    {
        var markup = new SafeMarkdownPreviewService().Render("[Åbn](slaegt-media://billede/ukendt)", _path);
        markup.Html.Should().NotContain("href=\"");
    }

    [Fact]
    public void ZoomTilpassesUdenOpskaleringOgSkifterTilManuelleTrin()
    {
        var zoom = new DocumentImageZoom();
        zoom.UpdateFit(2000, 1000, 400, 400);
        zoom.Scale.Should().Be(0.2);
        zoom.StepBy(1);
        zoom.Scale.Should().Be(0.25);
        zoom.ActualSize();
        zoom.Scale.Should().Be(1);
        for (var index = 0; index < 20; index++) zoom.StepBy(1);
        zoom.Scale.Should().Be(4);
        zoom.CanZoomIn.Should().BeFalse();
        zoom.Fit();
        zoom.Scale.Should().Be(0.2);
        zoom.UpdateFit(100, 100, 400, 400);
        zoom.Scale.Should().Be(1);
    }

    [Fact]
    public async Task BilledvindueOgMiniaturerBrugerLokalFilOgKontrollererDenIgen()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            var directory = Directory.CreateTempSubdirectory();
            var file = Path.Combine(directory.FullName, "Ørbæk 1920.png");
            var cache = new DocumentMediaThumbnails();
            var inspection = new LocalMediaInspectionService(directory.FullName, null, null, null);
            var card = new DocumentMediaCard(new("1", "@I1@", null, null, file), new Uri("slaegt-media://billede/1"));
            var window = new DocumentImageWindow();
            try
            {
                using (var bitmap = new RenderTargetBitmap(new PixelSize(750, 500)))
                    bitmap.Save(file, new PngBitmapEncoderOptions());

                var withoutThumbnail = await cache.InspectAsync(card, inspection, false, CancellationToken.None);
                withoutThumbnail.Navigation.Should().Be(card.Navigation);
                withoutThumbnail.Thumbnail.Should().BeNull();
                var withThumbnail = await cache.InspectAsync(card, inspection, true, CancellationToken.None);
                withThumbnail.Thumbnail.Should().NotBeNullOrEmpty();
                using (var stream = new MemoryStream(Convert.FromBase64String(withThumbnail.Thumbnail!)))
                using (var thumbnail = new Bitmap(stream))
                    thumbnail.PixelSize.Should().Be(new PixelSize(160, 106));

                window.Show();
                await window.OpenImageAsync(withThumbnail, _ => Task.FromResult(inspection));
                window.Title.Should().Be("Ørbæk 1920.png");
                var photo = window.FindControl<Image>("Photo")!;
                ((Bitmap?)photo.Source)!.PixelSize.Should().Be(new PixelSize(750, 500));

                File.Delete(file);
                await window.OpenImageAsync(withThumbnail, _ => Task.FromResult(inspection));
                photo.Source.Should().BeNull();
                window.FindControl<TextBlock>("Status")!.Text.Should().Contain("findes ikke");
            }
            finally
            {
                window.Close();
                directory.Delete(true);
            }
        });
    }
}
