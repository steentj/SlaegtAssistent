using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class GitLocatorTests
{
    [Fact]
    public void FindGitExecutable_ReturnererNullNaarIngenKandidatValideres()
    {
        var locator = new GitLocator(new AlwaysFailRunner());

        locator.FindGitExecutable().Should().BeNull();
    }

    [Fact]
    public void FindGitExecutable_FinderPathKandidat()
    {
        var temp = Path.Combine(Path.GetTempPath(), "git-locator-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var name = OperatingSystem.IsWindows() ? "git.exe" : "git";
        var fakeGit = Path.Combine(temp, name);
        File.WriteAllText(fakeGit, string.Empty);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(fakeGit, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        var previousPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", temp);
        try
        {
            var locator = new GitLocator(new MatchingRunner(fakeGit));

            locator.FindGitExecutable().Should().Be(fakeGit);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", previousPath);
            Directory.Delete(temp, true);
        }
    }

    private sealed class AlwaysFailRunner : ILocalProcessRunner
    {
        public Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new LocalProcessResult(1, string.Empty, string.Empty));
    }

    private sealed class MatchingRunner(string expectedPath) : ILocalProcessRunner
    {
        public Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new LocalProcessResult(
                string.Equals(request.Executable, expectedPath, StringComparison.Ordinal) ? 0 : 1,
                string.Empty,
                string.Empty));
    }
}
