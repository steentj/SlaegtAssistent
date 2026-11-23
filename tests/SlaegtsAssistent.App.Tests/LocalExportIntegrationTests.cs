using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Graph;
using SlaegtsAssistent.Core.Graph.Export;
using SlaegtsAssistent.Core.FamilyTrees;
using SlaegtsAssistent.Core.Gedcom;
using Xunit;
using System.Numerics;

namespace SlaegtsAssistent.App.Tests;

public sealed class LocalExportIntegrationTests
{
    [BundledExportIntegrationFact]
    public async Task PakketSidecarEksportererSlaegtstraeIAlleFormater()
    {
        var fixture = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        await File.WriteAllTextAsync(fixture, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Søren /Æble/
            0 TRLR
            """);
        try
        {
            var tree = new GedcomLoader().Load(fixture);
            var plan = new FamilyTreeExportPlanner(new FamilyTreeIndex(tree)).Create("@I1@", "1", 0, 0);
            var sidecar = new LocalExportSidecar(new LocalProcessRunner(),
                Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")!, null,
                Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES"),
                Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS"));
            foreach (var format in new[] { "pdf", "docx", "odt" })
            {
                var bytes = await sidecar.RenderFamilyTreeAsync(plan, format, "A4", CancellationToken.None);
                bytes.Should().NotBeEmpty();
                if (format == "pdf") bytes.Should().StartWith("%PDF-"u8.ToArray());
                else bytes.Should().StartWith("PK"u8.ToArray());
            }
        }
        finally { File.Delete(fixture); }
    }

    [BundledExportIntegrationFact]
    public async Task PakketSidecarAfviserForStortSlaegtstraeMedDanskForklaring()
    {
        var person = new FamilyTreePerson("@I@", new string('Å', 3000), "Ukendt", "Ukendt", "Ukendt", "Ukendt");
        var cells = Enumerable.Range(0, 9)
            .Select(_ => new FamilyTreeExportCell(person, Array.Empty<BigInteger>())).ToArray();
        var plan = new FamilyTreeExportPlan("@I@", "1", person,
            [new FamilyTreeExportRow(1, "Børn", cells)]);
        var sidecar = new LocalExportSidecar(new LocalProcessRunner(),
            Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")!, null,
            Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES"),
            Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS"));
        Func<Task> action = () => sidecar.RenderFamilyTreeAsync(plan, "pdf", "A3", CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*kan ikke være læseligt på én side*");
    }

    [LocalExportIntegrationFact]
    public async Task RigtigSidecarReturnererEnPdf()
    {
        var executable = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_PYTHON")!;
        var script = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_SCRIPT")!;
        var html = """
            <html><body><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><line x1="10" y1="10" x2="70" y2="70" stroke="#777777"/><circle cx="10" cy="10" r="5" fill="#d84444"/><circle cx="70" cy="70" r="8" fill="#4488dd"/><text x="10" y="25" font-size="10">Åse Østergaard</text></svg></body></html>
            """;

        var pdf = await new LocalExportSidecar(new LocalProcessRunner(), executable, script,
                Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES"),
                Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS"))
            .RenderGraphPdfAsync(html, "A4", "landscape", CancellationToken.None);

        pdf.Should().StartWith("%PDF-"u8.ToArray());
    }

    [BundledExportIntegrationFact]
    public async Task PakketSidecarReturnererEnPdfUdenSystemPython()
    {
        var executable = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")!;
        var libraries = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES");
        var fonts = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS");
        var graph = new PersonGraphBuilder().Build([new("A", "Søren Æble", "M", "1900", null)], []);
        var layout = new PersonGraphLayout(new GraphBounds(100, 100), new Dictionary<string, GraphPoint> { ["A"] = new(50, 50) });
        var scene = new GraphExportSceneBuilder().Build(graph, layout, new(GraphExportScope.WholeGraph, true, "A"));
        var html = "<html><body>" + new GraphSvgWriter().Write(scene) + "</body></html>";
        var pdf = await new LocalExportSidecar(new LocalProcessRunner(), executable, null, libraries, fonts)
            .RenderGraphPdfAsync(html, "A4", "landscape", CancellationToken.None);
        pdf.Should().StartWith("%PDF-"u8.ToArray());
    }
}

public sealed class BundledExportIntegrationFactAttribute : FactAttribute
{
    public BundledExportIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")))
            Skip = "Kræver en lokalt pakket PDF-sidecar.";
    }
}

public sealed class LocalExportIntegrationFactAttribute : FactAttribute
{
    public LocalExportIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_PYTHON")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_SCRIPT")))
            Skip = "Kræver et lokalt afprøvet PDF-værktøj.";
    }
}
