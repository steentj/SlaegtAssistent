using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Tests;

public sealed class MarkdownGraphLinkExtractorTests
{
    [Fact]
    public void FinderPersonlinksMenIkkeBillederKodeOgHtml()
    {
        const string markdown = "[Anna](anna.md)\n\n[Anna igen][a]\n\n[a]: anna.md\n\n![foto](foto.md)\n\n`[kode](kode.md)`\n\n```\n[blok](blok.md)\n```\n\n<a href=\"html.md\">HTML</a>";
        var visited = new List<string>();
        PersonDocumentLink? Resolve(string url)
        {
            visited.Add(url);
            return url == "anna.md" ? new PersonDocumentLink("A", "Anna", "/tmp/anna.md") : null;
        }

        var links = new MarkdownGraphLinkExtractor().Extract("B", markdown, Resolve);

        links.Should().ContainSingle().Which.Should().Be(new GraphRelation("B", "A", GraphRelationKind.MarkdownLink, Origin: "B"));
        visited.Should().OnlyContain(url => url == "anna.md");
    }
}
