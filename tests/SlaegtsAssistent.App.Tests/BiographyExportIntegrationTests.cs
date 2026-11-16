using System.IO.Compression;
using System.Text;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Export;

namespace SlaegtsAssistent.App.Tests;

public class BiographyExportIntegrationTests
{
    [BundledExportIntegrationFact]
    public async Task PakketVaerktoejEksportererBiografierMedTabelBilledeOgLinks()
    {
        var root = Path.Combine(Path.GetTempPath(), "biografiproeve-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(root, "foto.png"), Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAALQAAAB4CAIAAADUhU+qAAACoElEQVR4nO3cwU1rQRAF0WdEXGRAJhOOM2HtDRE4AXJhj2hkIdtTt7vOEv3FdFMzRuiL09f1cki/efn1q5Jx6C++HCoZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScaj0egx2/vi85Z+t97djpNOo/89xYw1/W2NaGRHHXZoYWEnnOB7UxJxKesbxtCx6J9Itji1ZdE2kTxzbs+iXSJPfc6DKOHjnGfpywL8NK/kJyX454GUcCSfsGUfK3s8h52zysRK67pX2EZP3coSWcQSePCyOuP1Gnz8pjqzNNpgiJo6gnbaZJSYOPV9GHClXrdlEAXFE7LHlXPQ4+BtsPB09Dm2EjgN+sdrPyI2DvLUhk3Lj0HbQOLCXadS80DhEQIyDeY0GTk2MQxC4OIAXaOzsuDjEYRwKiYP2rg7fACsOoRiHEuJAvagbcfYAikM0xqGScahkHMLHwfkpjODM2AYlDgEZh0rGoZJxqGQcKhmHSsahknGoZBwqGYdKxqGScQgfR9wfcJ2wDUocAjIOlYxDJeNQQhyQn8K2W5g9gOIQjXEoJA7Oi7oLagOsOIRiHMqJA/WuDp8dF4c4iHHQLtDYqYlxCAIaB/AaDZwXGocIuHEwL9OoSblxkLc2ZEZ0HNqLHgf5YrWfjh4Hf4ON5wqII2KPLSfKiENbxMQRcdWazRITR9BO20yRFEfWZhucPyyOuP1Gn/z0db0cmSB/ybVlFqkvR9zGV8g5W8URsfeFP2HPjxX4R8xKzqLDy4H9TizYeUa/HJwnZLXIomccGxNZjbLoHMeTE1ntsugfx6MrWU2bmBXHfStZ3ZsYGsf/WlljavhhdBwa8XsOPYJxqGQcKhmHSsahknGoZBwqGYdKxqGScahkHCoZh0rGoaPyDcy1y1V3NQcDAAAAAElFTkSuQmCC"));
            var table = string.Join('\n', Enumerable.Range(1, 60).Select(i => $"| {i} | Æble Østergaard |"));
            var longUrl = "https://example.org/" + new string('a', 300);
            var longName = "Langt navn " + new string('ø', 180);
            var items = new[]
            {
                new BiographyExportItem("a", "Åse", Path.Combine(root, "a.md"), "# Åse\n\nFri tekst æøå med [Bo](b.md) og note[^1].\n\n[^1]: Kirkebog.\n\n| Nr | Kilde |\n| --- | --- |\n" + table + "\n\n![Portræt](foto.png)\n\n![Mangler](mangler.png)"),
                new BiographyExportItem("b", "Bo", Path.Combine(root, "b.md"), "# Bo\n\n[Kilde](a.md), [ekstern kilde](https://example.org/arkiv) og dansk tekst.\n\n```\n[kode](javascript:alert)\n```") ,
                new BiographyExportItem("c", "Bo", Path.Combine(root, "c.md"), "# Bo\n\nEn anden person med samme navn.\n\nLang kilde: [" + longUrl + "](" + longUrl + ")"),
                new BiographyExportItem("d", longName, Path.Combine(root, "d.md"), "# " + longName + "\n\nLangt navn bruges i layoutprøven.")
            };
            var executable = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_BUNDLE")!;
            var libraries = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_LIBRARIES");
            var fonts = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_EXPORT_FONTS");
            var sidecar = new LocalExportSidecar(new LocalProcessRunner(), executable, null, libraries, fonts);
            var preflight = new BiographyExportPreflightService().Prepare(items, BiographyExportMode.Combined, [root], CancellationToken.None);
            foreach (var format in new[] { BiographyExportFormat.Pdf, BiographyExportFormat.Docx, BiographyExportFormat.Odt })
            {
                var result = await sidecar.RenderBiographyAsync(items, BiographyExportMode.Combined, format, [root], CancellationToken.None);
                result.Warnings.Should().NotBeEmpty();
                result.Fingerprint.Should().Be(preflight.Fingerprints.Single());
                var outputDirectory = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_GOLDEN_OUTPUT");
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "syntetisk-biografi." + format.ToString().ToLowerInvariant()), result.Content);
                }
                if (format == BiographyExportFormat.Pdf)
                {
                    result.Content.Should().StartWith("%PDF-"u8.ToArray());
                    var python = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_TEST_PDF_READER_PYTHON");
                    if (!string.IsNullOrWhiteSpace(python))
                    {
                        var pdfPath = Path.Combine(root, "kontrol.pdf");
                        await File.WriteAllBytesAsync(pdfPath, result.Content);
                        var start = new System.Diagnostics.ProcessStartInfo(python) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                        start.ArgumentList.Add("-c");
                        start.ArgumentList.Add("import sys; from pypdf import PdfReader; print(''.join(page.extract_text() for page in PdfReader(sys.argv[1]).pages))");
                        start.ArgumentList.Add(pdfPath);
                        using var process = System.Diagnostics.Process.Start(start)!;
                        var text = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();
                        process.ExitCode.Should().Be(0, error);
                        var compact = new string(text.Where(character => !char.IsWhiteSpace(character)).ToArray());
                        compact.Should().Contain(longUrl).And.Contain(new string(longName.Where(character => !char.IsWhiteSpace(character)).ToArray()));
                    }
                    continue;
                }
                using var archive = new ZipArchive(new MemoryStream(result.Content));
                var entry = archive.GetEntry(format == BiographyExportFormat.Docx ? "word/document.xml" : "content.xml")!;
                using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                var xml = await reader.ReadToEndAsync();
                xml.Should().Contain("Æble Østergaard").And.Contain("Billede ikke medtaget");
                xml.Should().NotContain("SLAEGTSASSISTENT_SIDESKIFT_");
                archive.Entries.Should().Contain(x => x.FullName.StartsWith(format == BiographyExportFormat.Docx ? "word/media/" : "Pictures/", StringComparison.Ordinal));
            }
            var separateItems = new[]
            {
                new BiographyExportItem("a", "Åse", Path.Combine(root, "a.md"), "![Foto](foto.png)"),
                new BiographyExportItem("b", "Bo", Path.Combine(root, "b.md"), "![Foto](foto.png)")
            };
            var separate = new BiographyExportPreflightService().Prepare(separateItems, BiographyExportMode.Separate, [root], CancellationToken.None);
            for (var i = 0; i < separateItems.Length; i++)
            {
                var result = await sidecar.RenderBiographyAsync([separateItems[i]], BiographyExportMode.Separate, BiographyExportFormat.Pdf, [root], CancellationToken.None);
                result.Fingerprint.Should().Be(separate.Fingerprints[i]);
            }
        }
        finally { Directory.Delete(root, true); }
    }
}
