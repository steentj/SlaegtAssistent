using System.Net;
using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;
using SlaegtsAssistent.Core.Gedcom;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorNotesPreviewTests
{
    private static readonly (BiographyTemplateBlockType Block, BiographyTemplateField Field)[] NoteFields =
    [
        (BiographyTemplateBlockType.Facts, BiographyTemplateField.FamilyNote),
        (BiographyTemplateBlockType.Events, BiographyTemplateField.EventNote),
        (BiographyTemplateBlockType.Sources, BiographyTemplateField.SourceNote),
        (BiographyTemplateBlockType.Media, BiographyTemplateField.MediaNote),
    ];

    [Theory]
    [InlineData(BiographyTemplatePresentation.List)]
    [InlineData(BiographyTemplatePresentation.Table)]
    public async Task NotatetsTidspunkt_VisesIKursivIForhaandsvisningen(BiographyTemplatePresentation presentation)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ged");
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");
        try
        {
            File.WriteAllText(path,
                "0 HEAD\n1 CHAR UTF-8\n0 @I1@ INDI\n1 NOTE @N1@\n0 @N1@ NOTE Tekst\n" +
                "1 CHAN\n2 DATE 20 OCT 2023\n3 TIME 14:03:19\n" +
                "1 CREA\n2 DATE 20 OCT 2023\n3 TIME 14:02:20\n0 TRLR\n");
            var person = new GedcomLoader().Load(path).FindPerson("@I1@")!;
            var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
                [new(BiographyTemplateBlockType.Facts, true, "Fakta", presentation, [BiographyTemplateField.PersonNote])]);

            var preview = await new TemplateEditorPreviewService().RenderAsync(model, person, null, CancellationToken.None);

            preview.SafePreview.Html.Should().Contain("Tekst <em>(20-10-2023 14:03)</em>")
                .And.NotContain("14:02:20");
            preview.SafePreview.NetworkRequestAttempts.Should().Be(0);
        }
        finally
        {
            File.Delete(path);
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task DefaultPreview_ShouldShowNotesFromEveryExampleDataCategory()
    {
        var example = BiographyTemplateExampleData.Create().Person;
        var notes = new[]
        {
            example.Families.SelectMany(family => family.Notes).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)),
            example.Events.Concat(example.FamilyEvents).Select(item => item.Note).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)),
            example.Sources.Select(item => item.Note).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)),
            example.Media.Select(item => item.Note).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note)),
        };
        notes.Should().OnlyContain(note => !string.IsNullOrWhiteSpace(note));
        var model = BiographyTemplateEditorModel.CreateDefault();
        foreach (var (block, field) in NoteFields)
        {
            model.Blocks.Single(item => item.Type == block).Fields.Should().Contain(field);
        }

        var viewModel = new TemplateEditorViewModel(model, previewDelay: TimeSpan.Zero);
        try
        {
            await viewModel.PreviewUpdateTask;

            viewModel.PreviewErrorMessage.Should().BeNull();
            viewModel.SelectedPreviewPerson!.IsExample.Should().BeTrue();
            var text = PlainText(viewModel.PreviewHtmlDocument);
            foreach (var note in notes)
            {
                foreach (var line in note!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                {
                    text.Should().Contain(line.Trim());
                }
            }
            viewModel.PreviewNetworkRequestAttempts.Should().Be(0);
        }
        finally
        {
            viewModel.StopPreview();
        }
    }

    [Fact]
    public async Task OlderModel_ShouldKeepNotesUnselectedUntilEnabledInViewModel()
    {
        var standard = BiographyTemplateEditorModel.CreateDefault();
        var oldModel = new BiographyTemplateEditorModel(standard.Version, standard.Blocks.Select(block =>
            new BiographyTemplateBlock(block.Type, block.IsVisible, block.Heading, block.Presentation,
                block.Fields.Where(field => !NoteFields.Any(note => note.Field == field)).ToArray())).ToArray());
        var person = CreatePersonWithNotes();
        var viewModel = new TemplateEditorViewModel(oldModel, previewPeople: [person], previewDelay: TimeSpan.Zero);
        try
        {
            viewModel.SelectedPreviewPerson = viewModel.PreviewPeople.Single(option => !option.IsExample);
            await viewModel.PreviewUpdateTask;
            var noteTexts = new[] { "Familie først", "Hændelse først", "Kilde først", "Medie først" };
            foreach (var text in noteTexts)
            {
                viewModel.PreviewHtmlDocument.Should().NotContain(text);
            }

            for (var index = 0; index < NoteFields.Length; index++)
            {
                var (block, field) = NoteFields[index];
                viewModel.SelectedBlock = viewModel.Blocks.Single(item => item.Type == block);
                var option = viewModel.Fields.Single(item => item.Field == field);
                option.IsSelected.Should().BeFalse();
                oldModel.Blocks.Single(item => item.Type == block).Fields.Should().NotContain(field);

                viewModel.ToggleFieldCommand.Execute(option);
                await viewModel.PreviewUpdateTask;

                viewModel.PreviewErrorMessage.Should().BeNull();
                viewModel.Fields.Single(item => item.Field == field).IsSelected.Should().BeTrue();
                viewModel.CreateModel().Blocks.Single(item => item.Type == block).Fields.Should().Contain(field);
                viewModel.PreviewHtmlDocument.Should().Contain(noteTexts[index]);
            }

            foreach (var (block, field) in NoteFields)
            {
                oldModel.Blocks.Single(item => item.Type == block).Fields.Should().NotContain(field);
            }
        }
        finally
        {
            viewModel.StopPreview();
        }
    }

    [Fact]
    public async Task SelectedPerson_ListPreview_ShouldKeepMultilineNotesInsideTheirItems()
    {
        var viewModel = new TemplateEditorViewModel(
            CreateNotesModel(BiographyTemplatePresentation.List),
            previewPeople: [CreatePersonWithNotes()],
            previewDelay: TimeSpan.Zero);
        try
        {
            await viewModel.PreviewUpdateTask;
            viewModel.SelectedPreviewPerson = viewModel.PreviewPeople.Single(option => !option.IsExample);
            await viewModel.PreviewUpdateTask;

            viewModel.PreviewErrorMessage.Should().BeNull();
            var html = viewModel.PreviewHtmlDocument;
            html.Should().Contain("Bodil Sørensen").And.NotContain("Anna Marie Jensen");
            var facts = Section(html, "Fakta");
            var events = Section(html, "Hændelser");
            var sources = Section(html, "Kilder");
            var media = Section(html, "Medier");
            var familyItems = Elements(facts, "li");
            var eventItems = Elements(events, "li");
            var sourceItems = Elements(sources, "li");
            familyItems.Should().ContainSingle();
            eventItems.Should().HaveCount(4);
            sourceItems.Should().HaveCount(2);
            AssertLiteralMultiline(familyItems[0], "Familie først");
            AssertLiteralMultiline(eventItems[0], "Hændelse først");
            PlainText(eventItems[2]).Should().Contain("Familiehændelsens notat");
            AssertLiteralMultiline(sourceItems[0], "Kilde først");
            AssertLiteralMultiline(media, "Medie først");
            eventItems[1].Should().NotContain("Notat");
            eventItems[3].Should().NotContain("Notat");
            sourceItems[1].Should().NotContain("Notat");
            html.Should().NotContain("<h2>Falsk overskrift</h2>")
                .And.NotContain("<h3>Falsk overskrift</h3>")
                .And.NotContain("<em>falsk</em>")
                .And.NotContain("<a href=")
                .And.NotContain("<mark>");
            viewModel.PreviewNetworkRequestAttempts.Should().Be(0);
        }
        finally
        {
            viewModel.StopPreview();
        }
    }

    [Fact]
    public async Task ActualPerson_TablePreview_ShouldKeepMultilineNotesAndEmptyValuesInTheirCells()
    {
        var result = await new TemplateEditorPreviewService().RenderAsync(
            CreateNotesModel(BiographyTemplatePresentation.Table),
            CreatePersonWithNotes(),
            null,
            CancellationToken.None);

        var html = result.SafePreview.Html;
        var eventRows = Elements(Section(html, "Hændelser"), "tr");
        var sourceRows = Elements(Section(html, "Kilder"), "tr");
        eventRows.Should().HaveCount(5);
        sourceRows.Should().HaveCount(3);
        var eventCells = Elements(eventRows[1], "td");
        var emptyEventCells = Elements(eventRows[2], "td");
        var familyEventCells = Elements(eventRows[3], "td");
        var emptyFamilyEventCells = Elements(eventRows[4], "td");
        var sourceCells = Elements(sourceRows[1], "td");
        var emptySourceCells = Elements(sourceRows[2], "td");
        eventCells.Should().HaveCount(2);
        emptyEventCells.Should().HaveCount(2);
        familyEventCells.Should().HaveCount(2);
        emptyFamilyEventCells.Should().HaveCount(2);
        sourceCells.Should().HaveCount(2);
        emptySourceCells.Should().HaveCount(2);
        AssertLiteralMultiline(eventCells[1], "Hændelse først", expectLineBreak: false);
        AssertLiteralMultiline(sourceCells[1], "Kilde først", expectLineBreak: false);
        PlainText(emptyEventCells[1]).Should().BeNullOrWhiteSpace();
        PlainText(familyEventCells[1]).Should().Contain("Familiehændelsens notat");
        PlainText(emptyFamilyEventCells[1]).Should().BeNullOrWhiteSpace();
        PlainText(emptySourceCells[1]).Should().BeNullOrWhiteSpace();
        html.Should().NotContain("<h2>Falsk overskrift</h2>")
            .And.NotContain("<em>falsk</em>")
            .And.NotContain("<a href=")
            .And.NotContain("<mark>");
        result.SafePreview.NetworkRequestAttempts.Should().Be(0);
    }

    [Fact]
    public async Task CensusTable_ShouldKeepMultilineNotesAndOtherValuesInTheirCells()
    {
        var person = new Person("@I1@");
        person.Census.Add(new Census { Date = "1900\n1901 | 1902", Note = Note("Tælling først") });
        person.Census.Add(new Census { Date = "1910", Note = "   " });
        var model = new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion,
        [
            new(BiographyTemplateBlockType.Census, true, "Folketællinger",
                BiographyTemplatePresentation.Table, [BiographyTemplateField.CensusDate, BiographyTemplateField.CensusNote]),
        ]);

        var preview = await new TemplateEditorPreviewService().RenderAsync(model, person, null, CancellationToken.None);
        var rows = Elements(Section(preview.SafePreview.Html, "Folketællinger"), "tr");
        rows.Should().HaveCount(3);
        var cells = Elements(rows[1], "td");
        cells.Should().HaveCount(2);
        PlainText(cells[0]).Should().Be("1900 1901 | 1902");
        AssertLiteralMultiline(cells[1], "Tælling først", expectLineBreak: false);
        PlainText(Elements(rows[2], "td")[1]).Should().BeNullOrWhiteSpace();
        person.Census[0].Note.Should().Be(Note("Tælling først"));
    }

    [Theory]
    [InlineData(BiographyTemplatePresentation.List)]
    [InlineData(BiographyTemplatePresentation.Table)]
    public async Task BlankNotes_ShouldNotCreateVisibleArtifacts(
        BiographyTemplatePresentation presentation)
    {
        var service = new TemplateEditorPreviewService();
        var model = CreateNotesModel(presentation);
        var missing = await service.RenderAsync(model, CreatePersonWithBlankNotes(false), null, CancellationToken.None);
        var blank = await service.RenderAsync(model, CreatePersonWithBlankNotes(true), null, CancellationToken.None);

        NormalizeIntertagWhitespace(blank.SafePreview.Html)
            .Should().Be(NormalizeIntertagWhitespace(missing.SafePreview.Html));
        blank.SafePreview.NetworkRequestAttempts.Should().Be(0);
    }

    private static BiographyTemplateEditorModel CreateNotesModel(BiographyTemplatePresentation presentation) =>
        new(BiographyTemplateEditorModel.CurrentVersion,
        [
            new(BiographyTemplateBlockType.Title, true, "", BiographyTemplatePresentation.Standard,
                [BiographyTemplateField.PersonFullName]),
            new(BiographyTemplateBlockType.Facts, true, "Fakta", BiographyTemplatePresentation.List,
                [BiographyTemplateField.FamilyNote]),
            new(BiographyTemplateBlockType.Events, true, "Hændelser", presentation,
                [BiographyTemplateField.EventCategory, BiographyTemplateField.EventNote]),
            new(BiographyTemplateBlockType.Sources, true, "Kilder", presentation,
                [BiographyTemplateField.SourceTitle, BiographyTemplateField.SourceNote]),
            new(BiographyTemplateBlockType.Media, true, "Medier", BiographyTemplatePresentation.Standard,
                [BiographyTemplateField.MediaTitle, BiographyTemplateField.MediaNote]),
        ]);

    private static Person CreatePersonWithNotes()
    {
        var person = new Person("@I99@") { FullName = "Bodil Sørensen" };
        var family = new Family("@F1@");
        family.Notes.Add(Note("Familie først"));
        family.Notes.Add("");
        family.Notes.Add("   ");
        family.Events.Add(new GedcomEvent("MARR")
        {
            Category = GedcomEventCategory.Marriage,
            Note = "Familiehændelsens notat",
        });
        family.Events.Add(new GedcomEvent("DIV") { Category = GedcomEventCategory.Other, Note = "   " });
        person.Families.Add(family);
        person.Events.Add(new GedcomEvent("BIRT") { Category = GedcomEventCategory.Birth, Note = Note("Hændelse først") });
        person.Events.Add(new GedcomEvent("DEAT") { Category = GedcomEventCategory.Death, Note = "" });
        person.Sources.Add(new Source("@S1@") { Title = "Kirkebog", Note = Note("Kilde først") });
        person.Sources.Add(new Source("@S2@") { Title = "Anden kilde", Note = "   " });
        person.Media.Add(new Media("@M1@") { Title = "Familiefoto", Note = Note("Medie først") });
        person.Media.Add(new Media("@M2@") { Title = "Andet foto", Note = "" });
        return person;
    }

    private static string Note(string firstLine) =>
        firstLine + "\n## Falsk overskrift\n- Falsk liste\n*falsk* [falsk](https://example.invalid) a|b <mark>";

    private static Person CreatePersonWithBlankNotes(bool includeBlankValues)
    {
        var person = new Person("@I1@") { FullName = "Anna Jensen" };
        var family = new Family("@F1@");
        if (includeBlankValues)
        {
            family.Notes.Add("");
            family.Notes.Add("   ");
        }
        person.Families.Add(family);
        person.Events.Add(new GedcomEvent("BIRT")
        {
            Category = GedcomEventCategory.Birth,
            Note = includeBlankValues ? "   " : null,
        });
        person.Sources.Add(new Source("@S1@") { Title = "Kirkebog", Note = includeBlankValues ? "" : null });
        person.Media.Add(new Media("@M1@") { Title = "Foto", Note = includeBlankValues ? "   " : null });
        return person;
    }

    private static string Section(string html, string heading)
    {
        var match = Regex.Match(html, $@"<h2>{Regex.Escape(heading)}</h2>(.*?)(?=<h2>|</body>|\z)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        match.Success.Should().BeTrue();
        return match.Groups[1].Value;
    }

    private static string[] Elements(string html, string tag) =>
        Regex.Matches(html, $@"<{tag}(?:\s[^>]*)?>(.*?)</{tag}>",
                RegexOptions.Singleline | RegexOptions.CultureInvariant)
            .Select(match => match.Groups[1].Value).ToArray();

    private static string PlainText(string html) =>
        WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]*>", "", RegexOptions.Singleline));

    private static string NormalizeIntertagWhitespace(string html) =>
        Regex.Replace(html, @">\s+<", "><");

    private static void AssertLiteralMultiline(string html, string firstLine, bool expectLineBreak = true)
    {
        if (expectLineBreak)
        {
            html.Should().MatchRegex("<br\\s*/?>");
        }
        var text = PlainText(html);
        text.Should().Contain(firstLine)
            .And.Contain("## Falsk overskrift")
            .And.Contain("- Falsk liste")
            .And.Contain("*falsk*")
            .And.Contain("[falsk](https://example.invalid)")
            .And.Contain("a|b")
            .And.Contain("<mark>");
    }
}
