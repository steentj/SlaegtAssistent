using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyTemplateContractTests
{
    [Fact]
    public void Parse_ShouldRejectUnknownFieldWithFileLineAndColumn()
    {
        var action = () => new BiographyTemplateLoader().Parse(
            "# Person\n{{ person.unknown }}\n",
            "/skabeloner/person.md");

        var exception = action.Should().Throw<BiographyTemplateException>().Which;
        exception.Message.Should().Contain("Ukendt skabelonfelt");
        exception.Message.Should().Contain("/skabeloner/person.md");
        exception.Message.Should().Contain("linje 2, kolonne 1");
        exception.Line.Should().Be(2);
        exception.Column.Should().Be(1);
    }

    [Fact]
    public void Parse_ShouldRejectFieldFromWrongLoopContext()
    {
        var action = () => new BiographyTemplateLoader().Parse(
            "{{#each events}}{{ title }}{{/each}}",
            "person.md");

        action.Should().Throw<BiographyTemplateException>()
            .WithMessage("*`title`*hændelse*");
    }

    [Fact]
    public void Parse_ShouldRejectEachOverScalarAndAcceptEveryPublicField()
    {
        var scalarLoop = () => new BiographyTemplateLoader().Parse(
            "{{#each person.fullName}}{{ recordId }}{{/each}}",
            "person.md");
        scalarLoop.Should().Throw<BiographyTemplateException>()
            .WithMessage("*kan ikke bruges som løkke*");
        var collectionMemberWithoutLoop = () => new BiographyTemplateLoader().Parse(
            "{{ events.date }}",
            "person.md");
        collectionMemberWithoutLoop.Should().Throw<BiographyTemplateException>()
            .WithMessage("*skal bruges som løkke*");

        var allFields = string.Join(
            "\n",
            BiographyTemplateContract.RootScalarPaths.Select(path => $"{{{{ {path} }}}}"),
            BiographyTemplateContract.CollectionExamples);
        var template = new BiographyTemplateLoader().Parse(allFields, "alle-felter.md");

        template.ContractVersion.Should().Be(BiographyTemplateContract.CurrentVersion);
    }

    [Fact]
    public void DefaultTemplate_ShouldRenderParentsDanishCategoriesAndDeterministically()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Parents.Add(new Person("@I2@") { FullName = "Bo Jensen" });
        person.Parents.Add(new Person("@I3@") { FullName = "Clara Jensen" });
        person.Events.Add(new GedcomEvent("BIRT") { Category = GedcomEventCategory.Birth, Date = "1900" });
        person.Events.Add(new GedcomEvent("DEAT") { Category = GedcomEventCategory.Death, Date = "1980" });
        var generator = new BiographyTemplateMarkdownGenerator();

        var first = generator.Generate(person);
        var second = generator.Generate(person);

        first.Should().Be(second);
        first.Should().Contain("Bo Jensen, Clara Jensen");
        first.Should().Contain("**Fødsel**");
        first.Should().Contain("**Død**");
        first.Should().NotContain("**Birth**");
    }

    [Fact]
    public void CreatedDate_ShouldBeRenderedByDefaultTemplateAndCanBeOmittedByCustomTemplate()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen", CreatedDate = "27 AUG 2025" };
        person.Events.Add(new GedcomEvent("BIRT") { Category = GedcomEventCategory.Birth, Date = "1900" });

        var standard = new BiographyTemplateMarkdownGenerator().Generate(person);
        var udenOprettetDato = new BiographyTemplateMarkdownGenerator("# {{ person.fullName }}\n").Generate(person);
        var medOprettetDato = new BiographyTemplateMarkdownGenerator("Oprettet: {{ person.createdDate }}\n").Generate(person);
        var utenCrea = new BiographyTemplateMarkdownGenerator().Generate(new Person("@I2@") { FullName = "Bent" });

        standard.Should().Contain("**Oprettet dato:** 27 AUG 2025");
        medOprettetDato.Should().Contain("Oprettet: 27 AUG 2025");
        udenOprettetDato.Should().NotContain("Oprettet");
        standard.Should().NotContain("CREA");
        utenCrea.Should().NotContain("Oprettet dato");
    }

    [Fact]
    public void PersonNotes_ShouldRenderInOrderFromPersonAndApprovedSnapshot()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Notes.Add("Første notat");
        person.Notes.Add("Andet *notat*");
        var snapshot = CanonicalBiographySnapshot.Create(person);
        var generator = new BiographyTemplateMarkdownGenerator();

        var directBody = BiographyDocumentParser.Parse(generator.Generate(person)).Body;
        var snapshotBody = BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot)).Body;
        var customBody = BiographyDocumentParser.Parse(
            new BiographyTemplateMarkdownGenerator("# {{ person.fullName }}\n").Generate(person)).Body;
        var withoutNotes = BiographyDocumentParser.Parse(
            generator.Generate(new Person("@I2@") { FullName = "Bent" })).Body;

        directBody.Should().Contain("- **Notat:** Første notat\n- **Notat:** Andet \\*notat\\*");
        snapshotBody.Should().Be(directBody);
        customBody.Should().NotContain("Første notat").And.NotContain("**Notat:**");
        withoutNotes.Should().NotContain("**Notat:**");
    }

    [Fact]
    public void PersonNotes_ShouldPreserveLineBreaksWithoutCreatingMarkdownBlocks()
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen", BirthDate = "1900" };
        person.Notes.Add("Første linje\n## Ikke en overskrift\n- Ikke en liste\n1. Heller ikke en liste");

        var generated = new BiographyTemplateMarkdownGenerator().Generate(person);
        var body = BiographyDocumentParser.Parse(generated).Body;

        body.Should().Contain(
            "**Notat:** Første linje  \n  \\## Ikke en overskrift  \n  \\- Ikke en liste  \n  1\\. Heller ikke en liste");
        body.Should().NotContain("\n## Ikke en overskrift");
    }

    [Fact]
    public void CanonicalSnapshot_ShouldOnlySerializeCreatedDateWhenPresent()
    {
        var uden = CanonicalBiographySnapshot.Create(new Person("@I1@") { FullName = "Anna" });
        var med = CanonicalBiographySnapshot.Create(new Person("@I1@") { FullName = "Anna", CreatedDate = "27 AUG 2025" });

        uden.ToCanonicalJson().Should().NotContain("createdDate");
        med.ToCanonicalJson().Should().Contain("\"createdDate\":\"27 AUG 2025\"");
        med.ComputeFingerprint().Should().NotBe(uden.ComputeFingerprint());
    }

    [Fact]
    public void MediemappeHarForrangOgSymbolskeLinksUdenforAfvises()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var media = Directory.CreateDirectory(Path.Combine(root, "medier")).FullName;
        var gedcom = Directory.CreateDirectory(Path.Combine(root, "gedcom")).FullName;
        var document = Directory.CreateDirectory(Path.Combine(root, "dokumenter")).FullName;
        var outside = Directory.CreateDirectory(Path.Combine(root, "andet")).FullName;
        try
        {
            File.WriteAllText(Path.Combine(media, "foto.jpg"), "medie");
            File.WriteAllText(Path.Combine(gedcom, "foto.jpg"), "gedcom");
            File.WriteAllText(Path.Combine(outside, "hemmelig.jpg"), "hemmelig");
            var resolver = new BiographyMediaResolver();
            resolver.Resolve("foto.jpg", gedcom, document, media).ResolvedPath.Should().Be(Path.Combine(media, "foto.jpg"));
            if (!OperatingSystem.IsWindows())
            {
                File.CreateSymbolicLink(Path.Combine(media, "link.jpg"), Path.Combine(outside, "hemmelig.jpg"));
                resolver.Resolve("link.jpg", gedcom, document, media).RelativePath.Should().BeNull();
            }
            resolver.Resolve("https://example.com/foto.jpg", gedcom, document, media).RelativePath.Should().BeNull();
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void MediaResolver_ShouldResolveRelativePathFromGedcomFolderToDocumentFolder()
    {
        using var area = new TemporaryDirectory();
        var gedcomFolder = Directory.CreateDirectory(Path.Combine(area.Path, "gedcom")).FullName;
        var outputFolder = Directory.CreateDirectory(Path.Combine(area.Path, "markdown")).FullName;
        var mediaFolder = Directory.CreateDirectory(Path.Combine(gedcomFolder, "medier")).FullName;
        var mediaPath = Path.Combine(mediaFolder, "anna foto.jpg");
        File.WriteAllText(mediaPath, "foto");

        var result = new BiographyMediaResolver().Resolve(
            "medier/anna foto.jpg",
            gedcomFolder,
            outputFolder);

        result.RelativePath.Should().Be("../gedcom/medier/anna%20foto.jpg");
        result.Diagnostic.Should().BeNull();
        result.RequiresApproval.Should().BeFalse();
    }

    [Fact]
    public void MediaResolver_ShouldWarnForMissingAndBlockPathOutsideAllowedAreas()
    {
        using var area = new TemporaryDirectory();
        var gedcomFolder = Directory.CreateDirectory(Path.Combine(area.Path, "gedcom")).FullName;
        var outputFolder = Directory.CreateDirectory(Path.Combine(area.Path, "markdown")).FullName;
        var outside = Path.Combine(area.Path, "udenfor.jpg");
        File.WriteAllText(outside, "foto");
        var resolver = new BiographyMediaResolver();

        var missing = resolver.Resolve("mangler.jpg", gedcomFolder, outputFolder);
        var escaped = resolver.Resolve(outside, gedcomFolder, outputFolder);

        missing.RelativePath.Should().BeNull();
        missing.Diagnostic.Should().Contain("findes ikke");
        missing.RequiresApproval.Should().BeFalse();
        escaped.RelativePath.Should().BeNull();
        escaped.Diagnostic.Should().Contain("uden for de tilladte lokale mapper");
        escaped.RequiresApproval.Should().BeTrue();
    }

    [Fact]
    public void MediaResolver_ShouldWarnWhenFileCannotBeRead()
    {
        var resolver = new BiographyMediaResolver(
            _ => true,
            _ => throw new UnauthorizedAccessException("Ingen adgang"));

        var result = resolver.Resolve(
            "foto.jpg",
            Path.GetTempPath(),
            Path.GetTempPath());

        result.RelativePath.Should().BeNull();
        result.Diagnostic.Should().Contain("kan ikke læses");
        result.RequiresApproval.Should().BeFalse();
    }

    [Fact]
    public void PreviewAndGenerator_ShouldUseSameValidatedRenderingPath()
    {
        using var area = new TemporaryDirectory();
        var templatePath = Path.Combine(area.Path, "person.md");
        new BiographyTemplateEditorSerializer().Save(templatePath, BiographyTemplateEditorModel.CreateDefault());
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Events.Add(new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Date = "1900",
        });

        var preview = new BiographyTemplatePreviewService().Render(templatePath, person);
        var generated = new BiographyTemplateMarkdownGenerator(new BiographyTemplateLoader().Load(templatePath).Source).Generate(person);

        preview.Should().Contain("# Anna Jensen").And.Contain("Fødsel").And.Contain("1900");
        BiographyDocumentParser.Parse(generated).Body.Should().Contain(
            preview[..preview.IndexOf("## Biografi", StringComparison.Ordinal)].TrimEnd());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
