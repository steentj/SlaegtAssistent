using FluentAssertions;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.Tests;

public class AvaloniaGedcomFilePickerServiceTests
{
    [Fact]
    public void ResolveSelectedFilePath_ShouldReturnLocalPath_FromFileUri()
    {
        var path = AvaloniaGedcomFilePickerService.ResolveSelectedFilePath(
            new Uri("file:///tmp/eksempel.ged"));

        path.Should().Be("/tmp/eksempel.ged");
    }

    [Fact]
    public void CreateOpenOptions_ShouldExplicitlyAllowAllFilesAfterAnotherPicker()
    {
        var options = AvaloniaGedcomFilePickerService.CreateOpenOptions(null);

        options.Title.Should().Be("Vælg GEDCOM-fil");
        options.AllowMultiple.Should().BeFalse();
        options.FileTypeFilter.Should().ContainSingle();
        options.FileTypeFilter![0].Patterns.Should().Contain("*");
        options.FileTypeFilter[0].AppleUniformTypeIdentifiers.Should().Contain("public.item");
    }
}
