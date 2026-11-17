using System.Text.RegularExpressions;
using FluentAssertions;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.Core.Tests;

public sealed class BiographyTemplateNotesTests
{
    [Fact]
    public void Standardmodellen_TilvaelgerAlleNyeNotatfelter()
    {
        var model = BiographyTemplateEditorModel.CreateDefault();

        foreach (var (type, field) in NoteFields())
        {
            model.Blocks.Single(block => block.Type == type).Fields.Should().Contain(field);
            BiographyTemplateEditorCatalog.GetFields(type).Select(item => item.Field).Should().Contain(field);
        }

        var person = CreatePerson();
        var snapshot = CanonicalBiographySnapshot.Create(person);
        var generator = new BiographyTemplateMarkdownGenerator();
        var direct = BiographyDocumentParser.Parse(generator.Generate(person)).Body;
        var approved = BiographyDocumentParser.Parse(generator.Generate(snapshot, snapshot)).Body;

        foreach (var note in ExpectedNotes)
        {
            direct.Should().Contain(note);
            approved.Should().Contain(note);
        }
    }

    [Fact]
    public void Familienotater_ErTilgaengeligeISkabelonOgSnapshotIInputraekkefoelge()
    {
        var person = CreatePerson();
        var snapshot = CanonicalBiographySnapshot.Create(person);
        person.Families[0].Notes[0] = "Ændret efter godkendelse";
        const string source = "{{#each person.familyNotes }}[{{ text }}]{{/each}}";
        var template = new BiographyTemplateLoader().Parse(source);
        var renderer = new BiographyTemplateRenderer();

        var direct = renderer.Render(template, BiographyTemplateContext.FromPerson(person));
        var approved = renderer.Render(template, BiographyTemplateContext.FromSnapshot(snapshot));

        direct.Should().Be("[Ændret efter godkendelse][Andet familienotat][Tredje familienotat]");
        approved.Should().Be("[Første familienotat][Andet familienotat][Tredje familienotat]");
        BiographyTemplateContract.PublicFieldPaths.Should().Contain("person.familyNotes");
        BiographyTemplateContract.PublicFieldPaths.Should().Contain("person.familyNotes[].text");
        BiographyTemplateEditorCatalog.GetField(BiographyTemplateField.FamilyNote).TechnicalPaths
            .Should().Contain("person.familyNotes");
    }

    [Fact]
    public void GodkendtSnapshot_StyrerNotaterSelvOmImportenErNyere()
    {
        var person = CreatePerson();
        var approved = CanonicalBiographySnapshot.Create(person);
        person.Families[0].Notes[0] = "Ny importeret familienote";
        person.Events[0].Note = "Ny importeret hændelsesnote";
        person.Sources[0].Note = "Ny importeret kildenote";
        person.Media[0].Note = "Ny importeret medienote";
        var imported = CanonicalBiographySnapshot.Create(person);

        var body = BiographyDocumentParser.Parse(
            new BiographyTemplateMarkdownGenerator().Generate(approved, imported)).Body;

        body.Should().Contain("Første familienotat")
            .And.Contain("Personhændelsens notat")
            .And.Contain("Kildens notat")
            .And.Contain("Mediets notat")
            .And.NotContain("Ny importeret");
    }

    [Fact]
    public void AeldreGemtModel_BevarerFravalgOgKanTilvaelgeNotater()
    {
        const string legacyJson = """
            {
              "version": 1,
              "blocks": [
                { "type": "Facts", "isVisible": true, "heading": "Fakta", "presentation": "List", "fields": ["Birth"] },
                { "type": "Events", "isVisible": true, "heading": "Hændelser", "presentation": "List", "fields": ["EventCategory"] },
                { "type": "Sources", "isVisible": true, "heading": "Kilder", "presentation": "List", "fields": ["SourceTitle"] },
                { "type": "Media", "isVisible": true, "heading": "Medier", "presentation": "Standard", "fields": ["MediaTitle", "MediaRelativeFile"] }
              ]
            }
            """;
        var serializer = new BiographyTemplateEditorSerializer();
        var legacy = serializer.Deserialize(legacyJson);
        var reloaded = serializer.Deserialize(serializer.Serialize(legacy));
        var person = CreatePerson();
        var without = Render(reloaded, BiographyTemplateContext.FromPerson(person));

        foreach (var (type, field) in NoteFields())
        {
            reloaded.Blocks.Single(block => block.Type == type).Fields.Should().NotContain(field);
        }
        foreach (var note in ExpectedNotes)
        {
            without.Should().NotContain(note);
        }

        var selected = new BiographyTemplateEditorModel(
            reloaded.Version,
            reloaded.Blocks.Select(block =>
            {
                var field = NoteFields().Single(item => item.Type == block.Type).Field;
                return new BiographyTemplateBlock(
                    block.Type, block.IsVisible, block.Heading, block.Presentation,
                    block.Fields.Append(field).ToArray());
            }).ToArray());
        var restored = serializer.Deserialize(serializer.Serialize(selected));
        var with = Render(restored, BiographyTemplateContext.FromPerson(person));

        foreach (var note in ExpectedNotes)
        {
            with.Should().Contain(note);
        }
    }

