using System.Text.Json;
using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class LocalExportSidecarTests
{
    [Fact]
    public async Task GyldigtSvarReturnererPdfOgSletterJobmappe()
    {
        var runner = new FakeRunner();
        var pdf = await Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        pdf.Should().StartWith("%PDF-"u8.ToArray());
        Directory.Exists(runner.Directory).Should().BeFalse();
        runner.Request!.Arguments.Should().HaveCount(3);
        runner.Request.Timeout.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public async Task PakketSidecarStartesUdenPythonScript()
    {
        var runner = new FakeRunner();
        var executable = typeof(LocalExportSidecarTests).Assembly.Location;
        await new LocalExportSidecar(runner, executable, null, Path.GetTempPath()).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        runner.Request!.Arguments.Should().HaveCount(2);
        runner.Request.Arguments[0].Should().Be("--request");
        if (OperatingSystem.IsMacOS()) runner.Request.EnvironmentVariables!["DYLD_FALLBACK_LIBRARY_PATH"].Should().Be(Path.GetTempPath());
    }

    [Fact]
    public async Task VersionKontrolleresKunVedFoersteEksport()
    {
        var runner = new FakeRunner();
        var sidecar = Create(runner);
        await sidecar.RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        await sidecar.RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        runner.VersionChecks.Should().Be(1);
    }

    [Fact]
    public async Task ForkertVersionAfvisesFoerKonvertering()
    {
        var runner = new FakeRunner { Failure = "version" };
        var action = () => Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>().WithMessage("*version*");
        Directory.Exists(runner.Directory).Should().BeFalse();
    }

    [Theory]
    [InlineData("json")]
    [InlineData("job")]
    [InlineData("path")]
    [InlineData("empty")]
    [InlineData("signature")]
    [InlineData("exit")]
    [InlineData("duplicate")]
    [InlineData("extra")]
    [InlineData("oversized")]
    public async Task UgyldigtSvarAfvisesOgJobmappeSlettes(string failure)
    {
        var runner = new FakeRunner { Failure = failure };
        var action = () => Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        await action.Should().ThrowAsync<InvalidDataException>();
        Directory.Exists(runner.Directory).Should().BeFalse();
    }

    [Fact]
    public async Task ManglendePdfBibliotekGiverDanskFejl()
    {
        var runner = new FakeRunner { Failure = "missing" };
        var action = () => Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*biblioteker mangler*");
        Directory.Exists(runner.Directory).Should().BeFalse();
    }

    [Fact]
    public async Task AnnulleringSletterJobmappe()
    {
        var runner = new FakeRunner { Failure = "cancel" };
        using var cancellation = new CancellationTokenSource();
        var action = () => Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", cancellation.Token);
        runner.Cancel = cancellation;
        await action.Should().ThrowAsync<OperationCanceledException>();
        Directory.Exists(runner.Directory).Should().BeFalse();
    }

    [Fact]
    public async Task TidsudloebGiverForstaaeligFejlOgSletterJobmappe()
    {
        var runner = new FakeRunner { Failure = "cancel" };
        var action = () => Create(runner).RenderGraphPdfAsync("<html/>", "A4", "landscape", CancellationToken.None);
        await action.Should().ThrowAsync<TimeoutException>();
        Directory.Exists(runner.Directory).Should().BeFalse();
    }

    private static LocalExportSidecar Create(FakeRunner runner) => new(runner, typeof(LocalExportSidecarTests).Assembly.Location, typeof(LocalExportSidecarTests).Assembly.Location);

    private sealed class FakeRunner : ILocalProcessRunner
    {
        public string? Failure { get; init; }
        public CancellationTokenSource? Cancel { get; set; }
        public int VersionChecks { get; private set; }
        public string? Directory { get; private set; }
        public LocalProcessRequest? Request { get; private set; }

        public async Task<LocalProcessResult> RunAsync(LocalProcessRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            Directory = request.WorkingDirectory;
            if (Failure == "cancel") { Cancel?.Cancel(); throw new OperationCanceledException(); }
            if (request.Arguments[^1] == "--version")
            {
                VersionChecks++;
                return new(0, "{\"protocolVersion\":" + (Failure == "version" ? "2" : "1") + ",\"toolVersion\":\"0.1.0\",\"rendererVersion\":\"66.0\"}", "");
            }
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(request.Arguments[^1], cancellationToken));
            var job = document.RootElement.GetProperty("jobId").GetString();
            var output = document.RootElement.GetProperty("outputPath").GetString()!;
            if (Failure == "missing") return new(1, JsonSerializer.Serialize(new { protocolVersion = 1, jobId = job, status = "error", errorCode = "missing-tool", warnings = Array.Empty<string>() }), "");
            await File.WriteAllTextAsync(output, Failure == "empty" ? "" : Failure == "signature" ? "ugyldig" : "%PDF-1.7", cancellationToken);
            if (Failure == "oversized")
            {
                using var file = File.OpenWrite(output);
                file.SetLength(100L * 1024 * 1024 + 1);
            }
            var json = Failure == "json" ? "{" : JsonSerializer.Serialize(new { protocolVersion = 1, jobId = Failure == "job" ? "forkert" : job, status = "ok", outputPath = Failure == "path" ? output + "-forkert" : output, warnings = Array.Empty<string>() });
            if (Failure == "duplicate") json = json[..^1] + ",\"protocolVersion\":1}";
            if (Failure == "extra") json = json[..^1] + ",\"extra\":true}";
            return new(Failure == "exit" ? 1 : 0, json, "");
        }
    }
}
