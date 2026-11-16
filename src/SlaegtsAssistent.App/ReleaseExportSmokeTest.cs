using System;
using System.Threading;
using System.IO;
using SlaegtsAssistent.Core.Export;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App;

internal static class ReleaseExportSmokeTest
{
    public static int RunBiography()
    {
        try
        {
            var tool = LocalExportToolLocator.Find(AppContext.BaseDirectory)
                ?? throw new InvalidOperationException("Det private eksportværktøj mangler eller er ufuldstændigt.");
            var sidecar = new LocalExportSidecar(new LocalProcessRunner(), tool.Executable, null, tool.LibraryDirectory, tool.FontConfigFile);
            var root = Path.GetTempPath();
            BiographyExportItem[] items =
            [
                new("a", "Åse", Path.Combine(root, "slaegt-funktionstest-a.md"), "# Åse\n\nDansk tekst æøå og [Bo](slaegt-funktionstest-b.md)."),
                new("b", "Bo", Path.Combine(root, "slaegt-funktionstest-b.md"), "# Bo\n\nSyntetisk biografi.")
            ];
            foreach (var format in new[] { BiographyExportFormat.Pdf, BiographyExportFormat.Docx, BiographyExportFormat.Odt })
                sidecar.RenderBiographyAsync(items, BiographyExportMode.Combined, format, [], CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine("Funktionstest af biografieksport til PDF, DOCX og ODT bestået.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Funktionstest af biografieksport fejlede: {exception.Message}");
            return 1;
        }
    }

    public static int Run()
    {
        try
        {
            var tool = LocalExportToolLocator.Find(AppContext.BaseDirectory)
                ?? throw new InvalidOperationException("Den private PDF-sidecar mangler eller er ufuldstændig.");
            var sidecar = new LocalExportSidecar(new LocalProcessRunner(), tool.Executable, null, tool.LibraryDirectory, tool.FontConfigFile);
            const string html = """
                <html><body><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><line x1="20" y1="50" x2="80" y2="50" stroke="#777777"/><circle cx="20" cy="50" r="6" fill="#d84444"/><circle cx="80" cy="50" r="8" fill="#4488dd"/><text x="10" y="75" font-size="10">Åse Østergaard</text></svg></body></html>
                """;
            sidecar.RenderGraphPdfAsync(html, "A4", "landscape", CancellationToken.None).GetAwaiter().GetResult();
            Console.WriteLine("Funktionstest af privat PDF-sidecar bestået.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Funktionstest af privat PDF-sidecar fejlede: {exception.Message}");
            return 1;
        }
    }
}
