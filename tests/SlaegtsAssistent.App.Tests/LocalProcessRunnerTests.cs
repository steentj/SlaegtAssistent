using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class LocalProcessRunnerTests
{
    [Fact]
    public async Task ArgumenterMedMellemrumSendesSomEtArgument()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = await new LocalProcessRunner().RunAsync(
            new LocalProcessRequest("/usr/bin/printf", ["%s", "æ og ø"], Path.GetTempPath(), TimeSpan.FromSeconds(5)), CancellationToken.None);
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Be("æ og ø");
    }

    [Fact]
    public async Task LokaltMiljoeSendesTilProcessen()
    {
        if (OperatingSystem.IsWindows()) return;
        var result = await new LocalProcessRunner().RunAsync(
            new LocalProcessRequest("/usr/bin/printenv", ["SLAEGTSASSISTENT_TEST"], Path.GetTempPath(), TimeSpan.FromSeconds(5), new Dictionary<string, string?> { ["SLAEGTSASSISTENT_TEST"] = "privat værdi" }), CancellationToken.None);
        result.StandardOutput.Trim().Should().Be("privat værdi");
    }

    [Fact]
    public async Task TidsgrænseStopperProcessen()
    {
        if (OperatingSystem.IsWindows()) return;
        var action = () => new LocalProcessRunner().RunAsync(
            new LocalProcessRequest("/bin/sleep", ["10"], Path.GetTempPath(), TimeSpan.FromMilliseconds(100)), CancellationToken.None);
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ForStortOutputStopperProcessen()
    {
        if (OperatingSystem.IsWindows()) return;
        var action = () => new LocalProcessRunner().RunAsync(
            new LocalProcessRequest("/usr/bin/yes", [], Path.GetTempPath(), TimeSpan.FromSeconds(5)), CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task OutputgraensenMaalesIBytesOgsaaForDanskeBogstaver()
    {
        if (OperatingSystem.IsWindows() || !File.Exists("/usr/bin/perl")) return;
        var action = () => new LocalProcessRunner().RunAsync(
            new LocalProcessRequest("/usr/bin/perl", ["-e", "print 'ø' x 600000"], Path.GetTempPath(), TimeSpan.FromSeconds(5)), CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>();
    }
}
