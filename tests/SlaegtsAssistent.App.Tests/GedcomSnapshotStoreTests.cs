using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomSnapshotStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsSourceIdentityAndRawPersonSegments()
    {
        var sourcePath = CreateTemporaryGedcomFile();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var familyTree = new GedcomLoader().Load(sourcePath);
            var store = new FileSystemGedcomSnapshotStore();

            store.Save(outputDirectory, sourcePath, familyTree);
            var snapshot = store.Load(outputDirectory);

            snapshot.Should().NotBeNull();
            snapshot!.SourcePath.Should().Be(Path.GetFullPath(sourcePath));
            snapshot.SourceFileName.Should().Be(Path.GetFileName(sourcePath));
            snapshot.SourceHash.Should().NotBeNullOrWhiteSpace();
            snapshot.RawPersonSegments.Should().ContainKey("@I1@");
            snapshot.RawPersonSegments["@I1@"].Should().Contain("1 NAME Anna /Jensen/");
            Directory.Exists(Path.Combine(outputDirectory, ".slaegtsassistent", "gedcom"))
                .Should()
                .BeTrue();
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteDirectoryIfExists(outputDirectory);
        }
    }

    [Fact]
    public void Load_ThrowsClearException_WhenManifestIsCorrupt()
    {
        var sourcePath = CreateTemporaryGedcomFile();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var familyTree = new GedcomLoader().Load(sourcePath);
            var store = new FileSystemGedcomSnapshotStore();
            store.Save(outputDirectory, sourcePath, familyTree);

            var manifestPath = Path.Combine(
                outputDirectory,
                ".slaegtsassistent",
                "gedcom",
                "manifest.json");
            File.WriteAllText(manifestPath, "{ ugyldig json");

            var action = () => store.Load(outputDirectory);

            action.Should().Throw<GedcomSnapshotException>()
                .WithMessage("*manifest*kunne ikke læses*");
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteDirectoryIfExists(outputDirectory);
        }
    }

    [Fact]
    public void Save_WhenManifestCommitFails_PreservesPreviousSnapshot()
    {
        var sourcePath = CreateTemporaryGedcomFile();
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var originalTree = new GedcomLoader().Load(sourcePath);
            var originalStore = new FileSystemGedcomSnapshotStore();
            originalStore.Save(outputDirectory, sourcePath, originalTree);
            var originalSnapshot = originalStore.Load(outputDirectory);
            var replaceCalls = 0;
            var failingWriter = new AtomicFileWriter(stage =>
            {
                if (stage == AtomicWriteStage.BeforeDestinationReplace && ++replaceCalls == 2)
                {
                    throw new IOException("Simuleret fejl ved manifest-commit.");
                }
            });
            File.WriteAllLines(
                sourcePath,
                [
                    "0 HEAD",
                    "0 @I1@ INDI",
                    "1 NAME Anna /Andersen/",
                    "0 TRLR",
                ]);
            var changedTree = new GedcomLoader().Load(sourcePath);
            var failingStore = new FileSystemGedcomSnapshotStore(failingWriter);

            var action = () => failingStore.Save(outputDirectory, sourcePath, changedTree);

            action.Should().Throw<GedcomSnapshotException>()
                .WithMessage("*manifest.json*tidligere version*");
            var recoveredSnapshot = originalStore.Load(outputDirectory);
            recoveredSnapshot.Should().BeEquivalentTo(originalSnapshot);
        }
        finally
        {
            DeleteIfExists(sourcePath);
            DeleteDirectoryIfExists(outputDirectory);
        }
    }

    private static string CreateTemporaryGedcomFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllLines(
            path,
            [
                "0 HEAD",
                "1 SOUR SlaegtsAssistentTests",
                "1 GEDC",
                "2 VERS 5.5.1",
                "1 CHAR UTF-8",
                "0 @I1@ INDI",
                "1 NAME Anna /Jensen/",
                "0 TRLR",
            ]);
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
