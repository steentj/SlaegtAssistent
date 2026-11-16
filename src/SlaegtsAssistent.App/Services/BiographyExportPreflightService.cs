using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Services;

public sealed record BiographyExportPreflightResult(IReadOnlyList<string> Warnings, IReadOnlyList<string> Fingerprints);

public sealed class BiographyExportPreflightService
{
    public BiographyExportPreflightResult Prepare(IReadOnlyList<BiographyExportItem> items,
        BiographyExportMode mode, IReadOnlyCollection<string> allowedRoots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(allowedRoots);
        if (items.Count is 0 or > 100) throw new ArgumentException("Vælg mellem 1 og 100 biografier.");
        if (mode is not (BiographyExportMode.Combined or BiographyExportMode.Separate))
            throw new ArgumentOutOfRangeException(nameof(mode));
        using var job = new ExportJobDirectory(Guid.NewGuid().ToString("N"));
        var collector = new ExportResourceCollector(Path.Combine(job.Path, "assets"), allowedRoots);
        var builder = new BiographyExportHtmlBuilder();
        var warnings = new List<string>();
        var fingerprints = new List<string>();
        void Add(IReadOnlyList<BiographyExportItem> documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var html = builder.Build(documents, mode, collector);
            warnings.AddRange(html.Warnings);
            fingerprints.Add(BiographyExportHtmlBuilder.ComputeFingerprint(html.Html));
        }
        if (mode == BiographyExportMode.Combined) Add(items);
        else foreach (var item in items) Add([item]);
        cancellationToken.ThrowIfCancellationRequested();
        return new(warnings.Distinct(StringComparer.Ordinal).ToArray(), fingerprints.ToArray());
    }
}
