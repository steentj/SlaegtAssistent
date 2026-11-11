using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Services;

public sealed class MarkdownGraphLinkExtractor
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables().UseEmphasisExtras().Build();

    public IReadOnlyList<GraphRelation> Extract(string sourceId, string markdown, Func<string, PersonDocumentLink?> resolve)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(resolve);
        var document = Markdown.Parse(markdown, Pipeline);
        return document.Descendants<LinkInline>()
            .Where(link => !link.IsImage)
            .Select(link => link.GetDynamicUrl?.Invoke() ?? link.Url)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => resolve(url!))
            .Where(link => link is not null)
            .Select(link => new GraphRelation(sourceId, link!.RecordId, GraphRelationKind.MarkdownLink, Origin: sourceId))
            .Distinct().ToArray();
    }
}
