using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public sealed class BiographyPersonLinkTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"personlinks-{Guid.NewGuid():N}");
    private readonly FamilyTree _tree;

    public BiographyPersonLinkTests()
    {
        Directory.CreateDirectory(_folder);
        var file = Path.Combine(_folder, "familie.ged");
        File.WriteAllText(file, "0 HEAD\n1 SOUR Test\n1 GEDC\n2 VERS 5.5.1\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Far\n0 @I2@ INDI\n1 NAME Barn\n0 @F1@ FAM\n1 HUSB @I1@\n1 CHIL @I2@\n0 TRLR\n");
        _tree = new GedcomLoader().Load(file);
    }

    [Fact]
    public void BatchExport_LinksForwardAndBackwardAndPreservesExistingCustomFilename()
    {
        var father = _tree.FindPerson("@I1@")!;
        var existing = new BiographyTemplateMarkdownGenerator().Generate(father);
        File.WriteAllText(Path.Combine(_folder, "min far (æ).md"), existing);
        father.FullName = "Nyt navn";
        var service = new MarkdownBiographyExportService(new Settings());
        var generated = service.GenerateBiographies(_tree, _folder);
        generated["@I2@"].Should().Contain("[Nyt navn](min%20far%20%28%C3%A6%29.md)");
        generated["@I1@"].Should().Contain($"[Barn]({BiographyFileNameGenerator.Generate(_tree.FindPerson("@I2@")!)})");
        service.WriteBiographies(_tree, _folder);
        File.ReadAllText(Path.Combine(_folder, "min far (æ).md")).Should().Be(existing);
        File.ReadAllText(Path.Combine(_folder, BiographyFileNameGenerator.Generate(_tree.FindPerson("@I2@")!)))
            .Should().Contain("[Nyt navn](min%20far%20%28%C3%A6%29.md)");
        service.GenerateBiographies(_tree, _folder)["@I2@"].Should().Be(generated["@I2@"]);
    }

    [Fact]
    public void DuplicateIdentity_DoesNotLinkToEitherFileOrInventANewTarget()
    {
        var person = _tree.FindPerson("@I1@")!;
        var markdown = new BiographyTemplateMarkdownGenerator().Generate(person);
        File.WriteAllText(Path.Combine(_folder, "far.md"), markdown);
        File.WriteAllText(Path.Combine(_folder, "kopi.md"), markdown);
        var generated = new MarkdownBiographyExportService(new Settings()).GenerateBiographies(_tree, _folder);
        generated["@I2@"].Should().Contain("**Forældre:** Far\n").And.NotContain("[Far](");
    }

    [Fact]
    public void OccupiedInvalidTarget_DoesNotCreateBrokenLink()
    {
        var person = _tree.FindPerson("@I1@")!;
        File.WriteAllText(Path.Combine(_folder, BiographyFileNameGenerator.Generate(person)), "---\nformatVersion: 99\n---\n");
        var generated = new MarkdownBiographyExportService(new Settings()).GenerateBiographies(_tree, _folder);
        generated["@I2@"].Should().Contain("**Forældre:** Far\n").And.NotContain("[Far](");
    }

    [Fact]
    public void BatchGeneration_ObservesCancellation()
    {
        var service = new MarkdownBiographyExportService(new Settings());
        var action = () => service.GenerateBiographies(_tree, _folder, new CancellationToken(true));
        action.Should().Throw<OperationCanceledException>();
    }

    public void Dispose() => Directory.Delete(_folder, true);

    private sealed class Settings : IApplicationSettingsService
    {
        public AppSettings Load() => new();
        public void Save(AppSettings settings) { }
    }
}
