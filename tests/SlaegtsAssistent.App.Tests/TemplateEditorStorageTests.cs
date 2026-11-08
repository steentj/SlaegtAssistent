using System.Text;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorStorageTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public TemplateEditorStorageTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Save_ShouldReopenTheSameModelAndCompileForProduction()
    {
        var path = Path.Combine(_directory, "skabelon.json");
        var model = BiographyTemplateEditorModel.CreateDefault();
        var storage = new TemplateEditorStorageService();

        storage.Save(path, model);

        storage.Load(path).Should().BeEquivalentTo(model, options => options.WithStrictOrdering());
        new BiographyTemplateLoader().Load(path).Source.Should()
            .Be(new BiographyTemplateEditorCompiler().Compile(model));
        Directory.GetFiles(_directory).Should().Equal(path);
    }

    [Theory]
    [InlineData(AtomicWriteStage.BeforeTemporaryFileCreation)]
    [InlineData(AtomicWriteStage.AfterTemporaryFileFlush)]
    [InlineData(AtomicWriteStage.BeforeDestinationReplace)]
    public void FailedWrite_ShouldPreserveOriginalBytes(AtomicWriteStage failureStage)
    {
        var path = Path.Combine(_directory, "skabelon.json");
        File.WriteAllText(path, "Oprindelig fil");
        var storage = new TemplateEditorStorageService(new AtomicFileWriter(stage =>
        {
            if (stage == failureStage)
            {
                throw new IOException("Afbrudt skrivning");
            }
        }));

        var save = () => storage.Save(path, BiographyTemplateEditorModel.CreateDefault());

        save.Should().Throw<IOException>();
        File.ReadAllText(path).Should().Be("Oprindelig fil");
        Directory.GetFiles(_directory).Should().Equal(path);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidReload_ShouldNotReplaceOrCreateDestination(bool destinationExists)
    {
        var path = Path.Combine(_directory, "skabelon.json");
        if (destinationExists)
        {
            File.WriteAllText(path, "Oprindelig fil");
        }
        var storage = new TemplateEditorStorageService(new CorruptingWriter());

        var save = () => storage.Save(path, BiographyTemplateEditorModel.CreateDefault());

        save.Should().Throw<BiographyTemplateException>();
        if (destinationExists)
        {
            File.ReadAllText(path).Should().Be("Oprindelig fil");
        }
        else
        {
            File.Exists(path).Should().BeFalse();
        }
        Directory.GetFiles(_directory).Should().HaveCount(destinationExists ? 1 : 0);
    }

    [Fact]
    public void LegacyTemplate_ShouldExplainHowToStartAgain()
    {
        var path = Path.Combine(_directory, "tidligere.md");
        File.WriteAllText(path, "# {{ person.fullName }}");

        var load = () => new TemplateEditorStorageService().Load(path);

        load.Should().Throw<BiographyTemplateException>().WithMessage("*tidligere*standardskabelonen*");
    }

    [Theory]
    [InlineData("{\"version\":99,\"blocks\":[]}")]
    [InlineData("{\"version\":1,\"blocks\":[null]}")]
    [InlineData("{\"version\":1,\"blocks\":null}")]
    [InlineData("{ugyldig")]
    public void InvalidModel_ShouldProduceControlledErrorWithFilePath(string content)
    {
        var path = Path.Combine(_directory, "ugyldig.slaegtskabelon");
        File.WriteAllText(path, content);

        var load = () => new TemplateEditorStorageService().Load(path);

        load.Should().Throw<BiographyTemplateException>().Which.FilePath.Should().Be(path);
    }

    [Fact]
    public void ValidButDifferentReload_ShouldPreservePreviousFile()
    {
        var path = Path.Combine(_directory, "skabelon.slaegtskabelon");
        File.WriteAllText(path, "Oprindelig fil");
        var storage = new TemplateEditorStorageService(new DifferentModelWriter());

        var save = () => storage.Save(path, BiographyTemplateEditorModel.CreateDefault());

        save.Should().Throw<IOException>().WithMessage("*svarer ikke*");
        File.ReadAllText(path).Should().Be("Oprindelig fil");
        Directory.GetFiles(_directory).Should().Equal(path);
    }

    public void Dispose() => Directory.Delete(_directory, true);

    private sealed class CorruptingWriter : IAtomicFileWriter
    {
        public void WriteText(string path, string content, Encoding encoding) =>
            File.WriteAllText(path, "{ugyldig", encoding);

        public void WriteBytes(string path, byte[] content) => throw new NotSupportedException();
    }

    private sealed class DifferentModelWriter : IAtomicFileWriter
    {
        public void WriteText(string path, string content, Encoding encoding) =>
            File.WriteAllText(path, "{\"version\":1,\"blocks\":[]}", encoding);

        public void WriteBytes(string path, byte[] content) => throw new NotSupportedException();
    }
}
