using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public sealed class GedcomHelpViewModelTests
{
    [Fact]
    public void Content_IsDanishAndCoversIntroductionTutorialAndReference()
    {
        var viewModel = new GedcomHelpViewModel();

        GedcomHelpViewModel.Content.Should().Contain("GEDCOM – slægtsforskningens digitale fællessprog");
        GedcomHelpViewModel.Content.Should().Contain("1984");
        GedcomHelpViewModel.Content.Should().Contain("0 @I1@ INDI");
        GedcomHelpViewModel.Content.Should().Contain("CREA");
        GedcomHelpViewModel.Content.Should().Contain("https://gedcom.io/specifications/");
        viewModel.PreviewHtml.Should().Contain("<h2");
        viewModel.PreviewHtml.Should().Contain("<table>");
        viewModel.PreviewHtml.Should().Contain("<pre>");
    }
}
