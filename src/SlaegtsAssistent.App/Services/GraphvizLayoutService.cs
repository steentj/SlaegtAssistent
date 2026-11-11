using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.Services;

public sealed class GraphvizLayoutService
{
    private readonly ILocalProcessRunner _runner;
    private readonly string _executable;

    public GraphvizLayoutService(ILocalProcessRunner runner, string executable)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _executable = executable ?? throw new ArgumentNullException(nameof(executable));
    }

    public async Task<PersonGraphLayout> LayoutAsync(PersonGraph graph, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (graph.Nodes.Count == 0) return new PersonGraphLayout(new GraphBounds(1, 1), new Dictionary<string, GraphPoint>());
        var input = new GraphDotWriter().Write(graph);
        var directory = Path.Combine(Path.GetTempPath(), "slaegtsassistent-graf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var dotPath = Path.Combine(directory, "graph.dot");
            var plainPath = Path.Combine(directory, "graph.plain");
            await File.WriteAllTextAsync(dotPath, input.Dot, cancellationToken).ConfigureAwait(false);
            var result = await _runner.RunAsync(new LocalProcessRequest(_executable,
                ["-Tplain", dotPath, "-o", plainPath], directory, TimeSpan.FromSeconds(30)), cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0 || result.StandardError.Contains("Error:", StringComparison.OrdinalIgnoreCase) ||
                result.StandardError.Contains("unsupported", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Grafens layout kunne ikke beregnes med den medfølgende Graphviz-version.");
            if (!File.Exists(plainPath) || new FileInfo(plainPath).Length > 32 * 1024 * 1024)
                throw new InvalidDataException("Graphviz-output mangler eller er for stort.");
            return new GraphPlainParser().Parse(await File.ReadAllTextAsync(plainPath, cancellationToken).ConfigureAwait(false), input);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
