using System.Text;
using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class AtomicFileWriterTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    [Theory]
    [InlineData(AtomicWriteStage.BeforeTemporaryFileCreation)]
    [InlineData(AtomicWriteStage.AfterTemporaryFileFlush)]
    [InlineData(AtomicWriteStage.BeforeDestinationReplace)]
    public void WriteText_WhenAWriteStageFails_PreservesExistingFile(
        AtomicWriteStage failingStage)
    {
        var directory = CreateTemporaryDirectory();
        var path = Path.Combine(directory, "person.md");
        File.WriteAllText(path, "brugerens oprindelige tekst", Utf8WithoutBom);
        var originalBytes = File.ReadAllBytes(path);
        var writer = new AtomicFileWriter(stage =>
        {
            if (stage == failingStage)
            {
                throw new IOException("Simuleret skrivefejl.");
            }
        });

        try
        {
            var action = () => writer.WriteText(path, "ny tekst", Utf8WithoutBom);

            action.Should().Throw<AtomicFileWriteException>()
                .WithMessage($"*{path}*tidligere version*");
            File.ReadAllBytes(path).Should().Equal(originalBytes);
            Directory.EnumerateFiles(directory).Should().ContainSingle().Which.Should().Be(path);
        }
        finally
        {
            DeleteDirectoryIfExists(directory);
        }
    }

    [Fact]
    public void WriteText_WhenCommitSucceeds_ReplacesFileWithoutTemporaryRemainders()
    {
        var directory = CreateTemporaryDirectory();
        var path = Path.Combine(directory, "person.md");
        File.WriteAllText(path, "gammel tekst", Utf8WithoutBom);
        var writer = new AtomicFileWriter();

        try
        {
            writer.WriteText(path, "færdig tekst", Utf8WithoutBom);

            File.ReadAllText(path).Should().Be("færdig tekst");
            File.ReadAllBytes(path).Should().Equal(Utf8WithoutBom.GetBytes("færdig tekst"));
            Directory.EnumerateFiles(directory).Should().ContainSingle().Which.Should().Be(path);
        }
        finally
        {
            DeleteDirectoryIfExists(directory);
        }
    }

    [Fact]
    public void WriteBytes_WhenDestinationDoesNotExist_CreatesCompleteFile()
    {
        var directory = CreateTemporaryDirectory();
        var path = Path.Combine(directory, "source.gedcom");
        var content = Encoding.UTF8.GetBytes("0 HEAD\n0 TRLR\n");
        var writer = new AtomicFileWriter();

        try
        {
            writer.WriteBytes(path, content);

            File.ReadAllBytes(path).Should().Equal(content);
            Directory.EnumerateFiles(directory).Should().ContainSingle().Which.Should().Be(path);
        }
        finally
        {
            DeleteDirectoryIfExists(directory);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
