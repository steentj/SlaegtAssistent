using System.Text;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Tests;

public class BiographyExportServiceTests
{
    private static readonly BiographyExportItem[] People = [new("@I1@", "Anna", "/tmp/CON.md", "tekst"), new("@I2@", "Bo", "/tmp/con.md", "tekst")];

    [Fact]
    public void FilnavneErSikreOgStabileVedSammenfald()
    {
        var first = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var second = BiographyExportService.PlanTargets(People.Reverse().ToArray(), BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        first.Select(x => x.Path.ToLowerInvariant()).Should().OnlyHaveUniqueItems();
        first.Select(x => x.Path).Should().BeEquivalentTo(second.Select(x => x.Path));
        first.Should().OnlyContain(x => Path.GetFileName(x.Path).StartsWith("person-", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("con ")]
    [InlineData("COM¹")]
    [InlineData("LPT9")]
    public void ReserveredeWindowsnavneFaarSikkertPraefiks(string name)
    {
        var item = new BiographyExportItem("a", "Person", "/tmp/" + name + ".md", "tekst");
        var target = BiographyExportService.PlanTargets([item], BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp").Single();
        Path.GetFileName(target.Path).Should().StartWith("person-");
    }

    [Fact]
    public void LangeUnicodefilnavneForkortesSikkert()
    {
        var item = new BiographyExportItem("a", "Person", "/tmp/" + new string('å', 200) + ".md", "tekst");
        var target = BiographyExportService.PlanTargets([item], BiographyExportMode.Separate, BiographyExportFormat.Docx, "/tmp").Single();
        Encoding.UTF8.GetByteCount(Path.GetFileName(target.Path)).Should().BeLessThanOrEqualTo(165);
        Path.GetFileName(target.Path).Should().EndWith(".docx");
    }

    [Fact]
    public async Task KonverteringsfejlGemmerIngenFiler()
    {
        var converter = new Converter { FailAt = 2 };
        var writer = new Writer();
        var service = new BiographyExportService(converter, writer);
        var targets = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var result = await service.ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None);
        result.Saved.Should().BeEmpty(); result.NotSaved.Should().HaveCount(2); result.Errors.Should().ContainSingle();
        writer.Paths.Should().BeEmpty();
    }

    [Fact]
    public async Task FejlVedAndenKonverteringEfterladerEksisterendeMaalUændrede()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var targets = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, directory);
            foreach (var target in targets) await File.WriteAllTextAsync(target.Path, "oprindelig fil");
            var result = await new BiographyExportService(new Converter { FailAt = 2 }, new AtomicFileWriter())
                .ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None, targets.Select(target => target.Path).ToArray());
            result.Saved.Should().BeEmpty();
            result.NotSaved.Should().BeEquivalentTo(targets.Select(target => target.Path));
            result.Errors.Should().ContainSingle();
            foreach (var target in targets) (await File.ReadAllTextAsync(target.Path)).Should().Be("oprindelig fil");
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task AnnulleringUnderKonverteringGemmerIngenMaal()
    {
        using var cancellation = new CancellationTokenSource();
        var converter = new Converter { CancelAfterFirst = cancellation };
        var writer = new Writer();
        var targets = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var result = await new BiographyExportService(converter, writer)
            .ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, cancellation.Token);
        result.Cancelled.Should().BeTrue();
        result.Saved.Should().BeEmpty();
        result.NotSaved.Should().BeEquivalentTo(targets.Select(target => target.Path));
        writer.Paths.Should().BeEmpty();
    }