    [Theory]
    [InlineData(BiographyTemplateBlockType.Facts, BiographyTemplateField.FamilyNote, "Første familienotat")]
    [InlineData(BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote, "Personhændelsens notat")]
    [InlineData(BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote, "Kildens notat")]
    [InlineData(BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote, "Mediets notat")]
    public void KunValgtNotatfelt_ViserNotatForPersonOgSnapshot(
        BiographyTemplateBlockType type, BiographyTemplateField field, string expected)
    {
        var person = CreatePerson();
        var model = Model(type, PresentationFor(type), field);
        var snapshot = CanonicalBiographySnapshot.Create(person);

        var direct = Render(model, BiographyTemplateContext.FromPerson(person));
        var approved = Render(model, BiographyTemplateContext.FromSnapshot(snapshot));

        direct.Should().Contain(expected);
        approved.Should().Contain(expected);
        if (type == BiographyTemplateBlockType.Events)
        {
            direct.Should().Contain("Familiehændelsens notat");
            approved.Should().Contain("Familiehændelsens notat");
        }
        foreach (var other in ExpectedNotes.Where(note => note != expected &&
                     !(type == BiographyTemplateBlockType.Events && note == "Familiehændelsens notat") &&
                     !(type == BiographyTemplateBlockType.Facts && note.Contains("familienotat", StringComparison.Ordinal))))
        {
            direct.Should().NotContain(other);
        }
    }

