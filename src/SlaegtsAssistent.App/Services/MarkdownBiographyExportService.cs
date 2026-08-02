using System;
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
        new BiographyFileWriter(CreateGenerator(familyTree, outputDirectory))
            .WriteAll(familyTree, outputDirectory);
    }

    public string GenerateBiography(
        FamilyTree familyTree,
        Person person,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(familyTree);
        ArgumentNullException.ThrowIfNull(person);

        return CreateGenerator(familyTree, outputDirectory).Generate(person);
    }

    private IBiographyMarkdownGenerator CreateGenerator(
        FamilyTree familyTree,
        string outputDirectory)
    {
        var settings = _applicationSettingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.GlobalBiographyTemplatePath))
        {
            return new BiographyTemplateMarkdownGenerator(
                submitter: familyTree.Submitter,
                mediaBaseDirectory: outputDirectory);
        }

        var template = new BiographyTemplateLoader().Load(settings.GlobalBiographyTemplatePath);
        return new BiographyTemplateMarkdownGenerator(
            template.Source,
            familyTree.Submitter,
            outputDirectory);
    }
}
