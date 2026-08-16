using System;
using System.IO;
using System.Linq;
using System.Text;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App;

public static class ReleaseSmokeTest
{
    public static int Run(string? workingDirectory = null)
    {
        var ownsDirectory = string.IsNullOrWhiteSpace(workingDirectory);
        var directory = ownsDirectory
            ? Path.Combine(Path.GetTempPath(), $"slaegtsassistent-smoke-{Guid.NewGuid():N}")
            : Path.GetFullPath(workingDirectory!);

        try
        {
            Directory.CreateDirectory(directory);
            Execute(directory);
            Console.WriteLine("Grundlæggende funktionstest bestået.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Grundlæggende funktionstest fejlede: {exception.Message}");
            return 1;
        }
        finally
        {
            if (ownsDirectory && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private static void Execute(string directory)
    {
        var gedcomPath = Path.Combine(directory, "smoke-test.ged");
        File.WriteAllText(
            gedcomPath,
            "0 HEAD\n1 SOUR SlaegtsAssistentSmokeTest\n1 GEDC\n2 VERS 5.5.1\n" +
            "2 FORM LINEAGE-LINKED\n1 CHAR UTF-8\n0 @I1@ INDI\n" +
            "1 NAME Anna /Jensen/\n1 BIRT\n2 DATE 12 MAR 1900\n2 PLAC Aarhus\n0 TRLR\n",
            new UTF8Encoding(false));

        var tree = new GedcomLoader().Load(gedcomPath);
        if (tree.People.Count != 1 || tree.FindPerson("@I1@")?.FullName != "Anna Jensen")
        {
            throw new InvalidOperationException("GEDCOM-importen gav ikke det forventede resultat.");
        }

        var settingsPath = Path.Combine(directory, "settings.json");
        var settings = new JsonApplicationSettingsService(settingsPath, new AtomicFileWriter());
        settings.Save(new AppSettings { DefaultMarkdownOutputFolder = directory });
        if (settings.Load().DefaultMarkdownOutputFolder != directory)
        {
            throw new InvalidOperationException("Indstillinger kunne ikke genindlæses.");
        }

        new MarkdownBiographyExportService(settings).WriteBiographies(tree, directory);
        var markdownPath = Directory.EnumerateFiles(directory, "*.md").Single();
        var markdown = new FileSystemMarkdownFileStore().Read(markdownPath);
        if (!markdown.Contains("Anna Jensen", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Persondokumentet indeholder ikke den importerede person.");
        }

        var snapshotStore = new FileSystemGedcomSnapshotStore();
        snapshotStore.Save(directory, gedcomPath, tree);
        var snapshot = snapshotStore.Load(directory);
        if (snapshot?.RawPersonSegments.ContainsKey("@I1@") != true)
        {
            throw new InvalidOperationException("GEDCOM-snapshotet kunne ikke genindlæses.");
        }

        var catalog = new FileSystemMarkdownDocumentCatalog().Load(directory);
        if (!catalog.Any(document => document.RecordId == "@I1@"))
        {
            throw new InvalidOperationException("Persondokumentet kunne ikke genfindes efter genstart.");
        }

        var preview = new SafeMarkdownPreviewService().Render(markdown, markdownPath, [directory]);
        if (!preview.Html.Contains("Anna Jensen", StringComparison.Ordinal) ||
            preview.NetworkRequestAttempts != 0)
        {
            throw new InvalidOperationException("Det private Markdown-preview kunne ikke valideres.");
        }
    }
}
