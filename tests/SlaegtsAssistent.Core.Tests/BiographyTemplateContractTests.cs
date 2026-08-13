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
        File.WriteAllText(templatePath, "# {{ person.fullName }}\n{{#each events}}{{ category }}: {{ date }}\n{{/each}}");
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        person.Events.Add(new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Date = "1900",
        });

        var preview = new BiographyTemplatePreviewService().Render(templatePath, person);
        var generated = new BiographyTemplateMarkdownGenerator(File.ReadAllText(templatePath)).Generate(person);

        preview.Should().Be("# Anna Jensen\nFødsel: 1900\n");
        BiographyDocumentParser.Parse(generated).Body.Should().Contain(preview);
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
