using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public sealed class GraphSettingsStoreTests
{
    [Fact]
    public void ProbandGenskabesKunForDetRetteArbejdsområde()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new GraphSettingsStore(new AtomicFileWriter());
            var first = Path.Combine(root, "første");
            var second = Path.Combine(root, "andet");
            store.Save(first, "@I1@");
            store.Load(first).Should().Be("@I1@");
            store.Load(second).Should().BeNull();
            var file = Path.Combine(first, ".slaegtsassistent", "graph-settings.json");
            File.WriteAllText(file, "{\"formatVersion\":99,\"probandId\":\"@I2@\"}");
            var action = () => store.Load(first);
            action.Should().Throw<InvalidDataException>();
            File.ReadAllText(file).Should().Contain("99");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
