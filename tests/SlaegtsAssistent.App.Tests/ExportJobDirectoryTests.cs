using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class ExportJobDirectoryTests
{
    [Fact]
    public void JobmappeErPrivatPaaUnix()
    {
        if (OperatingSystem.IsWindows()) return;
        using var job = new ExportJobDirectory(Guid.NewGuid().ToString("N"));
        File.GetUnixFileMode(job.Path).Should().Be(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    [Fact]
    public void OpstartFjernerKunGamleMarkeredeOgIkkeAktiveJob()
    {
        var root = Path.Combine(Path.GetTempPath(), "slaegt-jobtest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var old = CreateJob(root, "SlaegtsAssistent eksportjob v1", DateTime.UtcNow.AddDays(-2));
            var recent = CreateJob(root, "SlaegtsAssistent eksportjob v1", DateTime.UtcNow);
            var unrelated = CreateJob(root, "Andet indhold", DateTime.UtcNow.AddDays(-2));
            var active = CreateJob(root, "SlaegtsAssistent eksportjob v1", DateTime.UtcNow.AddDays(-2));
            using var locked = new FileStream(Path.Combine(active, ".slaegtsassistent-job"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            ExportJobDirectory.CleanupAbandoned(root, DateTime.UtcNow);

            Directory.Exists(old).Should().BeFalse();
            Directory.Exists(recent).Should().BeTrue();
            Directory.Exists(unrelated).Should().BeTrue();
            Directory.Exists(active).Should().BeTrue();
        }
        finally { Directory.Delete(root, true); }
    }

    private static string CreateJob(string root, string markerContent, DateTime timestamp)
    {
        var directory = Path.Combine(root, "slaegtsassistent-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var marker = Path.Combine(directory, ".slaegtsassistent-job");
        File.WriteAllText(marker, markerContent);
        File.SetLastWriteTimeUtc(marker, timestamp);
        return directory;
    }
}
