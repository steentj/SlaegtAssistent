using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public class PersonListItemViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDisplayName()
    {
        var viewModel = new PersonListItemViewModel("@I1@", "Anna Jensen");

        viewModel.RecordId.Should().Be("@I1@");
        viewModel.DisplayName.Should().Be("Anna Jensen");
    }
}
