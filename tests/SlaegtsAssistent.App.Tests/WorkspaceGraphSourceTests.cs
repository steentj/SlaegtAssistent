using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class WorkspaceGraphSourceTests
{
    [Fact]
    public void SnapshotBrugerUgemtBufferOgSamtligeDokumenter()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var first = Path.Combine(directory, "anna.md");
            var second = Path.Combine(directory, "bo.md");
            static string Document(string id, string name) => BiographyDocumentSerializer.Serialize(
                new BiographyDocumentMetadata(3, id, name, new BiographyFactsSnapshot(name, null, null, null, null, null, [])), "# " + name);
            File.WriteAllText(first, Document("A", "Anna"));
            File.WriteAllText(second, Document("B", "Bo"));
            var files = new FileSystemMarkdownFileStore();
            var editor = new EditorViewModel(first, files);
            editor.Load();
            editor.MarkdownText = "# Anna\n\n[Bo](bo.md)";
            var graph = new WorkspaceGraphSource(files).CreateSnapshot(
                [new("A", "Anna", first), new("B", "Bo", second)],
                new Dictionary<string, EditorViewModel> { [first] = editor }, null);

            graph.Nodes.Select(node => node.RecordId).Should().BeEquivalentTo("A", "B");
            graph.Neighbours["A"].Should().Equal("B");
            editor.IsDirty.Should().BeTrue();
            File.ReadAllText(first).Should().NotContain("[Bo]");
        }
        finally { Directory.Delete(directory, true); }
    }
}
