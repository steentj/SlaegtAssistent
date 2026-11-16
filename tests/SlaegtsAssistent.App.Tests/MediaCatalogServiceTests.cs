using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaCatalogServiceTests
{
    [Fact]
    public void KatalogBrugerGodkendtSnapshotOgIkkeAfvisteImportdata()
    {
        var approvedPerson = new Person("@I1@") { FullName = "Anna" };
        approvedPerson.Media.Add(new Media("@M1@") { File = "godkendt.jpg", Title = "Godkendt" });
        var importedPerson = new Person("@I1@") { FullName = "Anna" };
        importedPerson.Media.Add(new Media("@M2@") { File = "afvist.jpg", Title = "Afvist" });
        var approved = CanonicalBiographySnapshot.Create(approvedPerson);
        var imported = CanonicalBiographySnapshot.Create(importedPerson);
        var document = BiographyDocumentParser.Parse(new BiographyTemplateMarkdownGenerator().Generate(approved, imported));

        var items = new MediaCatalogService().Build([new("@I1@", document, imported)]);

        items.Should().ContainSingle().Which.Reference.Should().Be("godkendt.jpg");
        items.Should().NotContain(item => item.Reference == "afvist.jpg");
    }

    [Fact]
    public void InlineMedierErStabilePerEjerOgFallbackBrugesKunUdenSnapshot()
    {
        var anna = new Person("@I1@");
        anna.Media.Add(new Media(null!) { File = "familie/foto.jpg", Title = "Familie" });
        var bo = new Person("@I2@");
        bo.Media.Add(new Media(null!) { File = "familie/foto.jpg", Title = "Familie" });
        var a = CanonicalBiographySnapshot.Create(anna);
        var b = CanonicalBiographySnapshot.Create(bo);
        MediaCatalogSource[] sources = [new("@I1@", null, a), new("@I2@", null, b)];
        var service = new MediaCatalogService();
        var first = service.Build(sources);
        first.Should().HaveCount(2);
        first.Select(x => x.Key).Should().OnlyHaveUniqueItems();
        service.Build(sources).Select(x => x.Key).Should().Equal(first.Select(x => x.Key));
        first.Select(x => x.OwnerId).Should().Contain(["@I1@", "@I2@"]);
    }

    [Fact]
    public void TommeEllerIkkeUnderstoettedeSnapshotsGiverIkkeImporteredeMedier()
    {
        var person = new Person("@I1@");
        person.Media.Add(new Media("@M1@") { File = "foto.jpg" });
        var approved = CanonicalBiographySnapshot.Create(person);
        var doc = BiographyDocumentParser.Parse(new BiographyTemplateMarkdownGenerator().Generate(person));
        var baseline = doc.Metadata!.SyncBaseline! with { Version = 999 };
        var invalid = doc with { Metadata = doc.Metadata with { SyncBaseline = baseline } };
        new MediaCatalogService().Build([new("@I1@", invalid, null)]).Should().BeEmpty();
        new MediaCatalogService().Build([new("@I1@", null, approved)]).Should().ContainSingle();
    }
}
