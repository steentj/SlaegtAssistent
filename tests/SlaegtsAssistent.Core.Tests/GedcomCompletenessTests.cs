using System.Text;
using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.Core.Tests;

public class GedcomCompletenessTests
{
    private readonly IGedcomLoader _loader = new GedcomLoader();

    [Fact]
    public void Load_RepresentativGedcom551FixtureBevarerDenAftalteDatakontrakt()
    {
        var tree = _loader.Load(FixturePath("complete-gedcom-551.ged"));

        tree.People.Should().HaveCount(2);
        tree.Families.Should().ContainSingle();
        tree.Sources.Should().ContainSingle();
        tree.Submitter!.Name.Should().Be("Det lokale arkiv");
        tree.FindPerson("@I1@")!.Notes.Should().Equal("Brugerens første note\nBrugerens anden linje");
        tree.FindPerson("@I1@")!.Events.Single().Sources.Single().Note.Should().Be("Tydelig håndskrift");
        tree.FindFamily("@F1@")!.Sources.Single().Page.Should().Be("Familieside 7");
        tree.Diagnostics.Should().ContainSingle(item => item.Tag == "_FLYT");
    }

    [Fact]
    public void Load_StrukturtagsBliverIkkeTilHaendelserOgKendteTagsBevarerRaekkefoelgen()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Anne /Jensen/
            1 SEX F
            1 FAMC @F1@
            1 FAMS @F2@
            1 CHAN
            2 DATE 1 JAN 2026
            1 NOTE Første linje
            2 CONT Anden linje
            2 CONC  med mellemrum
            1 OCCU Snedker
            2 DATE 1920
            1 EMIG
            2 PLAC Norge
            1 _FLYT Flytning
            2 DATE 1930
            0 @F1@ FAM
            1 HUSB @I1@
            1 NCHI 1
            1 CHAN
            2 DATE 2 JAN 2026
            1 NOTE Familienote
            1 DIV
            2 DATE 1940
            1 _SKIL Ukendt familiebegivenhed
            2 PLAC Aarhus
            0 TRLR
            """);

        try
        {
            var tree = _loader.Load(path);
            var person = tree.FindPerson("@I1@")!;
            var family = tree.FindFamily("@F1@")!;

            person.Notes.Should().Equal("Første linje\nAnden linje med mellemrum");
            person.Events.Select(item => item.Tag).Should().Equal("OCCU", "EMIG", "_FLYT");
            family.Notes.Should().Equal("Familienote");
            family.Events.Select(item => item.Tag).Should().Equal("DIV", "_SKIL");
            tree.Diagnostics.Should().Contain(item => item.RecordId == "@I1@" && item.Tag == "_FLYT");
            tree.Diagnostics.Should().Contain(item => item.RecordId == "@F1@" && item.Tag == "_SKIL");
            tree.Diagnostics.Should().NotContain(item =>
                item.Tag == "FAMC"
                || item.Tag == "FAMS"
                || item.Tag == "CHAN"
                || item.Tag == "NOTE"
                || item.Tag == "NCHI");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_CitationerBevarerFelterOgFortsatteTeksterIAlleUnderstoettedeKontekster()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Anne /Jensen/
            1 SOUR @S1@
            2 PAGE Personside
            2 DATA Persondata
            3 DATE 1900
            3 TEXT Persontekst
            4 CONT næste linje
            2 NOTE Personcitation
            3 CONC  fortsat
            1 BIRT
            2 SOUR @S1@
            3 PAGE Hændelsesside
            3 DATA Hændelsesdata
            4 DATE 1901
            4 TEXT Hændelsestekst
            5 CONT næste linje
            3 NOTE Hændelsescitation
            1 CENS
            2 SOUR @S1@
            3 PAGE Censusside
            3 DATA Censusdata
            4 DATE 1902
            4 TEXT Censustekst
            3 NOTE Censuscitation
            0 @F1@ FAM
            1 HUSB @I1@
            1 SOUR @S1@
            2 PAGE Familieside
            2 DATA Familiedata
            3 DATE 1903
            3 TEXT Familietekst
            2 NOTE Familiecitation
            1 MARR
            2 SOUR @S1@
            3 PAGE Vielsesside
            3 DATA Vielsesdata
            4 DATE 1904
            4 TEXT Vielsesvidne
            3 NOTE Vielsescitation
            0 @S1@ SOUR
            1 TITL Kirkebog
            2 CONT bind 1
            1 TEXT Original
            2 CONC  tekst
            1 NOTE Kildenote
            0 TRLR
            """);

        try
        {
            var tree = _loader.Load(path);
            var person = tree.FindPerson("@I1@")!;
            var family = tree.FindFamily("@F1@")!;

            AssertCitation(person.Sources.Single(), "Personside", "Persondata", "1900", "Persontekst\nnæste linje", "Personcitation fortsat");
            AssertCitation(person.Events.Single().Sources.Single(), "Hændelsesside", "Hændelsesdata", "1901", "Hændelsestekst\nnæste linje", "Hændelsescitation");
            AssertCitation(person.Census.Single().Sources.Single(), "Censusside", "Censusdata", "1902", "Censustekst", "Censuscitation");
            AssertCitation(family.Sources.Single(), "Familieside", "Familiedata", "1903", "Familietekst", "Familiecitation");
            AssertCitation(family.Events.Single().Sources.Single(), "Vielsesside", "Vielsesdata", "1904", "Vielsesvidne", "Vielsescitation");
            tree.FindSource("@S1@")!.Title.Should().Be("Kirkebog\nbind 1");
            tree.FindSource("@S1@")!.Text.Should().Be("Original tekst");
            tree.FindSource("@S1@")!.Note.Should().Be("Kildenote");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("UTF-8")]
    [InlineData("ASCII")]
    [InlineData("UNICODE")]
    public void Load_RespektererStandardtegnsaet(string characterSet)
    {
        var text = $"0 HEAD\r\n1 CHAR {characterSet}\r\n0 @I1@ INDI\r\n1 NAME Anna /Jensen/\r\n0 TRLR\r\n";
        var bytes = characterSet switch
        {
            "UNICODE" => Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes(text)).ToArray(),
            "ASCII" => Encoding.ASCII.GetBytes(text),
            _ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes(text),
        };
        var path = CreateFixture(bytes);

        try
        {
            _loader.Load(path).FindPerson("@I1@")!.FullName.Should().Be("Anna Jensen");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_AnselAfkoderDanskeTegnOgDiakritika()
    {
        var prefix = Encoding.ASCII.GetBytes("0 HEAD\r\n1 CHAR ANSEL\r\n0 @I1@ INDI\r\n1 NAME ");
        var name = new byte[]
        {
            0xEA, (byte)'A', (byte)'g', (byte)'e', (byte)' ',
            (byte)'/', (byte)'M', 0xB2, (byte)'l', (byte)'l', (byte)'e', (byte)'r', (byte)'/',
        };
        var suffix = Encoding.ASCII.GetBytes("\r\n0 TRLR\r\n");
        var path = CreateFixture(prefix.Concat(name).Concat(suffix).ToArray());

        try
        {
            _loader.Load(path).FindPerson("@I1@")!.FullName.Should().Be("Åge Møller");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UnicodeBigEndianAfkodesEfterBomOgHeader()
    {
        const string text = "0 HEAD\r\n1 CHAR UNICODE\r\n0 @I1@ INDI\r\n1 NAME Åse /Østergaard/\r\n0 TRLR\r\n";
        var encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true, throwOnInvalidBytes: true);
        var path = CreateFixture(encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());

        try
        {
            _loader.Load(path).FindPerson("@I1@")!.FullName.Should().Be("Åse Østergaard");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_ModstridendeEllerUunderstoettetTegnsaetAfvisesUdenLydloesKorruption()
    {
        var conflictingText = "0 HEAD\r\n1 CHAR ANSEL\r\n0 @I1@ INDI\r\n1 NAME Åge /Jensen/\r\n0 TRLR\r\n";
        var conflictingBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(conflictingText)).ToArray();
        var conflictingPath = CreateFixture(conflictingBytes);
        var unsupportedPath = CreateFixture("0 HEAD\n1 CHAR IBMPC\n0 TRLR\n");

        try
        {
            var conflictingAction = () => _loader.Load(conflictingPath);
            var unsupportedAction = () => _loader.Load(unsupportedPath);

            conflictingAction.Should().Throw<GedcomLoadException>().WithMessage("*modstridende*");
            unsupportedAction.Should().Throw<GedcomLoadException>().WithMessage("*understøttes ikke*");
        }
        finally
        {
            File.Delete(conflictingPath);
            File.Delete(unsupportedPath);
        }
    }

    [Fact]
    public void Load_UkendtAnselByteAfvisesUdenErstatningstegn()
    {
        var prefix = Encoding.ASCII.GetBytes("0 HEAD\r\n1 CHAR ANSEL\r\n0 @I1@ INDI\r\n1 NAME ");
        var suffix = Encoding.ASCII.GetBytes(" /Jensen/\r\n0 TRLR\r\n");
        var path = CreateFixture(prefix.Concat(new byte[] { 0xFF }).Concat(suffix).ToArray());

        try
        {
            var action = () => _loader.Load(path);

            action.Should().Throw<GedcomLoadException>().WithMessage("*ANSEL-byte 0xFF*");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_AlleMappedeStandardhaendelserBevaresUdenUkendtDiagnostik()
    {
        string[] personTags =
        [
            "ADOP", "BAPL", "BAPM", "BARM", "BASM", "BIRT", "BLES", "BURI", "CAST", "CHR",
            "CHRA", "CONF", "CONL", "CREM", "DEAT", "DSCR", "EDUC", "EMIG", "ENDL", "EVEN",
            "FACT", "FCOM", "GRAD", "IDNO", "IMMI", "NATI", "NATU", "NCHI", "NMR", "OCCU",
            "ORDN", "PROB", "PROP", "RELI", "RESI", "RETI", "SLGC", "SSN", "TITL", "WILL",
        ];
        string[] familyTags =
        [
            "ANUL", "CENS", "DIV", "DIVF", "ENGA", "EVEN", "MARB", "MARC", "MARL", "MARR",
            "MARS", "RESI", "SLGS",
        ];
        var gedcom = new StringBuilder("0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Anne /Jensen/\n");
        foreach (var tag in personTags)
        {
            gedcom.Append("1 ").Append(tag).Append(" værdi\n");
        }

        gedcom.Append("0 @F1@ FAM\n1 HUSB @I1@\n");
        foreach (var tag in familyTags)
        {
            gedcom.Append("1 ").Append(tag).Append(" værdi\n");
        }

        gedcom.Append("0 TRLR\n");
        var path = CreateFixture(gedcom.ToString());

        try
        {
            var tree = _loader.Load(path);

            tree.FindPerson("@I1@")!.Events.Select(item => item.Tag).Should().Equal(personTags);
            tree.FindFamily("@F1@")!.Events.Select(item => item.Tag).Should().Equal(familyTags);
            tree.Diagnostics.Should().NotContain(item => item.Tag != null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_BevarerRaasegmentetsLinjeskiftOgGiverDeterministiskOutput()
    {
        const string gedcom = "0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NAME Anne /Jensen/\n1 BIRT\n2 DATE 1900\n0 @I2@ INDI\n1 NAME Bent /Jensen/\n0 TRLR\n";
        const string expectedRaw = "0 @I1@ INDI\n1 NAME Anne /Jensen/\n1 BIRT\n2 DATE 1900\n";
        var path = CreateFixture(gedcom);

        try
        {
            var first = _loader.Load(path);
            var second = _loader.Load(path);

            first.FindPerson("@I1@")!.RawGedcom.Should().Be(expectedRaw);
            first.People.Select(person => person.RecordId).Should().Equal(second.People.Select(person => person.RecordId));
            first.FindPerson("@I1@")!.Events.Select(item => item.Tag)
                .Should().Equal(second.FindPerson("@I1@")!.Events.Select(item => item.Tag));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BiographyContext_MedtagerFamiliekilderOgCitationensNote()
    {
        var path = CreateFixture(
            """
            0 HEAD
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Anne /Jensen/
            0 @F1@ FAM
            1 HUSB @I1@
            1 SOUR Lokal familiebog
            2 NOTE Kontrolleret af arkivaren
            0 TRLR
            """);

        try
        {
            var person = _loader.Load(path).FindPerson("@I1@")!;
            var context = BiographyTemplateContext.FromPerson(person);

            context.Sources.Should().ContainSingle();
            context.Sources[0].Title.Should().Be("Lokal familiebog");
            context.Sources[0].Note.Should().Be("Kontrolleret af arkivaren");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void BiographyContext_SammenklapperIkkeForskelligeCitationerTilSammeKilderecord()
    {
        var person = _loader.Load(FixturePath("complete-gedcom-551.ged")).FindPerson("@I1@")!;

        var sources = BiographyTemplateContext.FromPerson(person).Sources;

        sources.Select(source => source.Page).Should().Equal(
            "Side 42",
            "Familieside 7",
            "Vielse 1920, side 18",
            "Folketælling 1911, opslag 8");
    }

    private static void AssertCitation(
        Core.Domain.Source source,
        string page,
        string data,
        string date,
        string text,
        string note)
    {
        source.Page.Should().Be(page);
        source.Data.Should().Be(data);
        source.Date.Should().Be(date);
        source.Text.Should().Be(text);
        source.Note.Should().Be(note);
    }

    private static string CreateFixture(string content)
    {
        return CreateFixture(new UTF8Encoding(false, true).GetBytes(content));
    }

    private static string FixturePath(string fileName)
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            ".."));
        return Path.Combine(projectDirectory, "Fixtures", "Gedcom", fileName);
    }

    private static string CreateFixture(byte[] content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        File.WriteAllBytes(path, content);
        return path;
    }
}