    [Fact]
    public async Task SkrivefejlViserPraecistDelresultat()
    {
        var converter = new Converter();
        var writer = new Writer { FailAt = 2 };
        var targets = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var result = await new BiographyExportService(converter, writer).ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None);
        converter.Calls.Should().Be(2);
        result.Saved.Should().Equal(targets[0].Path); result.NotSaved.Should().Equal(targets[1].Path); result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task FejlVedTredjeAtomiskePubliceringBevarerPraeciseResultater()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            BiographyExportItem[] people =
            [
                new("a", "Anna", Path.Combine(directory, "anna.md"), "Annas tekst"),
                new("b", "Bo", Path.Combine(directory, "bo.md"), "Bos tekst"),
                new("c", "Carl", Path.Combine(directory, "carl.md"), "Carls tekst")
            ];
            var targets = BiographyExportService.PlanTargets(people, BiographyExportMode.Separate, BiographyExportFormat.Pdf, directory);
            var stageCount = 0;
            var writer = new AtomicFileWriter(stage =>
            {
                if (stage == AtomicWriteStage.BeforeDestinationReplace && ++stageCount == 3)
                    throw new IOException("Simuleret diskfejl ved tredje fil.");
            });
            var result = await new BiographyExportService(new Converter(), writer)
                .ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None);
            result.Saved.Should().Equal(targets[0].Path, targets[1].Path);
            result.NotSaved.Should().Equal(targets[2].Path);
            result.Errors.Should().ContainSingle();
            (await File.ReadAllTextAsync(targets[0].Path)).Should().Be("Annas tekst");
            (await File.ReadAllTextAsync(targets[1].Path)).Should().Be("Bos tekst");
            File.Exists(targets[2].Path).Should().BeFalse();
            Directory.EnumerateFiles(directory).Should().HaveCount(2);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task NyMaalfilEfterGodkendelseOverskrivesIkke()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var target = BiographyExportService.PlanTargets([People[0]], BiographyExportMode.Separate, BiographyExportFormat.Pdf, directory);
            await File.WriteAllTextAsync(target[0].Path, "anden fil");
            var writer = new Writer();
            var result = await new BiographyExportService(new Converter(), writer).ExportAsync(target, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None);
            result.Saved.Should().BeEmpty();
            result.Errors.Should().ContainSingle().Which.Should().Contain("ikke godkendt");
            (await File.ReadAllTextAsync(target[0].Path)).Should().Be("anden fil");
            writer.Paths.Should().BeEmpty();
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task AnnulleringUnderPubliceringAngiverBeggeLister()
    {
        using var token = new CancellationTokenSource();
        var converter = new Converter();
        var writer = new Writer { CancelAfterFirst = token };
        var targets = BiographyExportService.PlanTargets(People, BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var result = await new BiographyExportService(converter, writer).ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, token.Token);
        result.Cancelled.Should().BeTrue();
        result.Saved.Should().Equal(targets[0].Path);
        result.NotSaved.Should().Equal(targets[1].Path);
    }

    [Fact]
    public async Task NyeAdvarslerForhindrerPublicering()
    {
        var targets = BiographyExportService.PlanTargets([People[0]], BiographyExportMode.Separate, BiographyExportFormat.Pdf, "/tmp");
        var writer = new Writer();
        var result = await new BiographyExportService(new Converter { Warning = "Nyt billede mangler." }, writer)
            .ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None, [], []);
        result.Saved.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Should().Contain("advarsler ændrede sig");
        writer.Paths.Should().BeEmpty();
    }

    [Fact]
    public async Task AendretBilledeEfterGennemgangForhindrerPublicering()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var imagePath = Path.Combine(directory, "foto.png");
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC");
            await File.WriteAllBytesAsync(imagePath, png);
            BiographyExportItem[] items = [new("a", "Anna", Path.Combine(directory, "a.md"), "![Foto](foto.png)")];
            var preflight = new BiographyExportPreflightService();
            var approved = preflight.Prepare(items, BiographyExportMode.Separate, [], CancellationToken.None);
            await File.WriteAllBytesAsync(imagePath, [.. png, 1]);
            var changed = preflight.Prepare(items, BiographyExportMode.Separate, [], CancellationToken.None);
            changed.Fingerprints.Should().NotEqual(approved.Fingerprints);
            var targets = BiographyExportService.PlanTargets(items, BiographyExportMode.Separate, BiographyExportFormat.Pdf, directory);
            var writer = new Writer();
            var outcome = await new BiographyExportService(new Converter { Fingerprint = changed.Fingerprints[0] }, writer)
                .ExportAsync(targets, BiographyExportMode.Separate, BiographyExportFormat.Pdf, [], null, CancellationToken.None, [], approved.Warnings, approved.Fingerprints);
            outcome.Saved.Should().BeEmpty();
            outcome.Errors.Should().ContainSingle().Which.Should().Contain("ændret efter gennemgangen");
            writer.Paths.Should().BeEmpty();
        }
        finally { Directory.Delete(directory, true); }
    }

    private sealed class Converter : IBiographyConverter
    {
        public int Calls { get; private set; }
        public int FailAt { get; init; }
        public string? Warning { get; init; }
        public string? Fingerprint { get; init; }
        public CancellationTokenSource? CancelAfterFirst { get; init; }
        public Task<BiographyConversionResult> RenderBiographyAsync(IReadOnlyList<BiographyExportItem> items, BiographyExportMode mode, BiographyExportFormat format, IReadOnlyCollection<string> roots, CancellationToken token)
        {
            if (++Calls == FailAt) throw new IOException("Konvertering mislykkedes.");
            if (Calls == 1) CancelAfterFirst?.Cancel();
            return Task.FromResult(new BiographyConversionResult(Encoding.UTF8.GetBytes(items[0].Body), Warning is null ? [] : [Warning], Fingerprint));
        }
    }
    private sealed class Writer : INoOverwriteAtomicFileWriter
    {
        public int FailAt { get; init; }
        public CancellationTokenSource? CancelAfterFirst { get; init; }
        public List<string> Paths { get; } = [];
        public void WriteBytes(string path, byte[] content)
        {
            if (Paths.Count + 1 == FailAt) throw new IOException("Disken er fuld.");
            Paths.Add(path);
            CancelAfterFirst?.Cancel();
        }
        public void WriteNewBytes(string path, byte[] content) => WriteBytes(path, content);
        public void WriteText(string path, string content, Encoding encoding) => throw new NotSupportedException();
    }
}
