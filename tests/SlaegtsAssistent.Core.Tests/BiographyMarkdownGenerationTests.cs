using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public class BiographyMarkdownGenerationTests
{
    private readonly IBiographyMarkdownGenerator _generator = new BiographyMarkdownGenerator();

    [Fact]
    public void Generate_MinimalPerson_ProducesHeadingFactsSectionAndBiographyPlaceholder()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen"
        };

        var markdown = _generator.Generate(person);

        markdown.Should().Be(
            "# Anna Jensen\n\n" +
            "## Fakta\n\n" +
            "## Biografi\n" +
            "_Skriv den fulde livshistorie her._\n");
    }

    [Fact]
    public void Generate_WhenBirthDateAndPlaceAreSet_RendersBirthFact()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen",
            BirthDate = "12 MAR 1900",
            BirthPlace = "Aarhus, Denmark"
        };

        var markdown = _generator.Generate(person);

        markdown.Should().Contain("- **Født:** 12 MAR 1900 i Aarhus, Denmark");
    }

    [Fact]
    public void Generate_WhenDeathDataIsAbsent_OmitsDeathFact()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen",
            BirthDate = "12 MAR 1900"
        };

        var markdown = _generator.Generate(person);

        markdown.Should().NotContain("**Død:**");
    }

    [Fact]
    public void Generate_WhenDeathDataIsSet_RendersDeathFact()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen",
            DeathDate = "02 APR 1980",
            DeathPlace = "Odense, Denmark"
        };

        var markdown = _generator.Generate(person);

        markdown.Should().Contain("- **Død:** 02 APR 1980 i Odense, Denmark");
    }

    [Fact]
    public void Generate_WhenParentsAreAbsent_OmitsParentsFact()
    {
        var person = new Person("@I1@")
        {
            FullName = "Anna Jensen"
        };

        var markdown = _generator.Generate(person);

        markdown.Should().NotContain("**Forældre:**");
    }

    [Fact]
    public void Generate_WhenParentsArePresent_RendersParentsFact()
    {
        var child = new Person("@I1@")
        {
            FullName = "Anna Jensen"
        };

        child.Parents.Add(new Person("@I2@") { FullName = "Jens Jensen" });
        child.Parents.Add(new Person("@I3@") { FullName = "Maren Jensen" });

        var markdown = _generator.Generate(child);

        markdown.Should().Contain("- **Forældre:** Jens Jensen, Maren Jensen");
    }

    [Fact]
    public void Generate_WithEquivalentInput_IsDeterministic()
    {
        var firstPerson = CreateEquivalentPerson();
        var secondPerson = CreateEquivalentPerson();

        var first = _generator.Generate(firstPerson);
        var second = _generator.Generate(secondPerson);

        first.Should().Be(second);
    }

    [Fact]
    public void FileNameGenerator_WithSamePerson_IsStableAcrossCalls()
    {
        var person = new Person("@I1@")
        {
            FullName = "Jens Hansen"
        };

        var first = BiographyFileNameGenerator.Generate(person);
        var second = BiographyFileNameGenerator.Generate(person);

        first.Should().Be(second);
    }

    [Fact]
    public void FileNameGenerator_WithSameNameAndDifferentRecordIds_ProducesUniqueNames()
    {
        var firstPerson = new Person("@I1@") { FullName = "Jens Hansen" };
        var secondPerson = new Person("@I2@") { FullName = "Jens Hansen" };

        var first = BiographyFileNameGenerator.Generate(firstPerson);
        var second = BiographyFileNameGenerator.Generate(secondPerson);

        first.Should().NotBe(second);
        first.Should().StartWith("jens-hansen-");
        second.Should().StartWith("jens-hansen-");
    }

    [Fact]
    public void FileNameGenerator_StripsDiacriticsAndInvalidCharacters()
    {
        var person = new Person("@I1@")
        {
            FullName = "Jens Hånsen /:<>?"
        };

        var fileName = BiographyFileNameGenerator.Generate(person);

        fileName.Should().MatchRegex("^[a-z0-9-]+-[a-f0-9]{4}\\.md$");
    }

    [Fact]
    public void WriteAll_WritesOneFilePerPersonWithExpectedContent()
    {
        var tree = new GedcomLoader().Load(FixturePath("two-generations.ged"));
        var outputDirectory = TempDirectory();
        var writer = new BiographyFileWriter(_generator);

        try
        {
            writer.WriteAll(tree, outputDirectory);

            var files = Directory.GetFiles(outputDirectory, "*.md");
            files.Should().HaveCount(tree.People.Count);

            foreach (var person in tree.People)
            {
                var fileName = BiographyFileNameGenerator.Generate(person);
                var path = Path.Combine(outputDirectory, fileName);

                File.Exists(path).Should().BeTrue();
                File.ReadAllText(path).Should().Be(_generator.Generate(person));
            }
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void WriteAll_WhenFileAlreadyExists_DoesNotOverwrite()
    {
        var tree = new GedcomLoader().Load(FixturePath("single-person.ged"));
        var person = tree.People.Single();
        var outputDirectory = TempDirectory();
        var writer = new BiographyFileWriter(_generator);

        try
        {
            Directory.CreateDirectory(outputDirectory);
            var existingPath = Path.Combine(outputDirectory, BiographyFileNameGenerator.Generate(person));
            File.WriteAllText(existingPath, "Eksisterende indhold");

            writer.WriteAll(tree, outputDirectory);

            File.ReadAllText(existingPath).Should().Be("Eksisterende indhold");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static Person CreateEquivalentPerson()
    {
        var child = new Person("@I1@")
        {
            FullName = "Anna Jensen",
            BirthDate = "12 MAR 1900",
            BirthPlace = "Aarhus, Denmark",
            DeathDate = "02 APR 1980",
            DeathPlace = "Odense, Denmark"
        };

        child.Parents.Add(new Person("@I2@") { FullName = "Jens Jensen" });
        child.Parents.Add(new Person("@I3@") { FullName = "Maren Jensen" });

        return child;
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

    private static string TempDirectory()
    {
        return Path.Combine(Path.GetTempPath(), $"SlaegtsAssistent-{Guid.NewGuid():N}");
    }
}
