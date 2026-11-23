using FluentAssertions;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.FamilyTrees;
using SlaegtsAssistent.Core.Gedcom;
using System.Numerics;
using System.Text;

namespace SlaegtsAssistent.Core.Tests;

public class FamilyTreeIndexTests
{
    [Fact]
    public void Index_HandlesLargeAncestorNumbersAndStopsCycles()
    {
        var gedcom = new StringBuilder("0 HEAD\n");
        for (var i = 0; i < 70; i++)
            gedcom.AppendLine($"0 @I{i}@ INDI\n1 NAME Person {i}");
        for (var i = 0; i < 69; i++)
            gedcom.AppendLine($"0 @F{i}@ FAM\n1 HUSB @I{i + 1}@\n1 CHIL @I{i}@");
        gedcom.AppendLine("0 @F69@ FAM\n1 HUSB @I0@\n1 CHIL @I69@\n0 TRLR");
        var path = WriteFixture(gedcom.ToString());
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            index.FindAncestor("@I0@", (BigInteger.One << 68).ToString())!.Id.Should().Be("@I68@");
            index.FindAncestor("@I0@", (BigInteger.One << 70).ToString()).Should().BeNull();
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Index_UsesFamilyRolesAndKeepsRepeatedAncestors()
    {
        var path = WriteFixture("""
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Person/
            0 @I2@ INDI
            1 NAME Far /Person/
            0 @I3@ INDI
            1 NAME Mor /Person/
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
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            index.FindAncestor("@I1@", "1")!.Id.Should().Be("@I1@");
            index.FindAncestor("@I1@", "2")!.Id.Should().Be("@I2@");
            index.FindAncestor("@I1@", "3")!.Id.Should().Be("@I3@");
            index.FindAncestor("@I1@", "4")!.Id.Should().Be("@I4@");
            index.FindAncestor("@I1@", "6")!.Id.Should().Be("@I4@");
            index.FindAncestor("@I1@", "5").Should().BeNull();
            index.FindAncestor("@I1@", "-1").Should().BeNull();
            index.Children("@I4@").Select(child => child.Person.Id).Should().Equal("@I2@", "@I3@");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Index_UsesBiologicalFamilyAndLabelsAdoption()
    {
        var path = WriteFixture("""
            0 HEAD
            0 @I1@ INDI
            1 NAME Barn /Test/
            1 FAMC @F1@
            2 PEDI birth
            1 FAMC @F2@
            2 PEDI adopted
            0 @I2@ INDI
            1 NAME Biologisk /Far/
            0 @I3@ INDI
            1 NAME Adoptiv /Far/
            0 @F1@ FAM
            1 HUSB @I2@
            1 CHIL @I1@
            0 @F2@ FAM
            1 HUSB @I3@
            1 CHIL @I1@
            0 TRLR
            """);
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            index.Parents("@I1@").Father!.Id.Should().Be("@I2@");
            index.Parents("@I1@").Mother.Should().BeNull();
            index.Children("@I3@").Should().ContainSingle().Which.Relation.Should().Be("Adoptivbarn");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Index_RespectsRoleSpecificAdoption()
    {
        var path = WriteFixture("""
            0 HEAD
            0 @I1@ INDI
            1 NAME Barn /Test/
            1 FAMC @F1@
            2 PEDI adopted
            1 ADOP
            2 FAMC @F1@
            3 ADOP WIFE
            0 @I2@ INDI
            1 NAME Far /Test/
            0 @I3@ INDI
            1 NAME Mor /Test/
            0 @F1@ FAM
            1 HUSB @I2@
            1 WIFE @I3@
            1 CHIL @I1@
            0 TRLR
            """);
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            index.Parents("@I1@").Father!.Id.Should().Be("@I2@");
            index.Parents("@I1@").Mother.Should().BeNull();
            index.Children("@I2@").Should().ContainSingle().Which.Relation.Should().BeNull();
            index.Children("@I3@").Should().ContainSingle().Which.Relation.Should().Be("Adoptivbarn");
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void NameAndYear_UseOnlyKnownInformation()
    {
        var person = new Person("@I@");
        person.Names.Add(new PersonName("Anna Jensen", "birth"));
        person.Names.Add(new PersonName("Anna Hansen", "married"));
        person.Names.Add(new PersonName("Anna Sommer", "navneændring"));
        person.Names.Add(new PersonName("Anna Alias", "aka"));
        FamilyTreeIndex.FormatName(person).Should().Be("Anna Jensen (Anna Hansen, Anna Sommer)");
        FamilyTreeIndex.FormatYear("ABT 12 MAR 1900").Should().Be("ca. 1900");
        FamilyTreeIndex.FormatYear("BET 1900 AND 1901").Should().Be("Ukendt");
    }

    private static string WriteFixture(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, contents);
        return path;
    }
}
