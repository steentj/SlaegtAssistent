using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaWorkspaceIndexTests
{
    [Fact]
    public async Task IndeksGenbrugesIndtilMappenOpdateres()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var indexes = new MediaWorkspaceIndex();
            var first = await indexes.GetAsync(root, CancellationToken.None);
            var second = await indexes.GetAsync(root, CancellationToken.None);
            second.Should().BeSameAs(first);
            File.WriteAllText(Path.Combine(root, "nyt.jpg"), "foto");
            first!.Find("nyt.jpg").Candidates.Should().BeEmpty();
            indexes.Invalidate();
            var updated = await indexes.GetAsync(root, CancellationToken.None);
            updated.Should().NotBeSameAs(first);
            updated!.Find("nyt.jpg").Candidates.Should().ContainSingle();
        }
        finally { Directory.Delete(root, true); }
    }
}
