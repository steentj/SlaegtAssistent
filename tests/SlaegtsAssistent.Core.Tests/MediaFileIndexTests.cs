using FluentAssertions;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.Core.Tests;

public sealed class MediaFileIndexTests
{
    [Fact]
    public async Task UfuldstaendigtIndeksKanIkkeBekraefteEtMatch()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var index = await MediaFileIndex.BuildAsync(missing, StringComparer.Ordinal);
        var match = index.Find("foto.jpg");
        match.Candidates.Should().BeEmpty();
        match.Diagnostic.Should().Contain("ikke gennemgås fuldstændigt");
    }

    [Fact]
    public async Task ResolverBrugerIndeksTilFlyttedeFilerOgAfviserDubletter()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var media = Directory.CreateDirectory(Path.Combine(root, "medier")).FullName;
        var document = Directory.CreateDirectory(Path.Combine(root, "dokumenter")).FullName;
        var first = Directory.CreateDirectory(Path.Combine(media, "ny", "familie")).FullName;
        var second = Directory.CreateDirectory(Path.Combine(media, "anden")).FullName;
        try
        {
            File.WriteAllText(Path.Combine(first, "foto.jpg"), "a");
            File.WriteAllText(Path.Combine(second, "foto.jpg"), "b");
            var index = await MediaFileIndex.BuildAsync(media, StringComparer.Ordinal);
            var resolver = new BiographyMediaResolver();
            var found = resolver.Resolve(@"C:\gammel\familie\foto.jpg", null, document, media, index);
            found.ResolvedPath.Should().Be(Path.Combine(first, "foto.jpg"));
            found.RelativePath.Should().NotBeNull();
            var ambiguous = resolver.Resolve("foto.jpg", null, document, media, index);
            ambiguous.RelativePath.Should().BeNull();
            ambiguous.Candidates.Should().HaveCount(2);
            ambiguous.Diagnostic.Should().Contain("tvetydig").And.Contain("foto.jpg");
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task FinderLaengsteStiafslutningOgAfviserTvetydigtFilnavn()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var first = Path.Combine(root, "a", "familie");
        var second = Path.Combine(root, "b", "andet");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        try
        {
            File.WriteAllText(Path.Combine(first, "foto.jpg"), "a");
            File.WriteAllText(Path.Combine(second, "foto.jpg"), "b");
            var index = await MediaFileIndex.BuildAsync(root, StringComparer.Ordinal);
            index.Find(@"C:\gammel\familie\foto.jpg").Candidates.Should().Equal(Path.Combine(first, "foto.jpg"));
            index.Find("foto.jpg").Candidates.Should().HaveCount(2);
            index.Find("foto.jpg").IsAmbiguous.Should().BeTrue();
            index.Find("ukendt.jpg").Candidates.Should().BeEmpty();
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task IndeksetFoelgerIkkeSymbolskeMapperOgKanAfbrydes()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var media = Directory.CreateDirectory(Path.Combine(root, "medier")).FullName;
        var outside = Directory.CreateDirectory(Path.Combine(root, "udenfor")).FullName;
        try
        {
            File.WriteAllText(Path.Combine(outside, "hemmelig.jpg"), "x");
            if (!OperatingSystem.IsWindows()) Directory.CreateSymbolicLink(Path.Combine(media, "link"), outside);
            var index = await MediaFileIndex.BuildAsync(media, StringComparer.Ordinal);
            index.Find("hemmelig.jpg").Candidates.Should().BeEmpty();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var action = () => MediaFileIndex.BuildAsync(media, StringComparer.Ordinal, cancellation.Token);
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task FilnavnematchRespektererValgtSammenligning()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "Foto.jpg"), "x");
            var sensitive = await MediaFileIndex.BuildAsync(root, StringComparer.Ordinal);
            var insensitive = await MediaFileIndex.BuildAsync(root, StringComparer.OrdinalIgnoreCase);
            sensitive.Find("foto.jpg").Candidates.Should().BeEmpty();
            insensitive.Find("foto.jpg").Candidates.Should().ContainSingle();
        }
        finally { Directory.Delete(root, true); }
    }
}
