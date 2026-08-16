using System.Text;
using FluentAssertions;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public sealed class LargeGedcomRegressionTests
{
    private const int PersonCount = 10_000;
    private const long MaximumAllocatedBytes = 512L * 1024 * 1024;

    [Fact]
    public void Load_StorDeterministiskFilBevarerAllePersonerIndenForRessourcerammen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, CreateGedcom(PersonCount), new UTF8Encoding(false));

        try
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            var tree = new GedcomLoader().Load(path);

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            tree.People.Should().HaveCount(PersonCount);
            tree.FindPerson("@I1@").Should().NotBeNull();
            tree.FindPerson($"@I{PersonCount}@").Should().NotBeNull();
            tree.Diagnostics.Should().BeEmpty();
            allocatedBytes.Should().BeLessThan(
                MaximumAllocatedBytes,
                "10.000 simple personer skal kunne behandles inden for den dokumenterede hukommelsesramme på 512 MB");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_StorFilRespektererCancellationUdenAtPublicereEtDelvistTrae()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllText(path, CreateGedcom(PersonCount), new UTF8Encoding(false));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            var action = () => new GedcomLoader().Load(path, null, cancellation.Token);

            action.Should().Throw<OperationCanceledException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateGedcom(int personCount)
    {
        var content = new StringBuilder(personCount * 100);
        content.AppendLine("0 HEAD");
        content.AppendLine("1 SOUR SlaegtsAssistentBelastningstest");
        content.AppendLine("1 GEDC");
        content.AppendLine("2 VERS 5.5.1");
        content.AppendLine("2 FORM LINEAGE-LINKED");
        content.AppendLine("1 CHAR UTF-8");
        for (var index = 1; index <= personCount; index++)
        {
            content.Append("0 @I").Append(index).AppendLine("@ INDI");
            content.Append("1 NAME Testperson ").Append(index).AppendLine(" /Jensen/");
            content.AppendLine(index % 2 == 0 ? "1 SEX M" : "1 SEX F");
            content.AppendLine("1 BIRT");
            content.Append("2 DATE ").Append(index % 28 + 1).AppendLine(" JAN 1900");
            content.AppendLine("2 PLAC Aarhus");
        }

        content.AppendLine("0 TRLR");
        return content.ToString();
    }
}
