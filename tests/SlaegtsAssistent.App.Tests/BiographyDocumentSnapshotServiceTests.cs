using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public class BiographyDocumentSnapshotServiceTests
{
    [Fact]
    public void BrugerUgemtEditorUdenAtGemmeOgSortererDansk()
    {
        var store = new Store();
        var editor = new EditorViewModel("a.md", store) { MarkdownText = "Ugemt tekst" };
        var result = new BiographyDocumentSnapshotService(store).Create(
            ["a", "b"],
            [new("a", "Åse", "a.md"), new("b", "Anders", "b.md")],
            new Dictionary<string, EditorViewModel> { ["a.md"] = editor });

        result.Errors.Should().BeEmpty();
        result.Items.Select(x => x.DisplayName).Should().Equal("Anders", "Åse");
        result.Items.Single(x => x.RecordId == "a").Body.Should().Be("Ugemt tekst");
        editor.IsDirty.Should().BeTrue();
        store.Writes.Should().Be(0);
    }

    [Fact]
    public void FejlEllerTvetydighedForhindrerHeleSnapshot()
    {
        var result = new BiographyDocumentSnapshotService(new Store()).Create(
            ["a", "b"],
            [new("a", "Anna", "a.md"), new("b", "Bo", "b.md"), new("b", "Bo", "c.md")],
            new Dictionary<string, EditorViewModel>());

        result.Items.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.DisplayName.Should().Be("Bo");
    }

    [Fact]
    public void AfviserOverHundredeIndenFilerLaeses()
    {
        var store = new Store();
        var result = new BiographyDocumentSnapshotService(store).Create(
            Enumerable.Range(0, 101).Select(i => i.ToString()), [], new Dictionary<string, EditorViewModel>());
        result.Errors.Should().ContainSingle();
        store.Reads.Should().Be(0);
    }

    [Fact]
    public void TilladerHundredeOgAfviserTomtUdvalg()
    {
        var service = new BiographyDocumentSnapshotService(new Store());
        var documents = Enumerable.Range(0, 100).Select(i => new MarkdownDocumentInfo(i.ToString(), "Person " + i, $"/tmp/person-{i}.md")).ToArray();
        service.Create(documents.Select(item => item.RecordId), documents, new Dictionary<string, EditorViewModel>()).Items.Should().HaveCount(100);
        service.Create([], documents, new Dictionary<string, EditorViewModel>()).Errors.Should().ContainSingle();
    }

    private sealed class Store : IMarkdownFileStore
    {
        public int Reads { get; private set; }
        public int Writes { get; private set; }
        public string Read(string path) { Reads++; return "Gemt tekst"; }
        public void Write(string path, string content) => Writes++;
    }
}
