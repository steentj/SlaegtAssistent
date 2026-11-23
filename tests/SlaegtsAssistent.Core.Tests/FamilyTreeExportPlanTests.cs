using FluentAssertions;
using SlaegtsAssistent.Core.FamilyTrees;
using SlaegtsAssistent.Core.Gedcom;
using System.Numerics;
using System.Text;

namespace SlaegtsAssistent.Core.Tests;

public class FamilyTreeExportPlanTests
{
    [Fact]
    public void Plan_UsesDeepAncestorWithoutChangingMainProband()
    {
        var data = new StringBuilder("0 HEAD\n");
        for (var i = 0; i < 10; i++)
            data.AppendLine($"0 @I{i}@ INDI\n1 NAME Person {i}");
        for (var i = 0; i < 9; i++)
            data.AppendLine($"0 @F{i}@ FAM\n1 HUSB @I{i + 1}@\n1 CHIL @I{i}@");
        data.AppendLine("0 TRLR");
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, data.ToString());
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            var planner = new FamilyTreeExportPlanner(index);
            var plan = planner.Create("@I0@", (BigInteger.One << 7).ToString(), 1, 0);
            plan.MainProbandId.Should().Be("@I0@");
            plan.Root.Id.Should().Be("@I7@");
            plan.Rows[0].Cells[0].Person!.Id.Should().Be("@I8@");
            plan.Rows[0].Cells[0].AncestorNumbers.Should().Equal(2);
            index.FindAncestor("@I0@", "1")!.Id.Should().Be("@I0@");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Plan_CombinesDescendantsInSameGenerationAndStopsCyclesPerPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Test/
            0 @I2@ INDI
            1 NAME A /Test/
            0 @I3@ INDI
            1 NAME B /Test/
            0 @I4@ INDI
            1 NAME Barn /Test/
            0 @F1@ FAM
            1 HUSB @I1@
            1 CHIL @I2@
            1 CHIL @I3@
            0 @F2@ FAM
            1 HUSB @I2@
            1 WIFE @I3@
            1 CHIL @I4@
            0 @F3@ FAM
            1 HUSB @I4@
            1 CHIL @I1@
            0 TRLR
            """);
        try
        {
            var plan = new FamilyTreeExportPlanner(new FamilyTreeIndex(new GedcomLoader().Load(path)))
                .Create("@I1@", "1", 0, 5);
            plan.Rows.Select(row => row.Generation).Should().Equal(0, 1, 2);
            plan.Rows[2].Cells.Should().ContainSingle().Which.Person!.Id.Should().Be("@I4@");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Plan_UsesSelectedAncestorAndNumbersFromNewRoot()
    {
        var path = Fixture();
        try
        {
            var planner = new FamilyTreeExportPlanner(new FamilyTreeIndex(new GedcomLoader().Load(path)));
            var plan = planner.Create("@I1@", "2", 1, 1);
            plan.Root.Id.Should().Be("@I2@");
            plan.OriginalAncestorNumber.Should().Be("2");
            plan.Rows.Select(row => row.Generation).Should().Equal(-1, 0, 1);
            plan.Rows[0].Cells[0].AncestorNumbers.Should().Equal(2);
            plan.Rows[0].Cells[1].Person.Should().BeNull();
            plan.Rows[0].Cells[1].AncestorNumbers.Should().Equal(3);
            plan.Rows[1].Cells.Should().ContainSingle().Which.AncestorNumbers.Should().Equal(1);
            plan.Rows[2].Cells.Should().ContainSingle().Which.Person!.Id.Should().Be("@I1@");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Plan_MergesRepeatedAncestorsAndRejectsInvalidInput()
    {
        var path = Fixture();
        try
        {
            var planner = new FamilyTreeExportPlanner(new FamilyTreeIndex(new GedcomLoader().Load(path)));
            var plan = planner.Create("@I1@", "1", 2, 0);
            plan.Rows[0].Cells.Should().HaveCount(3);
            plan.Rows[0].Cells[0].AncestorNumbers.Should().Equal(4, 6);
            Action invalidDepth = () => planner.Create("@I1@", "1", 3, 3);
            invalidDepth.Should().Throw<ArgumentOutOfRangeException>();
            Action invalidRoot = () => planner.Create("@I1@", "5", 1, 0);
            invalidRoot.Should().Throw<ArgumentException>();
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(4, FamilyTreeExportLayout.Detailed)]
    [InlineData(5, FamilyTreeExportLayout.Detailed)]
    [InlineData(6, FamilyTreeExportLayout.Compact)]
    [InlineData(8, FamilyTreeExportLayout.Compact)]
    [InlineData(9, FamilyTreeExportLayout.Vertical)]
    public void Layout_UsesNumberOfCells(int count, FamilyTreeExportLayout expected)
    {
        var row = new FamilyTreeExportRow(1, "Børn", Enumerable.Range(0, count)
            .Select(_ => new FamilyTreeExportCell(null, [])).ToArray());
        row.Layout.Should().Be(expected);
    }

    [Fact]
    public void Cell_ShowsDetailedFieldsOrCompactYears()
    {
        var person = new FamilyTreePerson("@I@", "Anna Øster", "12 MAR 1900", "Århus", "ABT 1980", "Odense");
        var cell = new FamilyTreeExportCell(person, [2, 6]);
        cell.Format(FamilyTreeExportLayout.Detailed).Should().Equal(
            "Anenummer: 2, 6", "Anna Øster", "Født: 12 MAR 1900", "Fødested: Århus",
            "Død: ABT 1980", "Dødssted: Odense");
        cell.Format(FamilyTreeExportLayout.Vertical).Should().Equal(
            "Anenummer: 2, 6", "Anna Øster", "1900 – ca. 1980");
        new FamilyTreeExportCell(null, [3]).Format(FamilyTreeExportLayout.Detailed)
            .Should().Contain("Fødested: Ukendt");
    }

    private static string Fixture()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Test/
            0 @I2@ INDI
            1 NAME Far /Test/
            0 @I3@ INDI
            1 NAME Mor /Test/
            0 @I4@ INDI
            1 NAME Fælles /Ane/
            0 @F1@ FAM
            1 HUSB @I2@
            1 WIFE @I3@
            1 CHIL @I1@
            0 @F2@ FAM
            1 HUSB @I4@
            1 CHIL @I2@
            0 @F3@ FAM
            1 HUSB @I4@
            1 CHIL @I3@
            0 TRLR
            """);
        return path;
    }
}