    [Theory]
    [InlineData(BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote, BiographyTemplateField.EventDate)]
    [InlineData(BiographyTemplateBlockType.Census, BiographyTemplateField.CensusNote, BiographyTemplateField.CensusDate)]
    [InlineData(BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote, BiographyTemplateField.SourceTitle)]
    [InlineData(BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote, BiographyTemplateField.MediaTitle)]
    public void FlerlinjedeNotater_ErSikreIListeOgTabel(
        BiographyTemplateBlockType type, BiographyTemplateField noteField, BiographyTemplateField otherField)
    {
        const string note = "Første | *ord*\r\n\r\n## Falsk overskrift\n- Falsk punkt\n<script>";
        var person = new Person("@I1@") { FullName = "Anna" };
        switch (type)
        {
            case BiographyTemplateBlockType.Events:
                person.Events.Add(new GedcomEvent("EVEN") { Date = "1900", Note = note });
                break;
            case BiographyTemplateBlockType.Census:
                person.Census.Add(new Census { Date = "1900", Note = note });
                break;
            case BiographyTemplateBlockType.Sources:
                person.Sources.Add(new Source("@S1@") { Title = "Kilde", Note = note });
                break;
            case BiographyTemplateBlockType.Media:
                person.Media.Add(new Media("@M1@") { Title = "Medie", Note = note });
                break;
        }
        var snapshot = CanonicalBiographySnapshot.Create(person);

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person),
                     BiographyTemplateContext.FromSnapshot(snapshot),
                 })
        {
            var list = Render(Model(type, BiographyTemplatePresentation.List, otherField, noteField), context);
            list.Should().Contain("Første").And.Contain("Falsk overskrift").And.Contain("Falsk punkt");
            list.Should().NotContain("\n## Falsk overskrift").And.NotContain("\n- Falsk punkt")
                .And.NotContain("<script>").And.Contain("\\*ord\\*").And.Contain("\\|");

            var table = Render(Model(type, BiographyTemplatePresentation.Table, otherField, noteField), context);
            var rows = table.Split('\n').Where(line => line.StartsWith('|')).ToArray();
            rows.Should().HaveCount(3);
            rows[2].Should().Contain("Første").And.Contain("Falsk overskrift").And.Contain("Falsk punkt")
                .And.Contain("\\*ord\\*").And.NotContain("<script>");
            Regex.Matches(rows[2], @"(?<!\\)\|").Count.Should().Be(3);
            table.Should().NotContain("\n## Falsk overskrift").And.NotContain("\n- Falsk punkt");
        }
    }

    [Theory]
    [InlineData(BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote)]
    [InlineData(BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote)]
    [InlineData(BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote)]
    public void KunNotatfelt_UdeladerTommePunkter(BiographyTemplateBlockType type, BiographyTemplateField field)
    {
        var person = new Person("@I1@");
        person.Events.Add(new GedcomEvent("EVEN") { Note = "  " });
        person.Sources.Add(new Source("@S1@") { Note = "  " });
        person.Media.Add(new Media("@M1@") { Note = "  " });

        var output = Render(Model(type, BiographyTemplatePresentation.List, field), BiographyTemplateContext.FromPerson(person));

        output.Should().NotContain("-").And.NotContain("Notat");
    }

    [Fact]
    public void Familienotater_OgMedienotater_EscaperFlerlinjetMarkdown()
    {
        var person = new Person("@I1@");
        var family = new Family("@F1@");
        family.Notes.Add("Først\n\n## Ikke overskrift\n- Ikke punkt\n<script>");
        person.Families.Add(family);
        person.Media.Add(new Media("@M1@") { Note = "Medie\n\n## Ikke overskrift\n- Ikke punkt\n<script>" });
        var snapshot = CanonicalBiographySnapshot.Create(person);

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person),
                     BiographyTemplateContext.FromSnapshot(snapshot),
                 })
        {
            foreach (var model in new[]
                     {
                         Model(BiographyTemplateBlockType.Facts, BiographyTemplatePresentation.List, BiographyTemplateField.FamilyNote),
                         Model(BiographyTemplateBlockType.Media, BiographyTemplatePresentation.Standard, BiographyTemplateField.MediaNote),
                     })
            {
                var output = Render(model, context);
                output.Should().Contain("Ikke overskrift").And.Contain("Ikke punkt")
                    .And.NotContain("\n## Ikke overskrift").And.NotContain("\n- Ikke punkt")
                    .And.NotContain("<script>");
            }
        }
    }

    [Fact]
    public void Kildenotater_FraAlleKildeplaceringerVisesOgsaaFraSnapshot()
    {
        var person = new Person("@I1@");
        var personEvent = new GedcomEvent("BIRT");
        personEvent.Sources.Add(new Source("@S1@") { Note = "Kilde til personhændelse" });
        person.Events.Add(personEvent);
        var family = new Family("@F1@");
        family.Sources.Add(new Source("@S2@") { Note = "Kilde til familie" });
        var familyEvent = new GedcomEvent("MARR");
        familyEvent.Sources.Add(new Source("@S3@") { Note = "Kilde til familiehændelse" });
        family.Events.Add(familyEvent);
        person.Families.Add(family);
        var census = new Census();
        census.Sources.Add(new Source("@S4@") { Note = "Kilde til folketælling" });
        person.Census.Add(census);
        var model = Model(BiographyTemplateBlockType.Sources, BiographyTemplatePresentation.List,
            BiographyTemplateField.SourceNote);

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person),
                     BiographyTemplateContext.FromSnapshot(CanonicalBiographySnapshot.Create(person)),
                 })
        {
            Render(model, context).Should().Contain("Kilde til personhændelse")
                .And.Contain("Kilde til familie")
                .And.Contain("Kilde til familiehændelse")
                .And.Contain("Kilde til folketælling");
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Medienotat_VisesUdenTilgaengeligFilOgUdenValgtTitel(bool includeFile, bool includeTitle)
    {
        var person = new Person("@I1@");
        person.Media.Add(new Media("@M1@")
        {
            File = "fil-der-ikke-findes-" + Guid.NewGuid().ToString("N") + ".jpg",
            Title = "Skjult titel",
            Note = "Notat uden fil",
        });
        var fields = new List<BiographyTemplateField> { BiographyTemplateField.MediaNote };
        if (includeFile) fields.Add(BiographyTemplateField.MediaRelativeFile);
        if (includeTitle) fields.Add(BiographyTemplateField.MediaTitle);
        var model = Model(BiographyTemplateBlockType.Media, BiographyTemplatePresentation.Standard, fields.ToArray());
        var snapshot = CanonicalBiographySnapshot.Create(person);
        var directory = Directory.GetCurrentDirectory();

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person, mediaBaseDirectory: directory, gedcomSourceDirectory: directory),
                     BiographyTemplateContext.FromSnapshot(snapshot, mediaBaseDirectory: directory, gedcomSourceDirectory: directory),
                 })
        {
            context.Media.Single().RelativeFile.Should().BeNull();
            var output = Render(model, context);
            output.Should().Contain("Notat uden fil").And.NotContain("![");
            if (!includeTitle) output.Should().NotContain("Skjult titel");
        }
    }

    [Fact]
    public void Medienotat_VisesMedFilSelvNaarTitlenErFravalgt()
    {
        var person = new Person("@I1@");
        person.Media.Add(new Media("@M1@") { File = "foto.jpg", Title = "Skjult titel", Note = "Mediets notat" });
        var model = Model(BiographyTemplateBlockType.Media, BiographyTemplatePresentation.Standard,
            BiographyTemplateField.MediaRelativeFile, BiographyTemplateField.MediaNote);

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person),
                     BiographyTemplateContext.FromSnapshot(CanonicalBiographySnapshot.Create(person)),
                 })
        {
            Render(model, context).Should().Contain("foto.jpg").And.Contain("Mediets notat").And.NotContain("![")
                .And.NotContain("Skjult titel");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MedieUdenNotat_HarUændretOutputSelvNaarNotatErTilvalgt(string? note)
    {
        var person = new Person("@I1@");
        person.Media.Add(new Media("@M1@") { File = "foto.jpg", Title = "Portræt", Note = note });
        var without = Model(BiographyTemplateBlockType.Media, BiographyTemplatePresentation.Standard,
            BiographyTemplateField.MediaTitle, BiographyTemplateField.MediaRelativeFile);
        var with = Model(BiographyTemplateBlockType.Media, BiographyTemplatePresentation.Standard,
            BiographyTemplateField.MediaTitle, BiographyTemplateField.MediaRelativeFile, BiographyTemplateField.MediaNote);

        foreach (var context in new[]
                 {
                     BiographyTemplateContext.FromPerson(person),
                     BiographyTemplateContext.FromSnapshot(CanonicalBiographySnapshot.Create(person)),
                 })
        {
            var oldOutput = Render(without, context);
            oldOutput.Should().Be("## Medier\n- Portræt foto.jpg\n\n");
            Render(with, context).Should().Be(oldOutput);
        }
    }

    private static readonly string[] ExpectedNotes =
    [
        "Første familienotat", "Andet familienotat", "Tredje familienotat",
        "Personhændelsens notat", "Familiehændelsens notat", "Kildens notat", "Mediets notat",
    ];

    private static (BiographyTemplateBlockType Type, BiographyTemplateField Field)[] NoteFields() =>
    [
        (BiographyTemplateBlockType.Facts, BiographyTemplateField.FamilyNote),
        (BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote),
        (BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote),
        (BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote),
    ];

    private static Person CreatePerson()
    {
        var person = new Person("@I1@") { FullName = "Anna", BirthDate = "1900" };
        var firstFamily = new Family("@F1@");
        firstFamily.Notes.Add("Første familienotat");
        firstFamily.Notes.Add("Andet familienotat");
        firstFamily.Events.Add(new GedcomEvent("MARR")
        {
            Category = GedcomEventCategory.Marriage,
            Note = "Familiehændelsens notat",
        });
        var secondFamily = new Family("@F2@");
        secondFamily.Notes.Add("Tredje familienotat");
        person.Families.Add(firstFamily);
        person.Families.Add(secondFamily);
        person.Events.Add(new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Note = "Personhændelsens notat",
        });
        person.Sources.Add(new Source("@S1@") { Title = "Kirkebog", Note = "Kildens notat" });
        person.Media.Add(new Media("@M1@") { File = "foto.jpg", Title = "Portræt", Note = "Mediets notat" });
        return person;
    }

    private static BiographyTemplatePresentation PresentationFor(BiographyTemplateBlockType type) =>
        type == BiographyTemplateBlockType.Media
            ? BiographyTemplatePresentation.Standard
            : BiographyTemplatePresentation.List;

    private static BiographyTemplateEditorModel Model(
        BiographyTemplateBlockType type,
        BiographyTemplatePresentation presentation,
        params BiographyTemplateField[] fields) =>
        new(BiographyTemplateEditorModel.CurrentVersion,
            [new BiographyTemplateBlock(type, true, BiographyTemplateEditorCatalog.GetBlock(type).Label, presentation, fields)]);

    private static string Render(BiographyTemplateEditorModel model, BiographyTemplateContext context)
    {
        var source = new BiographyTemplateEditorCompiler().Compile(model);
        var template = new BiographyTemplateLoader().Parse(source);
        return new BiographyTemplateRenderer().Render(template, context);
    }
}
