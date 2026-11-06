using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public IReadOnlyDictionary<string, string> GenerateBiographies(FamilyTree familyTree, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var generator = CreateGenerator(familyTree, outputDirectory);
        return familyTree.People.ToDictionary(person => person.RecordId, person =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return generator.Generate(person);
        }, StringComparer.Ordinal);
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
        var personLinks = BiographyPersonLinkFactory.Create(familyTree, outputDirectory,
            new FileSystemMarkdownDocumentCatalog().Load(outputDirectory));
        if (string.IsNullOrWhiteSpace(settings.GlobalBiographyTemplatePath))
        {
            return new BiographyTemplateMarkdownGenerator(
                submitter: familyTree.Submitter,
                mediaBaseDirectory: outputDirectory,
                gedcomSourceDirectory: GetGedcomDirectory(familyTree),
                personLinks: personLinks);
        }

        var template = new BiographyTemplateLoader().Load(settings.GlobalBiographyTemplatePath);
        return new BiographyTemplateMarkdownGenerator(
            template.Source,
            familyTree.Submitter,
            outputDirectory,
            GetGedcomDirectory(familyTree),
            personLinks);
    }

    private static string? GetGedcomDirectory(FamilyTree familyTree) =>
        string.IsNullOrWhiteSpace(familyTree.SourceFilePath)
            ? null
            : Path.GetDirectoryName(familyTree.SourceFilePath);
}
