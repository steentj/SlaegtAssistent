using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class GitVersionControlServiceTests
{
    [Fact]
    public async Task GetStatusAsync_GitMangler_NaarLocatorReturnererNull()
    {
        var service = new GitVersionControlService(new NullGitLocator(), new LocalProcessRunner());

        var status = await service.GetStatusAsync(Path.GetTempPath(), CancellationToken.None);

        status.State.Should().Be(VersionControlState.GitMangler);
    }

    [GitFact]
    public async Task EnableCommitOgHistorik_VirkerIReeltArkiv()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "versioning-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            File.WriteAllText(Path.Combine(workspace, "person.md"), "# Person\n");
            var git = new GitLocator().FindGitExecutable()!;
            var service = new GitVersionControlService(new FixedGitLocator(git), new LocalProcessRunner());

            var enable = await service.EnableAsync(workspace, CancellationToken.None);
            enable.Success.Should().BeTrue(enable.Message);

            var statusAfterEnable = await service.GetStatusAsync(workspace, CancellationToken.None);
            statusAfterEnable.State.Should().Be(VersionControlState.Klar);
            statusAfterEnable.ChangedFileCount.Should().Be(0);

            File.AppendAllText(Path.Combine(workspace, "person.md"), "Ny tekst\n");
            var statusWithChange = await service.GetStatusAsync(workspace, CancellationToken.None);
            statusWithChange.ChangedFileCount.Should().Be(1);

            var commit = await service.CommitAllAsync(workspace, "Test æøå", CancellationToken.None);
            commit.Success.Should().BeTrue(commit.Message);

            var history = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            history.Entries.Should().Contain(entry => entry.Message == "Test æøå");
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    [GitFact]
    public async Task RollbackTilStartcommit_BevarerBaselineDokumenter()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "versioning-rollback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            var firstPath = Path.Combine(workspace, "anna.md");
            var secondPath = Path.Combine(workspace, "bo.md");
            File.WriteAllText(firstPath, "# Anna\nOriginal\n");
            File.WriteAllText(secondPath, "# Bo\nOriginal\n");

            var git = new GitLocator().FindGitExecutable()!;
            var service = new GitVersionControlService(new FixedGitLocator(git), new LocalProcessRunner());

            var enable = await service.EnableAsync(workspace, CancellationToken.None);
            enable.Success.Should().BeTrue(enable.Message);

            var historyAfterEnable = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            var startCommit = historyAfterEnable.Entries.Single(entry => entry.Message == "Start versionsstyring").Id;

            File.WriteAllText(firstPath, "# Anna\nÆndret\n");
            var commit = await service.CommitAllAsync(workspace, "Anden version", CancellationToken.None);
            commit.Success.Should().BeTrue(commit.Message);

            var rollback = await service.RollbackToAsync(workspace, startCommit, CancellationToken.None);
            rollback.Success.Should().BeTrue(rollback.Message);

            File.Exists(firstPath).Should().BeTrue();
            File.Exists(secondPath).Should().BeTrue();
            File.ReadAllText(firstPath).Should().Contain("Original");
            File.ReadAllText(secondPath).Should().Contain("Original");
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    [GitFact]
    public async Task RollbackFremOgTilbage_EfterladerIngenIkkeCommittedeAendringer()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "versioning-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            var rootPath = Path.Combine(workspace, "person.md");
            Directory.CreateDirectory(Path.Combine(workspace, "under"));
            File.WriteAllText(Path.Combine(workspace, "under", "skal-ignoreres.md"), "Udenfor omfang");
            File.WriteAllText(rootPath, "# Person\nV1\n");

            var git = new GitLocator().FindGitExecutable()!;
            var service = new GitVersionControlService(new FixedGitLocator(git), new LocalProcessRunner());

            (await service.EnableAsync(workspace, CancellationToken.None)).Success.Should().BeTrue();
            File.WriteAllText(rootPath, "# Person\nV2\n");
            (await service.CommitAllAsync(workspace, "V2", CancellationToken.None)).Success.Should().BeTrue();

            var history = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            var start = history.Entries.Single(entry => entry.Message == "Start versionsstyring").Id;
            var second = history.Entries.Single(entry => entry.Message == "V2").Id;

            (await service.RollbackToAsync(workspace, start, CancellationToken.None)).Success.Should().BeTrue();
            (await service.RollbackToAsync(workspace, second, CancellationToken.None)).Success.Should().BeTrue();

            var status = await service.GetStatusAsync(workspace, CancellationToken.None);
            status.ChangedFileCount.Should().Be(0);
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    [GitFact]
    public async Task RollbackMellemVersionerMedMangeFiler_BevarerRenTilstand()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "versioning-many-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            for (var i = 0; i < 94; i++)
            {
                File.WriteAllText(Path.Combine(workspace, $"person-{i:000}.md"), $"# Person {i}\nV1\n");
            }

            var git = new GitLocator().FindGitExecutable()!;
            var service = new GitVersionControlService(new FixedGitLocator(git), new LocalProcessRunner());

            (await service.EnableAsync(workspace, CancellationToken.None)).Success.Should().BeTrue();
            File.AppendAllText(Path.Combine(workspace, "person-010.md"), "Ændring\n");
            (await service.CommitAllAsync(workspace, "V2", CancellationToken.None)).Success.Should().BeTrue();

            var history = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            var start = history.Entries.Single(entry => entry.Message == "Start versionsstyring").Id;
            var v2 = history.Entries.Single(entry => entry.Message == "V2").Id;

            (await service.RollbackToAsync(workspace, start, CancellationToken.None)).Success.Should().BeTrue();
            (await service.RollbackToAsync(workspace, v2, CancellationToken.None)).Success.Should().BeTrue();

            var status = await service.GetStatusAsync(workspace, CancellationToken.None);
            status.ChangedFileCount.Should().Be(0);
            (await service.GetHistoryAsync(workspace, 20, CancellationToken.None)).Entries.Should().HaveCountGreaterThanOrEqualTo(4);
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    [GitFact]
    public async Task RollbackMedIkkeCommittedeAendringer_LaaserIkkeOgAfsluttesRent()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "versioning-autocommit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            var file = Path.Combine(workspace, "person.md");
            File.WriteAllText(file, "# Person\nV1\n");

            var git = new GitLocator().FindGitExecutable()!;
            var service = new GitVersionControlService(new FixedGitLocator(git), new LocalProcessRunner());

            (await service.EnableAsync(workspace, CancellationToken.None)).Success.Should().BeTrue();
            File.WriteAllText(file, "# Person\nV2\n");
            (await service.CommitAllAsync(workspace, "V2", CancellationToken.None)).Success.Should().BeTrue();

            var history = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            var start = history.Entries.Single(entry => entry.Message == "Start versionsstyring").Id;

            File.AppendAllText(file, "Lokal ikke-committet ændring\n");
            var rollback = await service.RollbackToAsync(workspace, start, CancellationToken.None);

            rollback.Success.Should().BeTrue(rollback.Message);
            (await service.GetStatusAsync(workspace, CancellationToken.None)).ChangedFileCount.Should().Be(0);

            var updatedHistory = await service.GetHistoryAsync(workspace, 10, CancellationToken.None);
            updatedHistory.Entries.Should().Contain(entry => entry.Message == "Automatisk version før tilbagerulning");
        }
        finally
        {
            Directory.Delete(workspace, true);
        }
    }

    private sealed class NullGitLocator : IGitLocator
    {
        public string? FindGitExecutable() => null;
    }

    private sealed class FixedGitLocator(string path) : IGitLocator
    {
        public string? FindGitExecutable() => path;
    }
}
