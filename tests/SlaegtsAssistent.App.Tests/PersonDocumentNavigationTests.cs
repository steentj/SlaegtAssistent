using FluentAssertions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class PersonDocumentNavigationTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly string _target;
    private readonly PersonListItemViewModel _person;

    public PersonDocumentNavigationTests()
    {
        Directory.CreateDirectory(_folder);
        _target = Path.Combine(_folder, "Søren (#1).md");
        File.WriteAllText(_target, "# Søren");
        _person = new("@I1@", "Søren", _target);
    }

    [Fact]
    public Task EditorButtons_AreBoundFocusableAndOpenTheReferencedPerson() =>
        HeadlessTestApplication.RunAsync(() =>
        {
            var source = Path.Combine(_folder, "barn.md");
            File.WriteAllText(source, "[Søren](S%C3%B8ren%20%28%231%29.md)");
            PersonDocumentLink? opened = null;
            var editor = new EditorViewModel(source, new FileSystemMarkdownFileStore(),
                resolvePersonLink: href => PersonDocumentNavigation.Resolve(href, source, [_person]),
                openPersonLink: link => opened = link);
            editor.Load();
            var viewModel = new MainWindowViewModel { Editor = editor };
            var window = new MainWindow();
            var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
            ((Panel)webView.Parent!).Children.Remove(webView);
            try
            {
                window.DataContext = viewModel;
                window.Measure(new Size(1280, 800));
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                var button = window.GetVisualDescendants().OfType<Button>()
                    .Single(item => item.Content as string == "Søren ↗");
                button.Focusable.Should().BeTrue();
                button.Command.Should().NotBeNull();
                button.Command!.Execute(null);
                opened!.RecordId.Should().Be(_person.RecordId);
                editor.IsDirty.Should().BeFalse();
            }
            finally { window.Close(); }
            return Task.CompletedTask;
        });

    [Fact]
    public void Preview_UsesOnlyResolvedPersonTargetsAndAddsMarkerWithoutChangingMarkdown()
    {
        const string markdown = "[Søren](S%C3%B8ren%20%28%231%29.md) og [igen](S%C3%B8ren%20%28%231%29.md) [ukendt](ukendt.md)";
        var source = Path.Combine(_folder, "barn.md");
        var result = new SafeMarkdownPreviewService().Render(markdown, source,
            resolvePersonLink: href => PersonDocumentNavigation.Resolve(href, source, [_person]));
        result.PersonLinks.Should().ContainSingle().Which.RecordId.Should().Be("@I1@");
        result.Html.Should().Contain("slaegt-person://open/%40I1%40").And.Contain("class=\"person-link\"");
        result.HtmlDocument.Should().Contain("content:\" ↗\"");
        result.Html.Should().NotContain("href=\"ukendt.md\"");
        result.NetworkRequestAttempts.Should().Be(0);
        markdown.Should().NotContain("↗").And.NotContain("slaegt-person:");
    }

    [Theory]
    [InlineData("https://example.org/person.md")]
    [InlineData("file:///tmp/person.md")]
    [InlineData("//server/person.md")]
    [InlineData("%2F%2Fserver/person.md")]
    [InlineData("javascript:alert(1)")]
    [InlineData("slaegt-person://open/%40I1%40")]
    [InlineData("mangler.md")]
    [InlineData("#afsnit")]
    public void Resolver_RejectsUnknownOrNonRelativeTargets(string href)
    {
        PersonDocumentNavigation.Resolve(href, Path.Combine(_folder, "barn.md"), [_person]).Should().BeNull();
    }

    [Fact]
    public void Resolver_RejectsDuplicateIdentityDiagnosticsAndDeletedFiles()
    {
        var source = Path.Combine(_folder, "barn.md");
        var href = Uri.EscapeDataString(Path.GetFileName(_target));
        PersonDocumentNavigation.Resolve(href, source,
            [_person, new(_person.RecordId, "Kopi", Path.Combine(_folder, "kopi.md"))]).Should().BeNull();
        PersonDocumentNavigation.Resolve(href, source,
            [new(_person.RecordId, "Fejl", _target, syncStatus: BiographySyncStatus.Tvetydig)]).Should().BeNull();
        File.Delete(_target);
        PersonDocumentNavigation.Resolve(href, source, [_person]).Should().BeNull();
    }

    public void Dispose() => Directory.Delete(_folder, true);
}
