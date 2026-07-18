using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public class PersonListItemViewModelTests
{
    [Fact]
    public void Constructor_ShouldExposeDisplayName()
    {
        var viewModel = new PersonListItemViewModel("Anna Jensen");

        viewModel.DisplayName.Should().Be("Anna Jensen");
    }
}
