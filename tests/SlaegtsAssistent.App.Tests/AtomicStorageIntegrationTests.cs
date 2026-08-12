using System.Text;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public sealed class AtomicStorageIntegrationTests
{
    [Fact]
    public void MarkdownStore_ShouldUseAtomicWriter()
    {
        var writer = new RecordingAtomicFileWriter();
        var store = new FileSystemMarkdownFileStore(writer);

        store.Write("/tmp/person.md", "indhold");

        writer.TextWrites.Should().ContainSingle();
        writer.TextWrites[0].Path.Should().Be("/tmp/person.md");
        writer.TextWrites[0].Content.Should().Be("indhold");
    }

    [Fact]
    public void SettingsService_ShouldUseAtomicWriter()
    {
        var writer = new RecordingAtomicFileWriter();
        var settingsPath = Path.Combine(Path.GetTempPath(), "test-settings.json");
        var service = new JsonApplicationSettingsService(settingsPath, writer);

        service.Save(new AppSettings { Theme = ThemePreference.Dark });

        writer.TextWrites.Should().ContainSingle();
        writer.TextWrites[0].Path.Should().Be(settingsPath);
        writer.TextWrites[0].Content.Should().Contain("Dark");
    }

    [Fact]
    public void SnapshotStore_ShouldUseAtomicWriterForSourceAndManifest()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.WriteAllText(sourcePath, "0 HEAD\n0 @I1@ INDI\n1 NAME Anna /Jensen/\n0 TRLR\n");
        var writer = new RecordingAtomicFileWriter(writeThrough: true);

        try
        {
            var familyTree = new GedcomLoader().Load(sourcePath);
            var store = new FileSystemGedcomSnapshotStore(writer);

            store.Save(outputDirectory, sourcePath, familyTree);

            writer.ByteWrites.Should().ContainSingle();
            writer.TextWrites.Should().ContainSingle(write => write.Path.EndsWith("manifest.json"));
            store.Load(outputDirectory).Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
            }

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private sealed class RecordingAtomicFileWriter(bool writeThrough = false) : IAtomicFileWriter
    {
        private readonly AtomicFileWriter _writer = new();

        public List<(string Path, string Content)> TextWrites { get; } = [];

        public List<(string Path, byte[] Content)> ByteWrites { get; } = [];

        public void WriteText(string path, string content, Encoding encoding)
        {
            TextWrites.Add((path, content));
            if (writeThrough)
            {
                _writer.WriteText(path, content, encoding);
            }
        }

        public void WriteBytes(string path, byte[] content)
        {
            ByteWrites.Add((path, content));
            if (writeThrough)
            {
                _writer.WriteBytes(path, content);
            }
        }
    }
}
