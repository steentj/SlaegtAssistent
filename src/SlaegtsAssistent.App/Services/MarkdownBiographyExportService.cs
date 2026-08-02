using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class MarkdownBiographyExportService : IMarkdownBiographyExportService
{
    private readonly IApplicationSettingsService _applicationSettingsService;

    public MarkdownBiographyExportService(IApplicationSettingsService applicationSettingsService)
    {
        _applicationSettingsService = applicationSettingsService;
    }

    public void WriteBiographies(FamilyTree familyTree, string outputDirectory)
    {
        var settings = _applicationSettingsService.Load();
        IBiographyMarkdownGenerator generator;
        if (string.IsNullOrWhiteSpace(settings.GlobalBiographyTemplatePath))
        {
            generator = new BiographyTemplateMarkdownGenerator(
                submitter: familyTree.Submitter,
                mediaBaseDirectory: outputDirectory);
        }
        else
        {
            var template = new BiographyTemplateLoader().Load(settings.GlobalBiographyTemplatePath);
            generator = new BiographyTemplateMarkdownGenerator(
                template.Source,
                familyTree.Submitter,
                outputDirectory);
        }

        new BiographyFileWriter(generator).WriteAll(familyTree, outputDirectory);
    }
}
